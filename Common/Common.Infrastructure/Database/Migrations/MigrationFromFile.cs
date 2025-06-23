using System;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Common.Infrastructure.Database.Migrations;

public class MigrationFromFile : MigrationBase
{
    protected sealed override void Up(MigrationBuilder migrationBuilder)
    {
        try
        {
            var attr = (MigrationAttribute) GetType().GetCustomAttribute(typeof(MigrationAttribute));
            if (attr == null)
                return;

            var fileExtension = ".sql";

            var filename = attr.Id + fileExtension;

            var subFolder = Path.Join("Migrations", "MigrationFile");
            var folder = AppDomain.CurrentDomain.BaseDirectory + subFolder;

            var path = Path.Join(folder, filename);

            if (!File.Exists(path))
                throw new MigrationFileNotFoundException(filename, folder);

            var sqlQuery = File.ReadAllText(path);

            sqlQuery = PrepareQuery(sqlQuery);

            migrationBuilder.Sql(sqlQuery);
        }
        catch (Exception ex) when (ex.GetType() != typeof(MigrationFileNotFoundException))
        {
            var typeName = GetType().Name;

            throw new Exception($"Failed apply migration {typeName}.\n\r {ex.Message}", ex);
        }
    }
}

public class MigrationFileNotFoundException(string fileName, string folder) : Exception(string.Format(messageFormat, fileName, folder))
{
    protected const string messageFormat =
        "Migration file {0} not found.\n\r Developer hint:Set File property 'copy To output directory = copy always' \n\r  File name:{0} folder:{1}";

    public string FileName { get; } = fileName;
    public string Folder { get; } = folder;
}