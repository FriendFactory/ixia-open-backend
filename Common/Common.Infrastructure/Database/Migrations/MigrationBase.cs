using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Common.Infrastructure.Database.Migrations;

public abstract class MigrationBase : Migration
{
    protected virtual string PrepareQuery(string sqlQuery)
    {
        return sqlQuery.Replace("begin;", "", true, CultureInfo.CurrentCulture).Replace("commit", "", true, CultureInfo.CurrentCulture);
    }
}