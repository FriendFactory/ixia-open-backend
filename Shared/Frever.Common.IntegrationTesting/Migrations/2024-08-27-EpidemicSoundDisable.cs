using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Common.IntegrationTesting.Migrations;

[DbContext(typeof(IntegrationTestMigrationDbContext))]
[Migration("2024-08-27-EpidemicSoundDisable")]
public class M_2024_08_27_EpidemicSoundDisable : MigrationFromFile { }