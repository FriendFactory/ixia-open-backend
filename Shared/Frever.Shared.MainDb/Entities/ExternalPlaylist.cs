using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class ExternalPlaylist : IEntity, IAdminAsset, IStageable
{
    public string ExternalPlaylistId { get; set; }
    public int SortOrder { get; set; }
    public string[] Countries { get; set; }
    public long Id { get; set; }
    public long ReadinessId { get; set; }
    public Readiness Readiness { get; set; }
}