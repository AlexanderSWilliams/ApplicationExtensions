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
        public static StringBuilder NullBuilder = new StringBuilder("NULL");

        public enum NullableOption
        {
            OnlyNullable,
            OnlyNonNullable,
            Both
        }

        public static string AutoBackupDBCommand(this DbConnection conn, string sqlServerPath, string dbName, string stats = "10")
        {
            var sourceLogicalFileName = GetLogicalFileNameFromDB(conn, dbName);
            if (sourceLogicalFileName != dbName)
            {
                var query = @"ALTER DATABASE [" + dbName + @"] MODIFY FILE (Name = '" + sourceLogicalFileName + @"', NEWNAME = [" + dbName + @"]);
                ALTER DATABASE [" + dbName + @"] MODIFY FILE (Name = '" + sourceLogicalFileName + @"_log',NEWNAME = [" + dbName + @"_log]);";

                conn.Execute(query);
            }

            return conn.Query<string>(@"BACKUP DATABASE [" + dbName + @"] TO  DISK = N'" + sqlServerPath + @"\AutoBackup\" + dbName + @".bak' WITH FORMAT, INIT,  NAME = N'" + dbName + @"-Full Database Backup', NOREWIND, NOUNLOAD,  STATS = " + stats + @"").First();
        }

        public static string BackupDBCommand(this DbConnection conn, string sqlServerPath, string dbName, string stats = "10")
        {
            var sourceLogicalFileName = GetLogicalFileNameFromDB(conn, dbName);

            if (sourceLogicalFileName != dbName)
            {
                var query = (@"ALTER DATABASE [" + dbName + @"] MODIFY FILE (Name = '" + sourceLogicalFileName + @"', NEWNAME = [" + dbName + @"]);
                ALTER DATABASE [" + dbName + @"] MODIFY FILE (Name = '" + sourceLogicalFileName + @"_log',NEWNAME = [" + dbName + @"_log]);");
                conn.Execute(query);
            }

            return conn.Query<string>(@"BACKUP DATABASE [" + dbName + @"] TO  DISK = N'" + sqlServerPath + @"\Backup\" + dbName + @".bak' WITH FORMAT, INIT,  NAME = N'" + dbName + @"-Full Database Backup', NOREWIND, NOUNLOAD,  STATS = " + stats + @"").First();
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

        public static string DeleteAllTablesCommand(this DbConnection conn, string[] tablePrefixNamesToOmit, string mandatoryPrefix = null)
        {
            var OrderedToDeleleTables = GetTableNamessInOrderOfValidDeletion(conn, tablePrefixNamesToOmit, mandatoryPrefix);

            return OrderedToDeleleTables.Aggregate(x => new StringBuilder("DELETE FROM [").Append(x).Append("]"), (result, x) => result.Append(System.Environment.NewLine).Append("DELETE FROM [").Append(x).Append("]")).ToString();
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

        public static IEnumerable<string> GetColumnNames(this DbConnection conn, string tableName)
        {
            return conn.Query<string>(@"select
               syscolumns.name as [Column]
            from
               sysobjects, syscolumns
            where sysobjects.id = syscolumns.id
            and   sysobjects.xtype = 'u'
            and   sysobjects.name = '" + tableName + @"'
            order by syscolumns.name");
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

        public static IEnumerable<string> GetDepedentTables(DbConnection conn, string tableName,
            NullableOption nullableOption, int depth = int.MaxValue)
        {
            var Relationships = SQLServer.GetAllRelationships(conn);
            if (nullableOption == NullableOption.OnlyNullable)
                Relationships = Relationships.Where(x => x.IsNullable).ToList();
            if (nullableOption == NullableOption.OnlyNonNullable)
                Relationships = Relationships.Where(x => !x.IsNullable).ToList();

            depth--;
            var Result = new HashSet<string>(Relationships.Where(x => !x.IsNullable && x.RelatedTable == tableName).Select(x => x.TableName));

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

        public static string GetLogicalFileNameFromBak(this DbConnection conn, string bakName, string sqlServerPath)
        {
            var query = @"DECLARE @Table TABLE (LogicalName varchar(128),[PhysicalName] varchar(128), [Type] varchar, [FileGroupName] varchar(128), [Size] varchar(128),
            [MaxSize] varchar(128), [FileId]varchar(128), [CreateLSN]varchar(128), [DropLSN]varchar(128), [UniqueId]varchar(128), [ReadOnlyLSN]varchar(128), [ReadWriteLSN]varchar(128),
            [BackupSizeInBytes]varchar(128), [SourceBlockSize]varchar(128), [FileGroupId]varchar(128), [LogGroupGUID]varchar(128), [DifferentialBaseLSN]varchar(128), [DifferentialBaseGUID]varchar(128), [IsReadOnly]varchar(128), [IsPresent]varchar(128), [TDEThumbprint]varchar(128)
)
DECLARE @Path varchar(1000)='" + sqlServerPath + @"\Backup\" + bakName + @".bak'
DECLARE @LogicalNameData varchar(128),@LogicalNameLog varchar(128)
INSERT INTO @table
EXEC('
RESTORE FILELISTONLY
   FROM DISK=''' +@Path+ '''
   ')

   SET @LogicalNameData=(SELECT LogicalName FROM @Table WHERE Type='D')
   SET @LogicalNameLog=(SELECT LogicalName FROM @Table WHERE Type='L')

SELECT @LogicalNameData";

            return conn.Query<string>(query).First();
        }

        public static string GetLogicalFileNameFromDB(this DbConnection conn, string databaseName)
        {
            var query = @"select top 1 name from [" + databaseName + @"].sys.database_files";

            return conn.Query<string>(query, conn).First();
        }

        public static string GetPrimaryKeyColumn(this DbConnection conn, string tableName)
        {
            // Generated from
            //INFORMATION_SCHEMA.KEY_COLUMN_USAGE.GroupJoin(INFORMATION_SCHEMA.TABLE_CONSTRAINTS, o => new {o.TABLE_NAME, o.CONSTRAINT_CATALOG, o.CONSTRAINT_SCHEMA, o.CONSTRAINT_NAME },
            //        i => new {i.TABLE_NAME, i.CONSTRAINT_CATALOG, i.CONSTRAINT_SCHEMA, i.CONSTRAINT_NAME }, (o, i) => new {
            //            o.TABLE_NAME,
            //            o.COLUMN_NAME,
            //            i.First().CONSTRAINT_TYPE
            //        })
            //        .Where(x => x.CONSTRAINT_TYPE == "PRIMARY KEY" && x.TABLE_NAME == "TableName")
            //        .Select(x => x.COLUMN_NAME)

            var query = @"
                    SELECT [t3].[COLUMN_NAME]
                    FROM (
                        SELECT [t0].[TABLE_NAME], [t0].[COLUMN_NAME], (
                            SELECT [t2].[CONSTRAINT_TYPE]
                            FROM (
                                SELECT TOP (1) [t1].[CONSTRAINT_TYPE]
                                FROM [INFORMATION_SCHEMA].[TABLE_CONSTRAINTS] AS [t1]
                                WHERE ([t0].[TABLE_NAME] = [t1].[TABLE_NAME]) AND ([t0].[CONSTRAINT_CATALOG] = [t1].[CONSTRAINT_CATALOG]) AND ([t0].[CONSTRAINT_SCHEMA] = [t1].[CONSTRAINT_SCHEMA]) AND ([t0].[CONSTRAINT_NAME] = [t1].[CONSTRAINT_NAME])
                                ) AS [t2]
                            ) AS [value]
                        FROM [INFORMATION_SCHEMA].[KEY_COLUMN_USAGE] AS [t0]
                        ) AS [t3]
                    WHERE ([t3].[value] = 'PRIMARY KEY') AND ([t3].[TABLE_NAME] = '" + tableName + "')";

            return conn.Query<string>(query).First();
        }

        public static StringBuilder GetSQLValueBuilder(string value)
        {
            return value != null ? new StringBuilder("'").Append(value.Replace("'", "''")).Append("'") : SQLServer.NullBuilder;
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

        public static IEnumerable<string> GetTableNamessInOrderOfValidDeletion(this DbConnection conn, string[] tablePrefixNamesToOmit, string mandatoryPrefix = null)
        {
            var Relationships = SQLServer.GetAllRelationships(conn);

            var TablesWithRelatedTables = Relationships
                .Where(x => !x.IsNullable || tablePrefixNamesToOmit.Contains(x.TableName))
                .GroupBy(x => x.TableName)
                .Select(x => new
                {
                    TableName = x.Key,
                    RelatedTables = x.Select(y => y.RelatedTable).Distinct()
                }).ToList();

            var TableNamesToDelete = mandatoryPrefix != null ? SQLServer.GetTableNames(conn).Where(x => x.StartsWith(mandatoryPrefix) && !tablePrefixNamesToOmit.Any(y => x.StartsWith(y))).ToList() :
                SQLServer.GetTableNames(conn).Where(x => !tablePrefixNamesToOmit.Any(y => x.StartsWith(y))).ToList();
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

        public static string InsertRowsCommand(string tableName, IEnumerable<object> data, IEnumerable<string> columnsToReturn = null, bool indentityInsert = false, IEnumerable<string> columnsToExclude = null)
        {
            return InsertRowsCommand(tableName, data.Select(x => x.ToStringStringDictionary()), columnsToReturn, indentityInsert, columnsToExclude);
        }

        public static string MergeRowsCommand(string tableName, string primaryKeyColumnName, IEnumerable<object> data, string setCMD = null)
        {
            var dictData = data.Select(x => x.ToStringStringDictionary());

            var TempTableName = "__" + tableName + "Temp";
            var columnNames = dictData.Select(x => x.Keys).First();
            var columnNamesDelimited = columnNames.Aggregate(x => new StringBuilder(x), (result, x) => result.Append(", ").Append(x));
            var setColumnsCommand = columnNames.Where(x => x != primaryKeyColumnName)
                                        .Aggregate(x => new StringBuilder("[").Append(tableName).Append("].").Append(x).Append(" = [").Append(TempTableName).Append("].").Append(x),
                                            (result, x) => result.Append(", [").Append(tableName).Append("].").Append(x).Append(" = [").Append(TempTableName).Append("].").Append(x));

            var builder = new StringBuilder("BEGIN TRANSACTION\r\nSELECT TOP 0 ").Append(columnNamesDelimited).Append(" INTO [").Append(TempTableName).Append("] from [").Append(tableName)
                .Append(@"]

					SET IDENTITY_INSERT [").Append(TempTableName).Append("] ON").Append(System.Environment.NewLine)
                                    .Append(SQLServer.InsertRowsCommand(TempTableName, dictData))
                                    .Append("SET IDENTITY_INSERT [").Append(TempTableName).Append(@"] OFF;");

            if (setCMD != null)
                builder.Append("UPDATE [").Append(TempTableName).Append("] ").Append(setCMD).Append(";");

            builder.Append("UPDATE [").Append(tableName).Append("] SET ").Append(setColumnsCommand).Append(" FROM [").Append(TempTableName).Append("] INNER JOIN [").Append(tableName).Append("] ON [").Append(TempTableName)
                    .Append("].[").Append(primaryKeyColumnName).Append(@"] = [").Append(tableName).Append(@"].[").Append(primaryKeyColumnName).Append(@"];

					SET IDENTITY_INSERT [").Append(tableName).Append(@"] ON
					INSERT INTO [")
                    .Append(tableName).Append(@"] (").Append(columnNamesDelimited).Append(") SELECT ").Append(columnNamesDelimited).Append(" FROM [").Append(TempTableName).Append(@"] AS [t0]
						WHERE NOT (EXISTS(
							SELECT TOP (1) NULL AS [EMPTY]
							FROM [").Append(tableName).Append(@"] AS [t1]
							WHERE [t0].[").Append(primaryKeyColumnName).Append(@"] = [t1].[").Append(primaryKeyColumnName).Append(@"]));
					SET IDENTITY_INSERT [").Append(tableName).Append(@"] OFF

					DROP TABLE [").Append(TempTableName).Append("];\r\nCOMMIT TRANSACTION;");

            return builder.Length > 0 ? builder.ToString() : ";";
        }

        public static void RestoreDB(this DbConnection conn, string sqlServerPath, string sourceBak, string destinationDB, string stats = "5", bool assumeSourceLogicalFileNameIsCorrect = true)
        {
            var sourceLogicalFileName = GetLogicalFileNameFromBak(conn, sourceBak, sqlServerPath);

            if (!assumeSourceLogicalFileNameIsCorrect)
            {
                if (sourceLogicalFileName != sourceBak)
                {
                    conn.Execute(@"ALTER DATABASE [" + sourceBak + @"] MODIFY FILE (Name = '" + sourceLogicalFileName + @"', NEWNAME = [" + sourceBak + @"]);
                ALTER DATABASE [" + sourceBak + @"] MODIFY FILE (Name = '" + sourceLogicalFileName + @"_log',NEWNAME = [" + sourceBak + @"_log]);");
                }
            }
            conn.Execute(@"IF  NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'" + destinationDB + @"')
                        CREATE DATABASE [" + destinationDB + @"]");

            conn.Execute(@"ALTER DATABASE [" + destinationDB + @"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
            RESTORE DATABASE [" + destinationDB + @"] FROM  DISK = N'" + sqlServerPath + @"\Backup\" + sourceBak + @".bak' WITH MOVE N'" + sourceLogicalFileName + @"' TO N'" + sqlServerPath + @"\DATA\" + destinationDB + @".mdf', MOVE N'" + sourceLogicalFileName + @"_log' TO N'" + sqlServerPath + @"\DATA\" + destinationDB + @"_log.ldf',  NOUNLOAD,  REPLACE,  STATS = " + stats + @"
            ALTER DATABASE [" + destinationDB + @"] SET MULTI_USER ");

            var destinationLogicalFileName = GetLogicalFileNameFromDB(conn, destinationDB);
            if (destinationLogicalFileName != destinationDB)
            {
                conn.Execute(@"ALTER DATABASE [" + destinationDB + @"] MODIFY FILE (Name = '" + destinationLogicalFileName + @"', NEWNAME = " + destinationDB + @");
				ALTER DATABASE [" + destinationDB + @"] MODIFY FILE (Name = '" + destinationLogicalFileName + @"_log',NEWNAME = " + destinationDB + @"_log);");
            }
        }

        public static string UpdateRowsCommand(string tableName, string primaryKeyColumnName, IEnumerable<object> columns)
        {
            return UpdateRowsCommand(tableName, primaryKeyColumnName, columns.Select(x => x.ToStringStringDictionary()));
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
            return dict.Aggregate(x => (new StringBuilder("([t0].[")).Append(x.Key).Append("] = ").Append(GetSQLValueBuilder(x.Value)),
                (result, x) => result.Append(" AND [t0].[").Append(x.Key).Append("] = ").Append(GetSQLValueBuilder(x.Value)).Append(")"));
        }

        private static string InsertRowsCommand(string tableName, IEnumerable<IDictionary<string, string>> data, IEnumerable<string> columnsToReturn = null, bool indentityInsert = false, IEnumerable<string> columnsToExclude = null)
        {
            var stringBuilder = indentityInsert ? new StringBuilder("SET IDENTITY_INSERT [").Append(tableName).Append("] ON").Append(Environment.NewLine) : new StringBuilder();
            var OutPutCommand = columnsToReturn != null ? (new StringBuilder("output ")).Append(columnsToReturn.Aggregate(x => (new StringBuilder("inserted.[")).Append(x).Append("]"),
                (result, x) => result.Append(", ").Append("inserted.[").Append(x).Append("]"))).Append(" into @Result ") : null;
            if (columnsToReturn != null)
                stringBuilder.Append("DECLARE @Result TABLE (").Append(columnsToReturn.Aggregate(x => (new StringBuilder("[")).Append(x).Append("]").Append(" nvarchar(max)"), (result, x) => result.Append(", ")
                        .Append(x).Append(" nvarchar(max)"))).Append(");");
            foreach (IDictionary<string, string> dict in data)
            {
                var row = columnsToExclude != null ? dict.Where(x => !columnsToExclude.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value) : dict;
                stringBuilder.Append("INSERT INTO [");
                stringBuilder.Append(tableName);
                stringBuilder.Append("] ");
                stringBuilder.Append(row.Aggregate(x => new StringBuilder("([").Append(x.Key).Append("]"), (result, x) => result.Append(", [").Append(x.Key).Append("]")));
                stringBuilder.Append(") ");
                if (columnsToReturn != null)
                    stringBuilder.Append(OutPutCommand);
                stringBuilder.Append(" VALUES ");
                stringBuilder.Append(row.Aggregate(x => new StringBuilder("(").Append(SQLServer.GetSQLValueBuilder(x.Value)),
                    (result, x) => result.Append(", ").Append(SQLServer.GetSQLValueBuilder(x.Value))));
                stringBuilder.Append(") ");
            }
            if (columnsToReturn != null)
                stringBuilder.Append(";SELECT * FROM @Result;");
            if (indentityInsert)
                stringBuilder.Append("SET IDENTITY_INSERT [").Append(tableName).Append("] OFF");
            return stringBuilder.Length > 0 ? stringBuilder.ToString() : ";";
        }

        private static string UpdateRowsCommand(string tableName, string primaryKeyColumnName, IEnumerable<IDictionary<string, string>> columns)
        {
            var builder = new StringBuilder();
            foreach (var column in columns)
            {
                builder.Append("UPDATE [").Append(tableName).Append("] SET ").Append(column.Where(x => x.Key != primaryKeyColumnName)
                                        .Aggregate(x => new StringBuilder("[").Append(x.Key).Append("] = ").Append(GetSQLValueBuilder(x.Value)),
                                            (result, x) => result.Append(", ").Append("[").Append(x.Key).Append("] = ").Append(GetSQLValueBuilder(x.Value))))
                        .Append(" WHERE [").Append(primaryKeyColumnName).Append("] = ").Append(column[primaryKeyColumnName])
                        .Append(';');
            }

            return builder.Length > 0 ? builder.ToString() : ";";
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
    }
}