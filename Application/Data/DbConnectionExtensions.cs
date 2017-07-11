///////////////////////////////////////////////////////////////////////////////////////////////////
// Licensed to you under the New BSD License
// http://www.opensource.org/licenses/bsd-license.php
// Massive is copyright (c) 2009-2016 various contributors.
// All rights reserved.
// See for sourcecode, full history and contributors list: https://github.com/FransBouma/Massive
//
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this list of conditions and the
//   following disclaimer.
// - Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
//   the following disclaimer in the documentation and/or other materials provided with the distribution.
// - The names of its contributors may not be used to endorse or promote products derived from this software
//   without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
// WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
///////////////////////////////////////////////////////////////////////////////////////////////////
using Application.ByteArrayExtensions;
using Application.IEnumerableExtensions;
using Application.ObjectExtensions;
using Application.TypeExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;

namespace Application.Data
{
    public static class DbConnectionExtensions
    {
        static private readonly object _CommandTimeoutLock = new object();
        static private readonly DbProviderFactory _factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
        static private int _CommandTimeout = 30;

        public static int Execute(this DbConnection connection, string sql, int? commandTimeout = null, DbTransaction trans = null, params object[] args)
        {
            VerifyOpenConnection(connection);
            var cmd = CreateCommand(sql, connection, commandTimeout, trans, args);
            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                ex.Data.Add("SqlCommand", sql);
                return 1;
            }
        }

        public static List<T> Query<T>(this DbConnection connection, string sql, int? commandTimeout = null, DbTransaction trans = null, params object[] args)
        {
            var result = new List<T>();
            VerifyOpenConnection(connection);

            var HasDefaultConstructor = typeof(T).GetInstanceOfReferenceType() != null;
            try
            {
                using (var rdr = CreateCommand(sql, connection, commandTimeout, trans, args).ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (HasDefaultConstructor)
                        {
                            var row = (T)typeof(T).GetInstanceOfReferenceType();
                            rdr.RecordToDictionary().InjectInto(ref row);
                            result.Add(row);
                        }
                        else
                        {
                            if (rdr.FieldCount > 1)
                                throw new ApplicationException("The specified return type does not have a default constructor and the returned data has more than one column.");

                            result.Add((T)typeof(T).ParseValue(rdr.GetValue(0)));
                        }
                    }
                    rdr.Close();
                }
            }
            catch (SqlException ex)
            {
                ex.Data.Add("SqlCommand", sql);
            }
            return result;
        }

        public static List<Dictionary<string, string>> Query(this DbConnection connection, string sql, int? commandTimeout = null, DbTransaction trans = null, params object[] args)
        {
            var result = new List<Dictionary<string, string>>();
            VerifyOpenConnection(connection);
            try
            {
                using (var rdr = CreateCommand(sql, connection, commandTimeout, trans, args).ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        result.Add(rdr.RecordToStringDictionary());
                    }
                    rdr.Close();
                }
            }
            catch (SqlException ex)
            {
                ex.Data.Add("SqlCommand", sql);
            }

            return result;
        }

        public static void SetCommandTimeout(int timeout)
        {
            lock (_CommandTimeoutLock)
            {
                _CommandTimeout = timeout;
            }
        }

        public static void VerifyOpenConnection(this DbConnection connection)
        {
            if (connection.State == ConnectionState.Broken)
            {
                connection.Close();
                connection.Open();
            }

            if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Executing || connection.State == ConnectionState.Fetching)
                connection.Open();

            if (connection.State == ConnectionState.Connecting)
            {
                System.Threading.Thread.Sleep(10);
                VerifyOpenConnection(connection);
            }
        }

        private static void AddParam(this DbCommand cmd, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = string.Format("@{0}", cmd.Parameters.Count);
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                var o = value as ExpandoObject;
                if (o == null)
                {
                    p.Value = value;
                    var s = value as string;
                    if (s != null)
                    {
                        p.Size = s.Length > 4000 ? -1 : 4000;
                    }
                }
                else
                {
                    p.Value = ((IDictionary<string, object>)value).Values.FirstOrDefault();
                }
            }
            cmd.Parameters.Add(p);
        }

        private static void AddParams(this DbCommand cmd, params object[] args)
        {
            if (args == null)
                return;

            foreach (var item in args)
            {
                AddParam(cmd, item);
            }
        }

        private static DbCommand CreateCommand(string sql, DbConnection conn, int? commandTimeout, DbTransaction trans = null, params object[] args)
        {
            var result = _factory.CreateCommand();
            if (result != null)
            {
                var ConnAsTransaction = conn as ITransaction;
                result.CommandTimeout = commandTimeout ?? _CommandTimeout;
                result.Connection = ConnAsTransaction?.GetConnection() ?? conn;
                result.CommandText = sql;
                result.AddParams(args);
                result.Transaction = trans ?? ConnAsTransaction?.Transaction;
            }
            return result;
        }

        private static Dictionary<string, object> RecordToDictionary(this IDataReader reader)
        {
            var result = new Dictionary<string, object>();
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                result.Add(reader.GetName(i), DBNull.Value.Equals(v) ? null : v);
            }
            return result;
        }

        private static Dictionary<string, string> RecordToStringDictionary(this IDataReader reader)
        {
            var result = new Dictionary<string, string>();
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                var vByteArray = v as byte[];
                result.Add(reader.GetName(i), DBNull.Value.Equals(v) ? null :
                    (vByteArray.IsNullOrEmpty() ? v?.ToString() : "0x" + vByteArray.ToHexString()));
            }
            return result;
        }
    }
}