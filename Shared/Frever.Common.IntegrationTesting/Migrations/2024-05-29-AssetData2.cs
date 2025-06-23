using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Common.IntegrationTesting.Migrations;

[DbContext(typeof(IntegrationTestMigrationDbContext))]
[Migration("2024-05-29-AssetData2")]
public class M_2024_05_29_AssetData2 : MigrationFromFile { }