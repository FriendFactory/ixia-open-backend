using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Shared.MainDb.Migrations
{
    [DbContext(typeof(WriteDbContext))]
    [Migration("2024_05_21_LocalizationIsStartupItem")]
    public class S_2024_05_21_LocalizationIsStartupItem : MigrationFromFile;
}