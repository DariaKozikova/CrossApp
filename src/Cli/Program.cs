using Core.Domain;
using Core.Dto;

Console.WriteLine("=== Сценарій 1: успіх ===");
Order order = Order.Create("ORD-001", "CUST-99");
order.AddLine("P-1", "Ноутбук", 25000m, 1);
order.AddLine("P-2", "Миша", 500m, 2);
Console.WriteLine(order);
order.Confirm();
Console.WriteLine(order);

Console.WriteLine("\n=== Сценарій 2: порушення інваріантів ===");
Order draft = Order.Create("ORD-002", "CUST-99");
TryDo("порожній ID замовлення", () => Order.Create(" ", "CUST-99"));
TryDo("від'ємна кількість", () => draft.AddLine("P-3", "Клавіатура", 1000m, -5));
TryDo("рядок до підтвердженого замовлення", () => order.AddLine("P-3", "Клавіатура", 1000m, 1));
TryDo("підтвердження порожнього замовлення", () => draft.Confirm());
Console.WriteLine($" Стан після відмов: {draft}");

Console.WriteLine("\n=== Додаткові завдання ===");
Order cancelled = Order.Create("ORD-003", "CUST-99");
cancelled.Cancel();
TryDo("повторне скасування (enum + switch)", () => cancelled.Cancel());

Customer customer = Customer.Create("C-1", "Іван");
var open = Enumerable.Range(1, 5).Select(i => Order.Create($"O-{i}", "C-1")).ToList();
TryDo("ліміт відкритих замовлень (сервіс)", () => new OrderService().CreateOrder(customer, open, "O-NEW"));

var badDto = new OrderDto("ORD-BAD", "CUST-99", [new OrderLineDto("P-1", "Товар", 10m, -3)], "Draft");
var mapped = DomainMapper.MapOrders(new ImportResult<OrderDto>([order.ToDto(), badDto], []));
Console.WriteLine($" Імпорт: коректних {mapped.Items.Count}, помилок {mapped.Errors.Count}");
foreach (var error in mapped.Errors)
    Console.WriteLine($"   - {error}");

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}