namespace Core.Domain;

public sealed class OrderService
{
    private const int MaxOpenOrders = 5;

    public Order CreateOrder(Customer customer, IEnumerable<Order> existingOrders, string newOrderId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(existingOrders);

        int open = existingOrders.Count(o => o.CustomerId == customer.Id && o.Status == OrderStatus.Draft);

        if (open >= MaxOpenOrders)
            throw new InvalidOperationException(
                $"Клієнт {customer.Id} вже має {open} відкритих замовлень (максимум {MaxOpenOrders}). Спочатку підтвердіть або скасуйте їх.");

        return Order.Create(newOrderId, customer.Id);
    }
}