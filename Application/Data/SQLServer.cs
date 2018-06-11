using Application.GenericExtensions;
using Application.IEnumerableExtensions;
using Application.ObjectExtensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Application.Data.SQLServer
{
    public static class SQLServer
    {
        public static StringBuilder DefaultBuilder = new StringBuilder("DEFAULT");

        public static StringBuilder IsNULLBuilder = new StringBuilder("IS NULL");

        public static StringBuilder EqualsNullBuilder = new StringBuilder("= NULL");

        public static StringBuilder NULLBuilder = new StringBuilder("NULL");

        public enum NullableOption
        {
            OnlyNullable,
            OnlyNonNullable,
            Both
        }

        public static string BackupDBCommand(this DbConnection conn, string bakPath, string newName = null, string stats = "10")
        {
            var SourceDB = new System.Data.SqlClient.SqlConnectionStringBuilder(conn.ConnectionString).InitialCatalog;
            var Result = UpdateLogicalNamesCommand(conn, SourceDB);

            Result += @"BACKUP DATABASE [" + SourceDB + @"] TO  DISK = N'" + bakPath + @"' WITH FORMAT, INIT,  NAME = N'" + (newName ?? SourceDB) +
                @"', NOREWIND, NOUNLOAD,  STATS = " + stats + @";";

            return Result;
        }

        public static string ContainsIDsInTableCommand(string tableName, IEnumerable<object> matches)
        {
            if (!matches.Any())
                return null;

            var dictMatches = matches.Select(x => x.ToStringStringDictionary());

            var sb = new StringBuilder();
            return sb.Append("SELECT * FROM [").Append(tableName).Append("] AS [t0] WHERE (")
                .Append(dictMatches.Aggregate(x => GetMatchExpression(x), (result, x) => result.Append(" OR ").Append(GetMatchExpression(x)))).Append(")").ToString();
        }

        public static string DeleteAllTablesCommand(this DbConnection conn, string[] tableNamesToOmit)
        {
            var OrderedToDeleleTables = GetTableNamessInOrderOfValidDeletion(conn, tableNamesToOmit);

            return OrderedToDeleleTables.Aggregate(x => new StringBuilder("DELETE FROM [").Append(x).Append("]"), (result, x) => result.Append(System.Environment.NewLine).Append("DELETE FROM [").Append(x).Append("]")).ToString();
        }

        public static string DeleteIfExistsCommand(string tableName)
        {
            return "IF OBJECT_ID('tempdb.dbo." + tableName + "', 'U') IS NOT NULL\r\n    DROP TABLE #T;\r\nIF OBJECT_ID('dbo." + tableName +
                "', 'U') IS NOT NULL\r\n    DROP TABLE dbo." + tableName + ";\r\n";
        }

        public static string DropColumnAndDependentConstraintsAndIndex(this DbConnection conn, string tableName, string columnName)
        {
            var ForeignKeys = GetDependentForeignKeys(conn, tableName, columnName);
            var PrimaryKeys = GetDependentPrimaryKey(conn, tableName, columnName);
            var DefaultConstraints = GetDependentDefaultConstraints(conn, tableName, columnName);
            var CheckConstraints = GetDependentCheckConstraints(conn, tableName, columnName);

            var Constraints = ForeignKeys.Concat(PrimaryKeys).Concat(DefaultConstraints).Concat(CheckConstraints).ToList();
            var Indexes = GetDependentIndexes(conn, tableName, columnName);
            var DropConstraintCommand = "ALTER TABLE [" + tableName + "] DROP CONSTRAINT ";

            return Constraints.Aggregate(x => DropConstraintCommand + "[" + x + "]", (result, x) => result + ";" + System.Environment.NewLine + DropConstraintCommand +
                "[" + x + "]").FirstOrDefault() + ";" + System.Environment.NewLine +
                    Indexes.Aggregate(x => "DROP INDEX " + "[" + x + "]" + " ON [" + tableName + "]", (result, x) => result + ";" + System.Environment.NewLine +
                    "" + "DROP INDEX " + "[" + x + "]" + " ON [" + tableName + "]").FirstOrDefault() + ";" + System.Environment.NewLine +
                    "ALTER TABLE [" + tableName + @"] DROP COLUMN [" + columnName + @"];";
        }

        public static string DropForeignKeyConstraintCommand(this DbConnection conn, string tableName, string columnName, string relatedTableName)
        {
            var ForeignKeyName = GetForeignKeyName(conn, relatedTableName, columnName, tableName);

            return "ALTER TABLE " + tableName + " drop [" + ForeignKeyName + "]";
        }

        public static IEnumerable<RelationshipInfo> GetAllRelationships(this DbConnection conn)
        {
            // Generated from
            //sys.foreign_keys.GroupJoin(sys.foreign_key_columns, o => o.object_id, i => i.constraint_object_id, (o, i) => new {
            //    ForeignKeyName = o.name,
            //    column_id = i.First().parent_column_id,
            //    object_id = i.First().parent_object_id,
            //    i.First().referenced_column_id,
            //    i.First().referenced_object_id
            //})
            //.GroupJoin(sys.columns, o => new { o.object_id, o.column_id }, i => new { i.object_id, i.column_id }, (o, i) => new {
            //    o.ForeignKeyName,
            //    o.object_id,
            //    o.referenced_object_id,
            //    o.referenced_column_id,
            //    ColumnName = i.First().name,
            //    i.First().is_nullable
            //})
            //.GroupJoin(sys.columns, o => new { object_id = o.referenced_object_id, column_id = o.referenced_column_id }, i => new { i.object_id, i.column_id }, (o, i) => new {
            //    o.ForeignKeyName,
            //    o.object_id,
            //    o.referenced_object_id,
            //    o.referenced_column_id,
            //    o.is_nullable,
            //    o.ColumnName,
            //    ReferencedColumnName = i.First().name
            //})
            //.GroupJoin(sys.tables, o => o.object_id, i => i.object_id, (o, i) => new {
            //    o.ForeignKeyName,
            //    o.object_id,
            //    o.referenced_object_id,
            //    o.is_nullable,
            //    o.ColumnName,
            //    o.ReferencedColumnName,
            //    TableName = i.First().name
            //})
            //.GroupJoin(sys.tables, o => o.referenced_object_id, i => i.object_id, (o, i) => new {
            //    o.ColumnName,
            //    o.ForeignKeyName,
            //    IsNullable = o.is_nullable,
            //    o.ReferencedColumnName,
            //    RelatedTable = i.First().name,
            //    o.TableName,
            //})
            //.OrderBy(x => x.TableName).ThenBy(x => x.RelatedTable).ThenBy(x => x.ColumnName).ThenBy(x => x.ForeignKeyName)

            var query = @"
SELECT [t23].[value] AS [ColumnName], [t23].[name] AS [ForeignKeyName], [t23].[value5] AS [IsNullable], [t23].[value3] AS [ReferencedColumnName], [t23].[value2] AS [RelatedTable], [t23].[value22] AS [TableName]
FROM (
    SELECT [t20].[value], [t20].[name], [t20].[value5], [t20].[value3], (
        SELECT [t22].[name]
        FROM (
            SELECT TOP (1) [t21].[name]
            FROM [sys].[tables] AS [t21]
            WHERE [t20].[value4] = [t21].[object_id]
            ) AS [t22]
        ) AS [value2], [t20].[value2] AS [value22]
    FROM (
        SELECT [t17].[name], [t17].[value4], [t17].[value5], [t17].[value], [t17].[value3], (
            SELECT [t19].[name]
            FROM (
                SELECT TOP (1) [t18].[name]
                FROM [sys].[tables] AS [t18]
                WHERE [t17].[value2] = [t18].[object_id]
                ) AS [t19]
            ) AS [value2]
        FROM (
            SELECT [t14].[name], [t14].[value2], [t14].[value4], [t14].[value5], [t14].[value], (
                SELECT [t16].[name]
                FROM (
                    SELECT TOP (1) [t15].[name]
                    FROM [sys].[columns] AS [t15]
                    WHERE ([t14].[value4] = [t15].[object_id]) AND ([t14].[value3] = [t15].[column_id])
                    ) AS [t16]
                ) AS [value3]
            FROM (
                SELECT [t9].[name], [t9].[value2], [t9].[value4], [t9].[value3], (
                    SELECT [t11].[name]
                    FROM (
                        SELECT TOP (1) [t10].[name]
                        FROM [sys].[columns] AS [t10]
                        WHERE ([t9].[value2] = [t10].[object_id]) AND ([t9].[value] = [t10].[column_id])
                        ) AS [t11]
                    ) AS [value], (
                    SELECT [t13].[is_nullable]
                    FROM (
                        SELECT TOP (1) [t12].[is_nullable]
                        FROM [sys].[columns] AS [t12]
                        WHERE ([t9].[value2] = [t12].[object_id]) AND ([t9].[value] = [t12].[column_id])
                        ) AS [t13]
                    ) AS [value5]
                FROM (
                    SELECT [t0].[name], (
                        SELECT [t2].[parent_column_id]
                        FROM (
                            SELECT TOP (1) [t1].[parent_column_id]
                            FROM [sys].[foreign_key_columns] AS [t1]
                            WHERE [t0].[object_id] = [t1].[constraint_object_id]
                            ) AS [t2]
                        ) AS [value], (
                        SELECT [t4].[parent_object_id]
                        FROM (
                            SELECT TOP (1) [t3].[parent_object_id]
                            FROM [sys].[foreign_key_columns] AS [t3]
                            WHERE [t0].[object_id] = [t3].[constraint_object_id]
                            ) AS [t4]
                        ) AS [value2], (
                        SELECT [t6].[referenced_column_id]
                        FROM (
                            SELECT TOP (1) [t5].[referenced_column_id]
                            FROM [sys].[foreign_key_columns] AS [t5]
                            WHERE [t0].[object_id] = [t5].[constraint_object_id]
                            ) AS [t6]
                        ) AS [value3], (
                        SELECT [t8].[referenced_object_id]
                        FROM (
                            SELECT TOP (1) [t7].[referenced_object_id]
                            FROM [sys].[foreign_key_columns] AS [t7]
                            WHERE [t0].[object_id] = [t7].[constraint_object_id]
                            ) AS [t8]
                        ) AS [value4]
                    FROM [sys].[foreign_keys] AS [t0]
                    ) AS [t9]
                ) AS [t14]
            ) AS [t17]
        ) AS [t20]
    ) AS [t23]
ORDER BY [t23].[value22], [t23].[value2], [t23].[value], [t23].[name]
";

            return conn.Query<RelationshipInfo>(query);
        }

        public static IEnumerable<Dictionary<string, string>> GetColumnInformationSchema(this DbConnection conn, IEnumerable<string> columns = null)
        {
            return conn.Query(@"SELECT " + (columns != null ? columns.Aggregate(x => x, (result, x) => result + ", " + x) : "*")
                    + " FROM INFORMATION_SCHEMA.COLUMNS")
                .Select(x => x.ToDictionary(y => y.Key, y => y.Value?.ToString()));
        }

        public static List<string> ColumnNames(DbConnection conn, string tableName, string schema)
        {
            tableName = tableName.Replace("[", "").Replace("]", "");
            schema = schema.Replace("[", "").Replace("]", "");

            return conn.Query<string>(@"SELECT col.name FROM sys.columns col
                JOIN sys.tables tab
                ON col.object_id = tab.object_id
                JOIN sys.schemas sch
                ON tab.schema_id = sch.schema_id
                WHERE tab.name = '" + tableName + "' AND sch.name = '" + schema + "' ORDER BY column_id");
        }

        public static IEnumerable<SQLServer.RelationshipInfo> GetCyclicRelationships(this DbConnection conn)
        {
            var Relationships = SQLServer.GetAllRelationships(conn);
            var TablesWithRelatedTables = Relationships
                .GroupBy(x => x.TableName)
                .ToDictionary(x => x.Key, x => x.ToList());

            var result = new List<SQLServer.RelationshipInfo>();
            foreach (KeyValuePair<string, List<SQLServer.RelationshipInfo>> pair in TablesWithRelatedTables)
            {
                foreach (var value in pair.Value)
                {
                    var RelatedTable = value.RelatedTable;
                    if (pair.Key != RelatedTable && TablesWithRelatedTables.ContainsKey(RelatedTable) && TablesWithRelatedTables[RelatedTable]
                        .Select(x => x.RelatedTable).Contains(pair.Key))
                    {
                        result.Add(value);
                    }
                }
            }

            return result;
        }

        public static Dictionary<string, Dictionary<string, string>> GetDefaultDefinitions(this DbConnection conn)
        {
            return conn.Query(@"select Sch = s.name, ColumnName = col.name, TableName = o.name, DefaultValue = object_definition(col.default_object_id) from sys.default_constraints c
    inner join sys.columns col on col.default_object_id = c.object_id
    inner join sys.objects o  on o.object_id = c.parent_object_id
    inner join sys.schemas s on s.schema_id = o.schema_id")
                .GroupBy(x => "[" + x["Sch"] + "].[" + x["TableName"] + "]")
                .ToDictionary(x => x.Key, x => x.ToDictionary(y => y["ColumnName"], y => y["DefaultValue"], StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> GetDependentTables(DbConnection conn, string tableName,
                            NullableOption nullableOption, int depth = int.MaxValue)
        {
            var Relationships = SQLServer.GetAllRelationships(conn);
            if (nullableOption == NullableOption.OnlyNullable)
                Relationships = Relationships.Where(x => x.IsNullable).ToList();
            if (nullableOption == NullableOption.OnlyNonNullable)
                Relationships = Relationships.Where(x => !x.IsNullable).ToList();

            depth--;
            var Result = new HashSet<string>(Relationships.Where(x => x.RelatedTable == tableName).Select(x => x.TableName));

            int OldCount = 0;
            while (Result.Count != OldCount && depth > 0)
            {
                OldCount = Result.Count;
                depth--;

                foreach (var table in Result.ToList())
                {
                    var result = Relationships.Where(x => x.RelatedTable == table).Select(x => x.TableName);
                    foreach (var relatedTable in result)
                    {
                        Result.Add(relatedTable);
                    }
                }
            }

            return Result;
        }

        public static List<string> GetLogicalNamesFromBak(this DbConnection conn, string bakPath)
        {
            var query = @"DECLARE @Table TABLE (LogicalName varchar(128),[PhysicalName] varchar(128), [Type] varchar, [FileGroupName] varchar(128), [Size] varchar(128),
            [MaxSize] varchar(128), [FileId]varchar(128), [CreateLSN]varchar(128), [DropLSN]varchar(128), [UniqueId]varchar(128), [ReadOnlyLSN]varchar(128), [ReadWriteLSN]varchar(128),
            [BackupSizeInBytes]varchar(128), [SourceBlockSize]varchar(128), [FileGroupId]varchar(128), [LogGroupGUID]varchar(128), [DifferentialBaseLSN]varchar(128), [DifferentialBaseGUID]varchar(128), [IsReadOnly]varchar(128), [IsPresent]varchar(128), [TDEThumbprint]varchar(128)
)
DECLARE @Path varchar(1000)='" + bakPath + @"'
DECLARE @LogicalNameData varchar(128),@LogicalNameLog varchar(128)
INSERT INTO @table
EXEC('
RESTORE FILELISTONLY
   FROM DISK=''' +@Path+ '''
   ')

   SET @LogicalNameData=(SELECT LogicalName FROM @Table WHERE Type='D')
   SET @LogicalNameLog=(SELECT LogicalName FROM @Table WHERE Type='L')

SELECT @LogicalNameData UNION ALL SELECT @LogicalNameLog;";

            return conn.Query<string>(query);
        }

        public static Dictionary<string, List<string>> GetNonNullableColumns(this DbConnection conn)
        {
            return conn.Query(@"SELECT TABLE_NAME, COLUMN_NAME, TABLE_SCHEMA
FROM INFORMATION_SCHEMA.COLUMNS
WHERE IS_NULLABLE = 'NO'")
            .GroupBy(x => "[" + x["TABLE_SCHEMA"] + "].[" + x["TABLE_NAME"] + "]")
            .ToDictionary(x => x.Key, x => x.Select(y => y["COLUMN_NAME"]).ToList(), StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, List<string>> GetPrimaryKeyTableColumns(this DbConnection conn, IEnumerable<string> tableNames)
        {
            var WhereCMD = string.Format("WHERE TABLE_NAME in ({0})", string.Join(",", tableNames.Select(t => string.Format("'{0}'", t))));
            return GetPrimaryKeyTableColumns(conn, WhereCMD);
        }

        public static Dictionary<string, List<string>> GetPrimaryKeyTableColumns(this DbConnection conn, string WhereCMD = null)
        {
            var query = @"
                    SELECT * FROM (
select A.TABLE_NAME, A.TABLE_SCHEMA, COLUMN_NAME from INFORMATION_SCHEMA.KEY_COLUMN_USAGE A
	JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS B
	ON A.TABLE_NAME = B.TABLE_NAME AND
	   A.CONSTRAINT_CATALOG = B.CONSTRAINT_CATALOG AND
	   A.CONSTRAINT_SCHEMA = B.CONSTRAINT_SCHEMA AND
	   A.CONSTRAINT_NAME = B.CONSTRAINT_NAME
	WHERE B.CONSTRAINT_TYPE = 'PRIMARY KEY') A " + WhereCMD;

            return conn.Query(query).Select(x => new
            {
                SchemaName = x["TABLE_SCHEMA"],
                TableName = x["TABLE_NAME"],
                ColumName = x["COLUMN_NAME"]
            })
            .GroupBy(x => "[" + x.SchemaName + "].[" + x.TableName + "]")
            .ToDictionary(x => x.Key, x => x.Select(y => y.ColumName).ToList(), StringComparer.OrdinalIgnoreCase);
        }

        public static StringBuilder GetSQLEqualsValueBuilder(string value)
        {
            return value != null ? new StringBuilder("= '").Append(value.Replace("'", "''")).Append("'") : IsNULLBuilder;
        }

        public static StringBuilder GetSQLLikeValueBuilder(string value)
        {
            return value != null ? new StringBuilder("LIKE '").Append(value.Replace("'", "''")).Append("'") : IsNULLBuilder;
        }

        public static StringBuilder GetSQLSetValueBuilder(string value)
        {
            return value != null ? new StringBuilder("= '").Append(value.Replace("'", "''")).Append("'") : EqualsNullBuilder;
        }

        public static StringBuilder GetSQLValueBuilder(string value, bool omitQuote)
        {
            if (omitQuote)
                return value != null ? new StringBuilder(value) : SQLServer.DefaultBuilder;
            else
                return value != null ? new StringBuilder("'").Append(value.Replace("'", "''")).Append("'") : SQLServer.DefaultBuilder;
        }

        public static IEnumerable<string> GetTableNames(this DbConnection conn)
        {
            // Generated From
            //sys.tables.Select(x => x.name).OrderBy(x => x)
            var query = @"
                SELECT [t0].[name]
                FROM [sys].[tables] AS [t0]
                ORDER BY [t0].[name]";

            return conn.Query<string>(query);
        }

        public static IEnumerable<string> GetTableNamessInOrderOfValidDeletion(this DbConnection conn, string[] tableNamesToOmit)
        {
            var Relationships = SQLServer.GetAllRelationships(conn);

            var TablesWithRelatedTables = Relationships
                .Where(x => !x.IsNullable)
                .GroupBy(x => x.TableName)
                .Select(x => new
                {
                    TableName = x.Key,
                    RelatedTables = x.Select(y => y.RelatedTable).Distinct()
                }).ToList();

            var TableNamesToDelete = SQLServer.GetTableNames(conn).Where(x => !tableNamesToOmit.Any(y => x.StartsWith(y))).ToList();
            var NumberOfTables = TableNamesToDelete.Count;
            var OrderedToDeleleTables = new HashSet<string>();

            int OldCount = -1, newCount = 0;
            while (OrderedToDeleleTables.Count != OldCount)
            {
                foreach (var tableToInsert in TableNamesToDelete)
                {
                    if (!OrderedToDeleleTables.Contains(tableToInsert))
                    {
                        var IsOkayToInsert = true;

                        foreach (var table in TablesWithRelatedTables)
                        {
                            if (table.TableName != tableToInsert && table.RelatedTables.Contains(tableToInsert)
                                    && !OrderedToDeleleTables.Contains(table.TableName))
                            {
                                IsOkayToInsert = false;
                                break;
                            }
                        }

                        if (IsOkayToInsert)
                        {
                            OrderedToDeleleTables.Add(tableToInsert);
                        }
                    }
                }

                OldCount = newCount;
                newCount = OrderedToDeleleTables.Count;
            }

            return OrderedToDeleleTables;
        }

        public static Dictionary<string, HashSet<string>> GetUnQuotedColumns(this DbConnection conn)
        {
            return conn.Query(@"SELECT TABLE_NAME, COLUMN_NAME, TABLE_SCHEMA
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (DATA_TYPE = 'binary' OR DATA_TYPE = 'varbinary' OR DATA_TYPE = 'image')")
            .GroupBy(x => "[" + x["TABLE_SCHEMA"] + "].[" + x["TABLE_NAME"] + "]")
            .ToDictionary(x => x.Key, x => x.Select(y => y["COLUMN_NAME"]).ToHashSet(), StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> InsertRowsCommand(string tableName, object data, int batchRowCount, int multiLineNum = 25, IEnumerable<string> columnsToReturn = null, bool indentityInsert = false, IEnumerable<string> columnsToExclude = null, HashSet<string> unquotedColumns = null)
        {
            var Data = data as IEnumerable<object> ?? new[] { data };

            return Data.ChunkifyToList((list, y) => list.Count != batchRowCount).Select(x =>
                InsertRowsCommand(tableName, x.Select(y => y.ToStringStringDictionary()), multiLineNum, columnsToReturn, indentityInsert, columnsToExclude, unquotedColumns));
        }

        public static List<string> MergeRowsCommand(string tableName, IEnumerable<string> primaryKeyColumnNames, IEnumerable<object> data, bool tableHasIdentity, int batchRowCount, string setCMD = null, bool deleteUnspecifiedRows = false, Dictionary<string, string> ColumnsToDefault = null, IEnumerable<string> naturalKeyColumns = null, HashSet<string> unquotedColumns = null)
        {
            var dictData = data.Select(x => x.ToStringStringDictionary());
            var Result = new List<string>();

            var TempTableName = "__" + tableName + "Temp";
            var columnNames = dictData.Select(x => x.Keys).First();
            var columnNamesDelimited = columnNames.Select(x => "[" + x + "]").Aggregate(x => new StringBuilder(x), (result, x) => result.Append(", ").Append(x));
            var setColumnsCommand = columnNames.Where(x => !primaryKeyColumnNames.Contains(x)).Select(x => "[" + x + "]")
                                        .Aggregate(x => new StringBuilder("[").Append(tableName).Append("].").Append(x).Append(" = [").Append(TempTableName).Append("].").Append(x),
                                            (result, x) => result.Append(", [").Append(tableName).Append("].").Append(x).Append(" = [").Append(TempTableName).Append("].").Append(x));

            var builder = new StringBuilder(DeleteIfExistsCommand(TempTableName) + "SELECT TOP 0 ").Append(columnNamesDelimited).Append(" INTO [").Append(TempTableName).Append("] from [").Append(tableName)
                .Append(@"];");

            tableHasIdentity = tableHasIdentity && naturalKeyColumns == null;
            if (tableHasIdentity)
                builder.Append("SET IDENTITY_INSERT [").Append(TempTableName).Append("] ON").Append(System.Environment.NewLine);

            if (ColumnsToDefault != null)
                builder.Append(ColumnsToDefault.Aggregate("", (a, c) => a + string.Format("Alter table {0} add constraint def_temp_{0}_{1} default " + c.Value + " for {1};", TempTableName, c.Key))).Append(System.Environment.NewLine);

            Result.Add(builder.Length > 0 ? builder.ToString() : ";");

            dictData.ChunkifyToList((list, y) => list.Count != batchRowCount).ToList().ForEach(y =>
            {
                Result.Add(InsertRowsCommand(TempTableName, y, 25, null, false, null, unquotedColumns));
            });

            builder = new StringBuilder("");

            if (tableHasIdentity)
                builder.Append("SET IDENTITY_INSERT [").Append(TempTableName).Append(@"] OFF;");

            if (setCMD != null)
                builder.Append("UPDATE [").Append(TempTableName).Append("] ").Append(setCMD).Append(";");

            naturalKeyColumns = naturalKeyColumns ?? primaryKeyColumnNames;

            if (deleteUnspecifiedRows && !naturalKeyColumns.IsNullOrEmpty())
                builder.Append("DELETE a FROM [" + tableName + "] a LEFT OUTER JOIN [" + TempTableName +
                    "] b ON ").Append(naturalKeyColumns.Select(x => "a.[" + x + "] = b.[" + x + "]")
                                .Aggregate(x => x, (result, x) => result + " AND " + x))
                            .Append(" WHERE ")
                            .Append(naturalKeyColumns.Select(x => "b.[" + x + "] IS NULL")
                                .Aggregate(x => x, (result, x) => result + " OR " + x)).Append(";");

            if (!naturalKeyColumns.IsNullOrEmpty())
                builder.Append("UPDATE [").Append(tableName).Append("] SET ").Append(setColumnsCommand).Append(" FROM [").Append(TempTableName).Append("] INNER JOIN [").Append(tableName).Append("] ON ")
                    .Append((naturalKeyColumns.Select(x => "[" + TempTableName + "].[" + x + "] = [" + tableName + "].[" + x + "]")
                                .Aggregate(x => x, (result, x) => result + " AND " + x) + ";"));

            if (tableHasIdentity)
                builder.Append("SET IDENTITY_INSERT [").Append(tableName).Append(@"] ON;");

            builder.Append(@"INSERT INTO [")
                    .Append(tableName).Append(@"] (").Append(columnNamesDelimited).Append(") SELECT ")
                    .Append(columnNames.Select(x => "A.[" + x + "]").Aggregate(x => new StringBuilder(x), (result, x) => result.Append(", ").Append(x)))
                    .Append(" FROM [").Append(TempTableName).Append("]");

            if (!naturalKeyColumns.IsNullOrEmpty())
                builder.Append(@" AS A
						LEFT JOIN [").Append(tableName).Append(@"] AS B ON ")
                            .Append(naturalKeyColumns.Select(x => @"A.[" + x + @"] = B.[" + x + @"]")
                                            .Aggregate(x => x, (result, x) => result + " AND " + x))
                            .Append(" WHERE ").Append(naturalKeyColumns.Select(x => "B.[" + x + "] IS NULL").Aggregate(x => x, (result, x) => result + " OR " + x));

            if (tableHasIdentity)
                builder.Append(";SET IDENTITY_INSERT [").Append(tableName).Append(@"] OFF");

            builder.Append(";DROP TABLE [").Append(TempTableName).Append("];");

            Result.Add(builder.Length > 0 ? builder.ToString() : ";");
            return Result;
        }

        public static string RestoreDBCommand(this DbConnection conn, string mdfPath, string logPath, string bakPath, string destinationDB, string stats = "5")
        {
            var Result = @"IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'" + destinationDB + @"')
                        EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'" + destinationDB + @"';
                        ALTER DATABASE[" + destinationDB + @"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE[" + destinationDB + @"];
                        CREATE DATABASE [" + destinationDB + @"];";

            var sourceLogicalNames = GetLogicalNamesFromBak(conn, bakPath);
            Result += "RESTORE DATABASE [" + destinationDB + @"] FROM  DISK = N'" + bakPath + @"' WITH MOVE N'" + sourceLogicalNames[0] + @"' TO N'" + mdfPath + "', MOVE N'" + sourceLogicalNames[1] + @"' TO N'" + logPath + "',  NOUNLOAD,  REPLACE,  STATS = " + stats + ";";
            Result += UpdateLogicalNamesCommand(conn, destinationDB);

            return Result;
        }

        public static bool? TableHasIdentity(this DbConnection conn, string tableName)
        {
            var Identity = conn.Query("SELECT OBJECTPROPERTY(OBJECT_ID('" + tableName.Replace("]", "").Replace("[", "") + "'), 'TableHasIdentity')").First().First().Value.ToInt32();
            if (Identity == null)
                return null;
            return Identity == 1;
        }

        public static string UpdateLogicalNamesCommand(this DbConnection conn, string DB)
        {
            var query = @"select top 1 name from [" + DB + @"].sys.database_files WHERE type_desc = 'ROWS'
                          UNION ALL " +
                        @"select top 1 name from [" + DB + @"].sys.database_files WHERE type_desc = 'LOG'";

            var DBLogicalNames = conn.Query<string>(query);

            var Result = "";
            if (DBLogicalNames[0] != DB)
                Result += "ALTER DATABASE [" + DB + @"] MODIFY FILE (Name = '" + DBLogicalNames[0] + @"', NEWNAME = " + DB + ");";
            if (DBLogicalNames[1] != DB + "_log")
                Result += "ALTER DATABASE [" + DB + @"] MODIFY FILE (Name = '" + DBLogicalNames[1] + @"',NEWNAME = " + DB + @"_log);";

            return Result;
        }

        public static string DeleteRowsCommand(string tableName, object data)
        {
            tableName = tableName[0] == '[' ? tableName : "[" + tableName + "]";
            var Data = (data as IEnumerable<object> ?? new[] { data }).Select(x => x.ToStringStringDictionary());

            var builder = new StringBuilder();
            foreach (var row in Data)
            {
                builder.Append("DELETE FROM ").Append(tableName).Append(" WHERE ").Append(row.Select(x => new StringBuilder("[").Append(x.Key).Append("] = '").Append(x.Value).Append("'"))
                                                                                                .Aggregate(x => x, (result, x) => result.Append(" AND ").Append(x)));

                builder.Append(";\r\n");
            }

            return builder.Length > 0 ? builder.ToString() : ";";
        }

        public static string UpdateRowsCommand(string tableName, IEnumerable<string> primaryKeyColumnNames, object data)
        {
            var Data = data as IEnumerable<object> ?? new[] { data };
            return UpdateRowsCommand(tableName, primaryKeyColumnNames, Data.Select(x => x.ToStringStringDictionary()));
        }

        public static string UpdateRowsCommand<T, S>(string tableName, IEnumerable<SetWhereRow<T, S>> setWhereDiffs, bool useLike = false)
        {
            return UpdateRowsCommand(tableName, setWhereDiffs.Select(x => Tuple.Create(x.SetData.ToStringStringDictionary(), x.WhereData.ToStringStringDictionary())), useLike);
        }

        private static IEnumerable<string> GetDependentCheckConstraints(this DbConnection conn, string tableName, string columnName)
        {
            // Generated from
            //sys.check_constraints.GroupJoin(sys.columns, o => new {object_id = o.parent_object_id, column_id = o.parent_column_id}, i => new {i.object_id, i.column_id}, (o, i) => new {
            //    object_id = o.parent_object_id,
            //    CheckConstraintName = o.name,
            //    ColumnName = i.First().name
            //})
            //.GroupJoin(sys.tables, o => o.object_id, i => i.object_id, (o, i) => new {
            //    o.ColumnName,
            //    o.CheckConstraintName,
            //    TableName = i.First().name,
            //})
            //.Where(x => x.TableName == "[TableName]" && x.ColumnName == "[ColumnName]")
            //.Select(x => x.CheckConstraintName)

            var query = @"
                SELECT [t6].[name]
                FROM (
                    SELECT [t3].[value], [t3].[name], (
                        SELECT [t5].[name]
                        FROM (
                            SELECT TOP (1) [t4].[name]
                            FROM [sys].[tables] AS [t4]
                            WHERE [t3].[parent_object_id] = [t4].[object_id]
                            ) AS [t5]
                        ) AS [value2]
                    FROM (
                        SELECT [t0].[parent_object_id], [t0].[name], (
                            SELECT [t2].[name]
                            FROM (
                                SELECT TOP (1) [t1].[name]
                                FROM [sys].[columns] AS [t1]
                                WHERE ([t0].[parent_object_id] = [t1].[object_id]) AND ([t0].[parent_column_id] = [t1].[column_id])
                                ) AS [t2]
                            ) AS [value]
                        FROM [sys].[check_constraints] AS [t0]
                        ) AS [t3]
                    ) AS [t6]
                WHERE ([t6].[value2] = '" + tableName + @"') AND ([t6].[value] = '" + columnName + @"')";

            return conn.Query<string>(query);
        }

        private static IEnumerable<string> GetDependentDefaultConstraints(this DbConnection conn, string tableName, string columnName)
        {
            // Generated from
            //sys.default_constraints.GroupJoin(sys.columns, o => new {object_id = o.parent_object_id, column_id = o.parent_column_id}, i => new {i.object_id, i.column_id}, (o, i) => new {
            //    object_id = o.parent_object_id,
            //    DefaultConstraintName = o.name,
            //    ColumnName = i.First().name
            //})
            //.GroupJoin(sys.tables, o => o.object_id, i => i.object_id, (o, i) => new {
            //    o.ColumnName,
            //    o.DefaultConstraintName,
            //    TableName = i.First().name,
            //})
            //.Where(x => x.TableName == "[TableName]" && x.ColumnName == "[ColumnName]")
            //.Select(x => x.DefaultConstraintName)

            var query = @"
                SELECT [t6].[name]
                FROM (
                    SELECT [t3].[value], [t3].[name], (
                        SELECT [t5].[name]
                        FROM (
                            SELECT TOP (1) [t4].[name]
                            FROM [sys].[tables] AS [t4]
                            WHERE [t3].[parent_object_id] = [t4].[object_id]
                            ) AS [t5]
                        ) AS [value2]
                    FROM (
                        SELECT [t0].[parent_object_id], [t0].[name], (
                            SELECT [t2].[name]
                            FROM (
                                SELECT TOP (1) [t1].[name]
                                FROM [sys].[columns] AS [t1]
                                WHERE ([t0].[parent_object_id] = [t1].[object_id]) AND ([t0].[parent_column_id] = [t1].[column_id])
                                ) AS [t2]
                            ) AS [value]
                        FROM [sys].[default_constraints] AS [t0]
                        ) AS [t3]
                    ) AS [t6]
                WHERE ([t6].[value2] = '" + tableName + @"') AND ([t6].[value] = '" + columnName + @"')";

            return conn.Query<string>(query);
        }

        private static IEnumerable<string> GetDependentForeignKeys(this DbConnection conn, string tableName, string columnName)
        {
            // Generated from
            //sys.foreign_keys.GroupJoin(sys.foreign_key_columns, o => o.object_id, i => i.constraint_object_id, (o, i) => new {
            //    ForeignKeyName = o.name,
            //    column_id = i.First().parent_column_id,
            //    object_id = i.First().parent_object_id,
            //    i.First().referenced_column_id,
            //    i.First().referenced_object_id
            //})
            //.GroupJoin(sys.columns, o => new {o.object_id, o.column_id}, i => new {i.object_id, i.column_id}, (o, i) => new {
            //    o.ForeignKeyName,
            //    o.object_id,
            //    ColumnName = i.First().name
            //})
            //.GroupJoin(sys.tables, o => o.object_id, i => i.object_id, (o, i) => new {
            //    o.ForeignKeyName,
            //    o.ColumnName,
            //    TableName = i.First().name,
            //})
            //.Where(x => x.TableName == "[TableName]" && x.ColumnName == "[ColumnName]")
            //.Select(x => x.ForeignKeyName)

            var query = @"
	            SELECT [t11].[name]
	            FROM (
		            SELECT [t8].[name], [t8].[value], (
			            SELECT [t10].[name]
			            FROM (
				            SELECT TOP (1) [t9].[name]
				            FROM [sys].[tables] AS [t9]
				            WHERE [t8].[value2] = [t9].[object_id]
				            ) AS [t10]
			            ) AS [value2]
		            FROM (
			            SELECT [t5].[name], [t5].[value2], (
				            SELECT [t7].[name]
				            FROM (
					            SELECT TOP (1) [t6].[name]
					            FROM [sys].[columns] AS [t6]
					            WHERE ([t5].[value2] = [t6].[object_id]) AND ([t5].[value] = [t6].[column_id])
					            ) AS [t7]
				            ) AS [value]
			            FROM (
				            SELECT [t0].[name], (
					            SELECT [t2].[parent_column_id]
					            FROM (
						            SELECT TOP (1) [t1].[parent_column_id]
						            FROM [sys].[foreign_key_columns] AS [t1]
						            WHERE [t0].[object_id] = [t1].[constraint_object_id]
						            ) AS [t2]
					            ) AS [value], (
					            SELECT [t4].[parent_object_id]
					            FROM (
						            SELECT TOP (1) [t3].[parent_object_id]
						            FROM [sys].[foreign_key_columns] AS [t3]
						            WHERE [t0].[object_id] = [t3].[constraint_object_id]
						            ) AS [t4]
					            ) AS [value2]
				            FROM [sys].[foreign_keys] AS [t0]
				            ) AS [t5]
			            ) AS [t8]
		            ) AS [t11]
	            WHERE ([t11].[value2] = '" + tableName + @"') AND ([t11].[value] = '" + columnName + @"')";

            return conn.Query<string>(query);
        }

        private static IEnumerable<string> GetDependentIndexes(this DbConnection conn, string tableName, string columnName)
        {
            // Generated from
            //sys.index_columns.GroupJoin(sys.indexes, o => new {o.object_id, o.index_id}, i => new {i.object_id, i.index_id}, (o, i) => new {
            //    o.object_id,
            //    o.column_id,
            //    IndexName = i.First().name
            //})
            //.GroupJoin(sys.columns, o => new {o.object_id, o.column_id}, i => new {i.object_id, i.column_id}, (o, i) => new {
            //    o.object_id,
            //    o.IndexName,
            //    ColumnName = i.First().name
            //})
            //.GroupJoin(sys.tables, o => o.object_id, i => i.object_id, (o, i) => new {
            //    o.ColumnName,
            //    o.IndexName,
            //    TableName = i.First().name,
            //})
            //.Where(x => x.TableName == "[TableName]" && x.ColumnName == "[ColumnName]")
            //.Select(x => x.IndexName)

            var query = @"
                SELECT [t9].[value]
                FROM (
                    SELECT [t6].[value2], [t6].[value], (
                        SELECT [t8].[name]
                        FROM (
                            SELECT TOP (1) [t7].[name]
                            FROM [sys].[tables] AS [t7]
                            WHERE [t6].[object_id] = [t7].[object_id]
                            ) AS [t8]
                        ) AS [value3]
                    FROM (
                        SELECT [t3].[object_id], [t3].[value], (
                            SELECT [t5].[name]
                            FROM (
                                SELECT TOP (1) [t4].[name]
                                FROM [sys].[columns] AS [t4]
                                WHERE ([t3].[object_id] = [t4].[object_id]) AND ([t3].[column_id] = [t4].[column_id])
                                ) AS [t5]
                            ) AS [value2]
                        FROM (
                            SELECT [t0].[object_id], [t0].[column_id], (
                                SELECT [t2].[name]
                                FROM (
                                    SELECT TOP (1) [t1].[name]
                                    FROM [sys].[indexes] AS [t1]
                                    WHERE ([t0].[object_id] = [t1].[object_id]) AND ([t0].[index_id] = [t1].[index_id])
                                    ) AS [t2]
                                ) AS [value]
                            FROM [sys].[index_columns] AS [t0]
                            ) AS [t3]
                        ) AS [t6]
                    ) AS [t9]
                WHERE ([t9].[value3] = '" + tableName + @"') AND ([t9].[value2] = '" + columnName + @"')";

            return conn.Query<string>(query);
        }

        private static IEnumerable<string> GetDependentPrimaryKey(this DbConnection conn, string tableName, string columnName)
        {
            // Generated from
            //INFORMATION_SCHEMA.KEY_COLUMN_USAGE.GroupJoin(INFORMATION_SCHEMA.TABLE_CONSTRAINTS, o => new {o.TABLE_NAME, o.CONSTRAINT_CATALOG, o.CONSTRAINT_SCHEMA, o.CONSTRAINT_NAME },
            //        i => new {i.TABLE_NAME, i.CONSTRAINT_CATALOG, i.CONSTRAINT_SCHEMA, i.CONSTRAINT_NAME }, (o, i) => new {
            //            o.TABLE_NAME,
            //            o.COLUMN_NAME,
            //            o.CONSTRAINT_NAME,
            //            i.First().CONSTRAINT_TYPE
            //        })
            //        .Where(x => x.CONSTRAINT_TYPE == "PRIMARY KEY" && x.TABLE_NAME == "CourtViolation" && x.COLUMN_NAME == "ViolationID")
            //        .Select(x => x.CONSTRAINT_NAME)

            var query = @"
                SELECT [t3].[CONSTRAINT_NAME]
                FROM (
                    SELECT [t0].[TABLE_NAME], [t0].[COLUMN_NAME], [t0].[CONSTRAINT_NAME], (
                        SELECT [t2].[CONSTRAINT_TYPE]
                        FROM (
                            SELECT TOP (1) [t1].[CONSTRAINT_TYPE]
                            FROM [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] AS [t1]
                            WHERE ([t0].[TABLE_NAME] = [t1].[TABLE_NAME]) AND ([t0].[CONSTRAINT_CATALOG] = [t1].[CONSTRAINT_CATALOG]) AND ([t0].[CONSTRAINT_SCHEMA] = [t1].[CONSTRAINT_SCHEMA]) AND ([t0].[CONSTRAINT_NAME] = [t1].[CONSTRAINT_NAME])
                            ) AS [t2]
                        ) AS [value]
                    FROM [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] AS [t0]
                    ) AS [t3]
                WHERE ([t3].[value] = 'PRIMARY KEY') AND ([t3].[TABLE_NAME] = '" + tableName + @"') AND ([t3].[COLUMN_NAME] = '" + columnName + @"')";

            return conn.Query<string>(query);
        }

        private static string GetForeignKeyName(this DbConnection conn, string tableName, string columnName, string relatedTableName)
        {
            // Generated from
            //sys.foreign_keys.GroupJoin(sys.foreign_key_columns, o => o.object_id, i => i.constraint_object_id, (o, i) => new
            //{
            //    ForeignKeyName = o.name,
            //    column_id = i.First().parent_column_id,
            //    object_id = i.First().parent_object_id,
            //    i.First().referenced_column_id,
            //    i.First().referenced_object_id
            //})
            //.GroupJoin(sys.columns, o => new { o.object_id, o.column_id }, i => new { i.object_id, i.column_id }, (o, i) => new
            //{
            //    o.ForeignKeyName,
            //    o.object_id,
            //    o.referenced_object_id,
            //    ColumnName = i.First().name
            //})
            //.GroupJoin(sys.tables, o => o.object_id, i => i.object_id, (o, i) => new
            //{
            //    o.ForeignKeyName,
            //    o.ColumnName,
            //    o.referenced_object_id,
            //    RelatedTableName = i.First().name,
            //})
            //.GroupJoin(sys.tables, o => o.referenced_object_id, i => i.object_id, (o, i) => new
            //{
            //    o.ForeignKeyName,
            //    o.ColumnName,
            //    o.RelatedTableName,
            //    SourceTableName = i.First().name,
            //})
            //.Where(x => x.SourceTableName == tableName && x.RelatedTableName == relatedTableName && x.ColumnName == columnName)
            //.Select(x => x.ForeignKeyName);

            var query = @"
            SELECT [t16].[name]
            FROM (
                SELECT [t13].[name], [t13].[value], [t13].[value2], (
                    SELECT [t15].[name]
                    FROM (
                        SELECT TOP (1) [t14].[name]
                        FROM [sys].[tables] AS [t14]
                        WHERE [t13].[value3] = [t14].[object_id]
                        ) AS [t15]
                    ) AS [value3]
                FROM (
                    SELECT [t10].[name], [t10].[value], [t10].[value3], (
                        SELECT [t12].[name]
                        FROM (
                            SELECT TOP (1) [t11].[name]
                            FROM [sys].[tables] AS [t11]
                            WHERE [t10].[value2] = [t11].[object_id]
                            ) AS [t12]
                        ) AS [value2]
                    FROM (
                        SELECT [t7].[name], [t7].[value2], [t7].[value3], (
                            SELECT [t9].[name]
                            FROM (
                                SELECT TOP (1) [t8].[name]
                                FROM [sys].[columns] AS [t8]
                                WHERE ([t7].[value2] = [t8].[object_id]) AND ([t7].[value] = [t8].[column_id])
                                ) AS [t9]
                            ) AS [value]
                        FROM (
                            SELECT [t0].[name], (
                                SELECT [t2].[parent_column_id]
                                FROM (
                                    SELECT TOP (1) [t1].[parent_column_id]
                                    FROM [sys].[foreign_key_columns] AS [t1]
                                    WHERE [t0].[object_id] = [t1].[constraint_object_id]
                                    ) AS [t2]
                                ) AS [value], (
                                SELECT [t4].[parent_object_id]
                                FROM (
                                    SELECT TOP (1) [t3].[parent_object_id]
                                    FROM [sys].[foreign_key_columns] AS [t3]
                                    WHERE [t0].[object_id] = [t3].[constraint_object_id]
                                    ) AS [t4]
                                ) AS [value2], (
                                SELECT [t6].[referenced_object_id]
                                FROM (
                                    SELECT TOP (1) [t5].[referenced_object_id]
                                    FROM [sys].[foreign_key_columns] AS [t5]
                                    WHERE [t0].[object_id] = [t5].[constraint_object_id]
                                    ) AS [t6]
                                ) AS [value3]
                            FROM [sys].[foreign_keys] AS [t0]
                            ) AS [t7]
                        ) AS [t10]
                    ) AS [t13]
                ) AS [t16]
            WHERE ([t16].[value3] = '" + tableName + "') AND ([t16].[value2] = '" + relatedTableName + "') AND ([t16].[value] = '" + columnName + "')";

            return conn.Query<string>(query).First();
        }

        private static StringBuilder GetMatchExpression(IDictionary<string, string> dict)
        {
            return dict.Select(x => new StringBuilder("([t0].[").Append(x.Key).Append("] ").Append(GetSQLEqualsValueBuilder(x.Value)).Append(")"))
                    .Aggregate(x => x, (result, x) => result.Append(" AND ").Append(x));
        }

        private static string InsertRowsCommand(string tableName, IEnumerable<IDictionary<string, string>> data, int multiLineNum, IEnumerable<string> columnsToReturn = null, bool indentityInsert = false, IEnumerable<string> columnsToExclude = null, HashSet<string> unquotedColumns = null)
        {
            if (!data.Any())
                return ";";

            tableName = tableName[0] == '[' ? tableName : "[" + tableName + "]";
            unquotedColumns = unquotedColumns ?? new HashSet<string>();
            var stringBuilder = indentityInsert ? new StringBuilder("SET IDENTITY_INSERT ").Append(tableName).Append(" ON").Append(Environment.NewLine) : new StringBuilder();
            var OutPutCommand = columnsToReturn != null ? (new StringBuilder("output ")).Append(columnsToReturn.Aggregate(x => (new StringBuilder("inserted.[")).Append(x).Append("]"),
                (result, x) => result.Append(", ").Append("inserted.[").Append(x).Append("]"))).Append(" into @Result ") : null;
            if (columnsToReturn != null)
                stringBuilder.Append("DECLARE @Result TABLE (").Append(columnsToReturn.Aggregate(x => (new StringBuilder("[")).Append(x).Append("]").Append(" nvarchar(max)"), (result, x) => result.Append(", ")
                        .Append(x).Append(" nvarchar(max)"))).Append(");\r\n");

            if (columnsToExclude != null)
            {
                var ExcludeSet = columnsToExclude.ToHashSet();
                data = data.Select(x => x.Where(y => !ExcludeSet.Contains(y.Key)).ToDictionary(y => y.Key, y => y.Value));
            }
            var ColumnCMD = data.First().Aggregate(x => new StringBuilder("([").Append(x.Key).Append("]"), (result, x) => result.Append(", [").Append(x.Key).Append("]"));

            foreach (var rows in data.ChunkifyToList((list, y) => list.Count != multiLineNum))
            {
                stringBuilder.Append("INSERT INTO ");
                stringBuilder.Append(tableName);
                stringBuilder.Append(" ");
                stringBuilder.Append(ColumnCMD);
                stringBuilder.Append(") ");
                if (columnsToReturn != null)
                    stringBuilder.Append(OutPutCommand);
                stringBuilder.Append(" VALUES\r\n");

                stringBuilder.Append(rows.Select(r => r.Select(x => SQLServer.GetSQLValueBuilder(x.Value, unquotedColumns.Contains(x.Key))).Aggregate(x => new StringBuilder("(").Append(x),
                    (result, x) => result.Append(", ").Append(x)).Append(")"))
                    .Aggregate(x => x, (result, x) => result.Append(",\r\n").Append(x)));

                stringBuilder.Append(";\r\n ");
            }
            if (columnsToReturn != null)
                stringBuilder.Append(";SELECT * FROM @Result;");
            if (indentityInsert)
                stringBuilder.Append("SET IDENTITY_INSERT ").Append(tableName).Append(" OFF;");
            return stringBuilder.Length > 0 ? stringBuilder.ToString() : ";";
        }

        private static string UpdateRowsCommand(string tableName, IEnumerable<string> primaryKeyColumnNames, IEnumerable<IDictionary<string, string>> columns)
        {
            tableName = tableName[0] == '[' ? tableName : "[" + tableName + "]";
            var builder = new StringBuilder();
            foreach (var column in columns)
            {
                builder.Append("UPDATE ").Append(tableName).Append(" SET ").Append(column.Where(x => !primaryKeyColumnNames.Contains(x.Key))
                                        .Select(x => new StringBuilder("[").Append(x.Key).Append("] ").Append(GetSQLSetValueBuilder(x.Value)))
                                        .Aggregate(x => x,
                                            (result, x) => result.Append(", ").Append(x)));

                if (primaryKeyColumnNames.Any())
                    builder.Append(" WHERE ").Append((primaryKeyColumnNames.Select(x => "[" + x + "] = '" + column[x] + "'")
                            .Aggregate(x => x, (result, x) => result + " AND " + x)));

                builder.Append(";\r\n");
            }

            return builder.Length > 0 ? builder.ToString() : ";";
        }

        private static string UpdateRowsCommand(string tableName, IEnumerable<Tuple<IDictionary<string, string>, IDictionary<string, string>>> setWhereDiffs, bool useLike = false)
        {
            tableName = tableName[0] == '[' ? tableName : "[" + tableName + "]";
            var builder = new StringBuilder();
            foreach (var diff in setWhereDiffs)
            {
                builder.Append("UPDATE ").Append(tableName).Append(" SET ").Append(diff.Item1
                                        .Select(x => new StringBuilder("[").Append(x.Key).Append("] ").Append(GetSQLSetValueBuilder(x.Value)))
                                        .Aggregate(x => x,
                                            (result, x) => result.Append(", ").Append(x)));
                if (diff.Item2.Any())
                    builder.Append(" WHERE ").Append(diff.Item2.Select(x => new StringBuilder("[" + x.Key + "] ")
                                .Append(useLike ? GetSQLLikeValueBuilder(x.Value) : GetSQLEqualsValueBuilder(x.Value)))
                            .Aggregate(x => x, (result, x) => result.Append(" AND ").Append(x)));

                builder.Append(";\r\n");
            }

            return builder.Length > 0 ? builder.ToString() : ";";
        }

        public static class SetWhereRow
        {
            public static SetWhereRow<T, S> Create<T, S>(T setData, S whereData)
            {
                return new SetWhereRow<T, S> { SetData = setData, WhereData = whereData };
            }
        }

        public class RelationshipInfo
        {
            public string ColumnName { get; set; }

            public string ForeignKeyName { get; set; }

            public bool IsNullable { get; set; }

            public string ReferencedColumnName { get; set; }

            public string RelatedTable { get; set; }

            public string TableName { get; set; }
        }

        public class SetWhereRow<T, S>
        {
            public T SetData { get; set; }

            public S WhereData { get; set; }
        }
    }
}