using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

IOrderStore store = StoreFactory.Create(args);
var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");

var created = service.CreateOrder("CUST-99");
service.AddLine(created.Id, "P-101", "Кабель UTP cat6", 25m, 10);
service.AddLine(created.Id, "P-102", "Конектор RJ-45", 5m, 20);
service.Confirm(created.Id);

Console.WriteLine("\n=== Список усіх замовлень ===");
foreach (var o in service.All())
{
    Console.WriteLine($" {o.Id,-10} {o.CustomerId,-10} {o.Status,-11} Рядків: {o.Lines.Count,2} Сума: {o.Total,8:F2} грн");
}

Console.WriteLine("\n=== Пошук за ID ===");
var found = service.Find(created.Id);
if (found != null)
{
    Console.WriteLine($"Знайдено: {found}");
}

Console.WriteLine("\n=== Додаткове завдання 2: Пошук за Func<Order, bool> ===");
Console.WriteLine("Всі підтверджені замовлення із сумою > 3000 грн:");
var filtered = service.Find(o => o.Status == OrderStatus.Confirmed && o.Total > 3000m);
foreach (var o in filtered)
{
    Console.WriteLine($" [Знайдено] {o.Id} — Клієнт: {o.CustomerId}, Сума: {o.Total:F2} грн");
}

Console.WriteLine("\n=== Додаткове завдання 1: Декоратор CachingOrderStore ===");
var cachingStore = new CachingOrderStore(store);
var cachingService = new OrderService(cachingStore);
var cachedOrder = cachingService.Find(created.Id);
Console.WriteLine($"Знайдено через декоратор: {cachedOrder?.Id} (тип обгортки: {cachingStore.GetType().Name})");

Console.WriteLine("\n=== Сценарії відмови ===");
TryDo("операція з неіснуючим id", () => service.AddLine("NON-EXISTENT-ID", "P-99", "Тестовий товар", 100m, 1));
TryDo("додавання дубліката id у сховище", () => store.Add(created));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}