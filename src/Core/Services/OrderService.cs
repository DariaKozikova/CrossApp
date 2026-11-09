namespace Core.Services;

using Core.Abstractions;
using Core.Domain;
using Core.Events;

public sealed class OrderService(IOrderStore store, INotificationSink? sink = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _sink = sink;

    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(Order order, OrderChangeKind kind)
    {
        var args = new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total);
        Changed?.Invoke(this, args);
        _sink?.Notify(args);
    }

    public Order CreateOrder(string customerId, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        int openOrders = _store.List()
            .Count(o => o.CustomerId.Equals(customerId.Trim(), StringComparison.OrdinalIgnoreCase) && o.Status == OrderStatus.Draft);

        if (openOrders >= 5)
        {
            throw new DomainException(
                $"Клієнт {customerId} уже має {openOrders} відкритих замовлень (максимум 5). Спочатку підтвердіть або скасуйте їх.");
        }

        var orderId = string.IsNullOrWhiteSpace(id)
            ? Guid.NewGuid().ToString("N")[..8]
            : id.Trim();

        var order = Order.Create(orderId, customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated);
        return order;
    }

    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new DomainException($"Немає запису з id={orderId}.");
        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new DomainException($"Немає запису з id={orderId}.");
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
    }

    public void Cancel(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new DomainException($"Немає запису з id={orderId}.");
        order.Cancel();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderCancelled);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.Find(predicate);
    }

    public IReadOnlyList<Order> FindAll(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    public Task<int> ImportAsync(
        IOrderImporter importer,
        string path,
        CancellationToken ct = default) =>
        ImportAsync(importer, path, progress: null, ct);

    public async Task<int> ImportAsync(
        IOrderImporter importer,
        string path,
        IProgress<int>? progress,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        var total = items.Count;

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var item = items[i];
            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item, OrderChangeKind.OrderCreated);
                added++;
            }

            progress?.Report((int)((i + 1L) * 100 / total));
        }

        return added;
    }

    public async Task<int> ImportStreamAsync(
        IOrderImporter importer,
        string path,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var added = 0;
        var count = 0;

        await foreach (var item in importer.ReadAllAsync(path, ct))
        {
            ct.ThrowIfCancellationRequested();
            count++;

            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item, OrderChangeKind.OrderCreated);
                added++;
            }

            progress?.Report(count);
        }

        return added;
    }
}
