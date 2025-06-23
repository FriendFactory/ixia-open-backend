using Common.Infrastructure.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frever.Shared.MainDb.Migrations
{
    [DbContext(typeof(WriteDbContext))]
    [Migration("2025_03_19_AiSpeakerAndLanguageMode")]
    public class S_2025_03_19_AiSpeakerAndLanguageMode : MigrationFromFile;
}