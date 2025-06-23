using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Currency : IEntity
{
    public long Id { get; set; }
    public string Isoname { get; set; }
    public string DisplayName { get; set; }
    public string Symbol { get; set; }
}