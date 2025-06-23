using System.CommandLine;

namespace Frever.Utils.TableDataCloneScriptGenerator;

public class Configuration
{
    public string TableName { get; set; }

    public string ConnectionString { get; set; }

    public string ExtraWhereCondition { get; set; }

    public string IdColumn { get; set; }

    public GenerationMode Mode { get; set; } = GenerationMode.InsertAndUpdate;
}

public enum GenerationMode
{
    InsertOnly,
    InsertAndUpdate
}

public static class Cli
{
    public static RootCommand Run(Func<Configuration, Task> run)
    {
        var rootCommand = new RootCommand("Generate Postgres SQL script to export data from table");

        var tableNameOption = new Option<string>(["--table", "-t"], "Table name to generate inserts");
        var connectionStringOption = new Option<string>(["--connection", "-c"], "Database connection string");
        var extraWhereConditionOption = new Option<string>(
            ["--where", "-w"],
            () => string.Empty,
            "Extra where condition to filter exported table data"
        );
        var generationModeOption = new Option<string>(["--mode", "-m"], () => String.Empty, "Generation mode");
        var idColumnOption = new Option<string>(["--id", "-i"], () => string.Empty, "Name of the identifier column");

        rootCommand.AddOption(tableNameOption);
        rootCommand.AddOption(connectionStringOption);
        rootCommand.AddOption(extraWhereConditionOption);
        rootCommand.AddOption(generationModeOption);
        rootCommand.AddOption(idColumnOption);

        rootCommand.SetHandler(
            async (
                tableName,
                connectionString,
                extraWhere,
                modeName,
                id
            ) =>
            {
                var mode = GenerationMode.InsertAndUpdate;

                if (!String.IsNullOrWhiteSpace(modeName))
                {
                    mode = Enum.Parse<GenerationMode>(modeName);
                }

                if (string.IsNullOrWhiteSpace(id))
                    id = "Id";

                var config = new Configuration
                             {
                                 TableName = tableName,
                                 Mode = mode,
                                 ConnectionString = connectionString,
                                 ExtraWhereCondition = extraWhere,
                                 IdColumn = id
                             };

                await run(config);
            },
            tableNameOption,
            connectionStringOption,
            extraWhereConditionOption,
            generationModeOption,
            idColumnOption
        );

        return rootCommand;
    }
}