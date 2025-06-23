namespace Frever.Cache.Supplement;

public interface ICurrentGroupAccessor
{
    long? CurrentGroupId { get; }
}