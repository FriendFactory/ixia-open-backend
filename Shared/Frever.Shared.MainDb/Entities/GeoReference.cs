using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class GeoReference : IEntity
{
    public long Id { get; set; }
    public string Name { get; set; }
}