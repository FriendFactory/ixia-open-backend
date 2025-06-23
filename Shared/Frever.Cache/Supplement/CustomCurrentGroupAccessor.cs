using System;

namespace Frever.Cache.Supplement;

public class CustomCurrentGroupAccessor(Func<IServiceProvider, long?> accessCurrentGroup, IServiceProvider serviceProvider)
    : ICurrentGroupAccessor
{
    private readonly Func<IServiceProvider, long?> _accessCurrentGroup =
        accessCurrentGroup ?? throw new ArgumentNullException(nameof(accessCurrentGroup));

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public long? CurrentGroupId => _accessCurrentGroup(_serviceProvider);
}