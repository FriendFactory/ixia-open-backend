using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Shared.MainDb.Migrations
{
    [DbContext(typeof(WriteDbContext))]
    [Migration("2024_08_30_CharacterSpawnPositionExtend2")]
    public class S_2024_08_30_CharacterSpawnPositionExtend2 : MigrationFromFile;
}