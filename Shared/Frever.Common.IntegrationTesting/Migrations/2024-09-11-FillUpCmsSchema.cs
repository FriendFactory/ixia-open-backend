using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Common.IntegrationTesting.Migrations;

[DbContext(typeof(IntegrationTestMigrationDbContext))]
[Migration("2024-09-11-FillUpCmsSchema")]
public class M_2024_09_11_FillUpCmsSchema : MigrationFromFile { }