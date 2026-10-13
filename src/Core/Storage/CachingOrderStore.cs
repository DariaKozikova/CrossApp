namespace Core.Storage;

using Core.Abstractions;
using Core.Domain;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _fullyLoaded;

    public IReadOnlyList<Order> List()
    {
        if (!_fullyLoaded)
        {
            var items = _inner.List();
            foreach (var item in items)
            {
                _cache[item.Id] = item;
            }
            _fullyLoaded = true;
        }

        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var item = _inner.GetById(id);
        if (item != null)
        {
            _cache[item.Id] = item;
        }

        return item;
    }

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (!_fullyLoaded)
        {
            List();
        }

        return _cache.Values.Where(predicate).ToList();
    }

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _inner.Add(item);
        _cache[item.Id] = item;
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _inner.Update(item);
        _cache[item.Id] = item;
    }

    public bool Remove(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var removed = _inner.Remove(id);
        if (removed)
        {
            _cache.Remove(id);
        }
        return removed;
    }
}
