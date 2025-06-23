using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Common.Infrastructure.Database.Migrations;

public class MigrationFromFolder : MigrationBase
{
    protected sealed override void Up(MigrationBuilder migrationBuilder)
    {
        try
        {
            var attr = (MigrationAttribute) GetType().GetCustomAttribute(typeof(MigrationAttribute));
            if (attr == null)
                return;

            var fileExtension = "*.sql";
            var folderName = string.Join('_', attr.Id.Split('_').Skip(1).ToArray());

            var mainFolder = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Migrations", "MigrationFile");

            var path = Path.Join(mainFolder, folderName);

            var files = Directory.EnumerateFiles(path, fileExtension).OrderBy(x => x);
            var sb = new StringBuilder();
            foreach (var file in files)
                sb.AppendJoin(";\n\r", File.ReadAllText(file));

            var resultQuery = PrepareQuery(sb.ToString()); //string builder cant replace with ignore case  so use string;

            migrationBuilder.Sql(resultQuery);
        }
        catch (Exception ex) when (ex.GetType() != typeof(MigrationFileNotFoundException))
        {
            var typeName = GetType().Name;

            throw new Exception($"Failed apply migration {typeName}.\n\r {ex.Message}", ex);
        }
    }
}