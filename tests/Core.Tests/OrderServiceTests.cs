namespace Core.Tests;

using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

public sealed class OrderServiceTests
{
    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ThrowsDomainException_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("CUST-01", "ORD-1");
        service.AddLine(order.Id, "P-1", "Кабель", 100m, 2);
        service.Confirm(order.Id);

        var ex = Assert.Throws<DomainException>(() => service.Confirm(order.Id));
        Assert.Contains("вже підтверджено", ex.Message);
        Assert.Equal(OrderStatus.Confirmed, store.GetById(order.Id)!.Status);
    }

    [Fact]
    public void AddLine_WithInvalidQuantity_ThrowsArgumentOutOfRangeException_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("CUST-01", "ORD-2");

        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddLine(order.Id, "P-1", "Кабель", 100m, -5));
        Assert.Empty(store.GetById(order.Id)!.Lines);
    }

    [Fact]
    public void AddLine_CalculatesTotalCorrectly()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("CUST-01", "ORD-3");

        service.AddLine(order.Id, "P-1", "Миша", 500m, 2);
        service.AddLine(order.Id, "P-2", "Килимок", 250m, 1);

        var saved = store.GetById(order.Id);
        Assert.NotNull(saved);
        Assert.Equal(1250m, saved.Total);
        Assert.Equal(2, saved.Lines.Count);
    }

    [Fact]
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        var order = service.CreateOrder("CUST-02", "ORD-4");

        Assert.Single(events);
        Assert.Equal(OrderChangeKind.OrderCreated, events[0].Kind);
        Assert.Equal(order.Id, events[0].OrderId);
    }
}
