namespace Core.Services;
using Core.Abstractions;
using Core.Domain;

public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string customerId, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        int openOrders = _store.List()
            .Count(o => o.CustomerId.Equals(customerId.Trim(), StringComparison.OrdinalIgnoreCase) && o.Status == OrderStatus.Draft);

        if (openOrders >= 5)
        {
            throw new InvalidOperationException(
                $"Клієнт {customerId} уже має {openOrders} відкритих замовлень (максимум 5). Спочатку підтвердіть або скасуйте їх.");
        }

        var orderId = string.IsNullOrWhiteSpace(id)
            ? Guid.NewGuid().ToString("N")[..8]
            : id.Trim();

        var order = Order.Create(orderId, customerId);
        _store.Add(order);
        return order;
    }

    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає запису з id={orderId}.");
        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
    }

    public void Confirm(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає запису з id={orderId}.");
        order.Confirm();
        _store.Update(order);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.Find(predicate);
    }
}
