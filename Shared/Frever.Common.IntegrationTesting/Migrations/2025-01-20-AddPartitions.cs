using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Common.IntegrationTesting.Migrations;

[DbContext(typeof(IntegrationTestMigrationDbContext))]
[Migration("2025-01-20-AddPartitions")]
public class M_2025_01_20_AddPartitions : MigrationFromFile;