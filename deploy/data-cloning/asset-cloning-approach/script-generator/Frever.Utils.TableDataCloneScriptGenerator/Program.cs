using System.CommandLine;
using Frever.Utils.TableDataCloneScriptGenerator.SqlGeneration;
using Newtonsoft.Json;

namespace Frever.Utils.TableDataCloneScriptGenerator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var command = Cli.Run(Run);
        await command.InvokeAsync(args);
    }

    private static async Task Run(Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var generator = new SqlGenerator(configuration);

        try
        {
            var sql = await generator.GenerateExportSql();
            Console.WriteLine(sql);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            Console.Error.WriteLine(JsonConvert.SerializeObject(configuration));
            Environment.Exit(-1);
        }
    }
}