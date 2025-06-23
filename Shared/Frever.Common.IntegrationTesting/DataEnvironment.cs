using Frever.Shared.MainDb;

namespace Frever.Common.IntegrationTesting;

public partial class DataEnvironment
{
    public DataEnvironment(WriteDbContext mainDb)
    {
        var dict = new Dictionary<string, string>();
        SqlScrips = dict;

        var files = new HashSet<string>(FindAllSqlInFolder(CurrentTestFolder));
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            dict[file] = text;
        }

        Db = mainDb;
    }

    private IReadOnlyDictionary<string, string> SqlScrips { get; }

    /// <summary>
    ///     Gets a db context for the instance.
    ///     Can be used to seed data by filling up corresponding db set.
    ///     Automatically opens a test-wide transaction before saving.
    /// </summary>
    public WriteDbContext Db { get; }

    private string CurrentTestFolder => Path.GetDirectoryName(GetType().Assembly.Location);

    private string LookupSqlScriptByNamePart(string namePart)
    {
        var matching = SqlScrips.Keys.Where(k => k.Contains(namePart)).ToArray();

        if (matching.Length > 1)
        {
            var all = string.Join(";", matching.Select(p => p[CurrentTestFolder.Length..]));
            throw new ArgumentException($"Value '{namePart}' matching several SQL scripts: {all}", nameof(namePart));
        }

        if (matching.Length == 0)
            throw new ArgumentException($"Value '{namePart}' doesn't match any SQL script", nameof(namePart));

        return matching[0];
    }

    private static IEnumerable<string> FindAllSqlInFolder(string folder)
    {
        foreach (var file in Directory.GetFiles(folder, "*.sql"))
            yield return file;

        foreach (var subFolder in Directory.GetDirectories(folder))
            foreach (var file in FindAllSqlInFolder(subFolder))
                yield return file;
    }
}