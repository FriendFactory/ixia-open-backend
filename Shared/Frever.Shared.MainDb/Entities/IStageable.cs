namespace Frever.Shared.MainDb.Entities
{
    public interface IStageable
    {
        long ReadinessId { get; set; }
        Readiness Readiness { get; set; }
    }
}