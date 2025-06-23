using Microsoft.EntityFrameworkCore;

namespace Frever.Utils.TableDataCloneScriptGenerator.SqlGeneration;

public class SqlGenerator(Configuration configuration)
{
    public async Task<string> GenerateExportSql()
    {
        var cols = await LoadColumns(configuration.TableName);
        return GenerateInsert(cols);
    }

    private string GenerateInsert(InformationSchemaColumns[] cols)
    {
        var colsList = string.Join(", ", cols.Select(c => $"\"{c.ColumnName}\""));

        var insertValues = string.Join(" || ', ' || ", cols.Select(ColumnValueAccessExpression));

        var where = String.Empty;
        if (!String.IsNullOrWhiteSpace(configuration.ExtraWhereCondition))
        {
            where = $"\nwhere {configuration.ExtraWhereCondition}";
        }

        return $"""
                select 
                'insert into "{configuration.TableName}" ({colsList}) overriding system value ' ||
                'values (' ||
                {insertValues} ||
                ')' ||
                ' on conflict {GenerateOnConflict(cols)};'
                from "{configuration.TableName}" {where}
                """;
    }

    private string GenerateOnConflict(InformationSchemaColumns[] cols)
    {
        if (configuration.Mode == GenerationMode.InsertOnly)
            return "do nothing";

        var idCol = cols.Single(c => c.ColumnName.Equals(configuration.IdColumn));
        var updatableCols = cols.Where(c => !c.ColumnName.Equals(configuration.IdColumn));

        var updateValues = string.Join(", ", updatableCols.Select(c => $"\"{c.ColumnName}\" = EXCLUDED.\"{c.ColumnName}\" "));

        return $"""
                ("{idCol.ColumnName}") do
                update
                set {updateValues}
                """;
    }

    private string ColumnValueAccessExpression(InformationSchemaColumns column)
    {
        switch (column.DataType)
        {
            case "int8":
            case "int4":
            case "bool":
                return $"coalesce(\"{column.ColumnName}\"::text, 'null')";
            case "text":
            case "json":
            case "_int4":
            case "_float4":
            case "_int8":
            case "timestamptz":
            case "timestamp":
            case "uuid":
                return $"quote_nullable(\"{column.ColumnName}\"::text)";
            case "bytea":
                return $"quote_nullable(\"{column.ColumnName}\"::text)";
        }

        throw new InvalidOperationException(
            $"Data type {column.DataType} for {configuration.TableName}.{column.ColumnName} is not supported"
        );
    }

    private async Task<InformationSchemaColumns[]> LoadColumns(string tableName)
    {
        await using var db = CreateDbContext();
        var cols = await db.Database.SqlQueryRaw<InformationSchemaColumns>(
                                $"""
                                 select column_name "ColumnName",
                                        case
                                            when is_nullable = 'NO' then false
                                            else true
                                            end     "IsNullable",
                                         udt_name "DataType"
                                 from information_schema.columns
                                 where table_name = '{tableName}';
                                 """
                            )
                           .ToArrayAsync();

        if (cols.Length == 0)
            throw new InvalidOperationException($"Table {tableName} does not exists or have no columns");

        return cols;
    }

    private MyDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>().UseNpgsql(configuration.ConnectionString);

        return new MyDbContext(optionsBuilder.Options);
    }
}

public class InformationSchemaColumns
{
    public string ColumnName { get; set; }

    public bool IsNullable { get; set; }

    public string DataType { get; set; }
}