using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Common.IntegrationTesting.Migrations;

[DbContext(typeof(IntegrationTestMigrationDbContext))]
[Migration("2024-05-11-AssetData")]
public class M_2024_05_11_AssetData : MigrationFromFile { }