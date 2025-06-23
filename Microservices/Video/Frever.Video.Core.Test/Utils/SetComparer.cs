namespace Frever.Video.Core.Test.Utils;

public class HashSetEqualityComparer<TElement> : IEqualityComparer<ISet<TElement>>, IEqualityComparer<TElement[]>
{
    private readonly IEqualityComparer<TElement> _elementComparer = EqualityComparer<TElement>.Default;

    public HashSetEqualityComparer() { }

    public HashSetEqualityComparer(IEqualityComparer<TElement> elementComparer)
    {
        _elementComparer = elementComparer ?? throw new ArgumentNullException(nameof(elementComparer));
    }

    public bool Equals(ISet<TElement> x, ISet<TElement> y)
    {
        if (x.Count != y.Count)
            return false;

        foreach (var element in x)
            if (!y.Contains(element, _elementComparer))
                return false;

        return true;
    }

    public int GetHashCode(ISet<TElement> obj)
    {
        return obj.GetHashCode();
    }

    public bool Equals(TElement[] x, TElement[] y)
    {
        var setX = new HashSet<TElement>(x);
        var setY = new HashSet<TElement>(y);

        return Equals(setX, setY);
    }

    public int GetHashCode(TElement[] obj)
    {
        return obj.GetHashCode();
    }
}