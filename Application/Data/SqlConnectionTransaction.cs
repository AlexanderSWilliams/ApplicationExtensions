using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Application.Data
{
    public class SqlConnectionTransaction : DbConnection, ITransaction, IDisposable
    {
        private SqlConnection _connection;
        private bool _disposed = false;

        public SqlConnectionTransaction(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
        }

        public override string ConnectionString
        {
            get
            {
                return _connection.ConnectionString;
            }

            set
            {
                _connection.ConnectionString = value;
            }
        }

        public override string Database
        {
            get
            {
                return _connection.Database;
            }
        }

        public override string DataSource
        {
            get
            {
                return _connection.DataSource;
            }
        }

        public Func<DbConnection> GetConnection
        {
            get
            {
                return () => _connection;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override string ServerVersion
        {
            get
            {
                return _connection.ServerVersion;
            }
        }

        public override ConnectionState State
        {
            get
            {
                return _connection.State;
            }
        }

        public DbTransaction Transaction { get; set; }

        public override void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            _connection.Close();
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        public override void Open()
        {
            _connection.Open();
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return _connection.BeginTransaction(isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            return _connection.CreateCommand();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) // for idempotence
                return;

            if (disposing)  // free managed resources
            {
                if (Transaction != null)
                    Transaction.Rollback();
                _connection.Dispose();
            }

            // free unmanaged resources
            _disposed = true;

            base.Dispose();
        }
    }
}