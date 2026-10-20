using Cli.Sinks;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

IOrderStore store = useFile
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Orders());

if (args.Contains("--cached"))
{
    store = new CachingOrderStore(store);
}

var service = new OrderService(store);
Console.WriteLine($"Сховище: {store.GetType().Name}\n");

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
if (File.Exists(logPath))
{
    File.Delete(logPath);
}

var projectLogDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "logs"));
var projectLogPath = Path.Combine(projectLogDir, "app.log");
try
{
    Directory.CreateDirectory(projectLogDir);
    if (File.Exists(projectLogPath)) File.Delete(projectLogPath);
}
catch { }

void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    var line = $"{e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} Рядків: {e.LinesCount,2} → Сума: {e.Total,8:F2} грн";
    Console.WriteLine(line);

    try
    {
        File.AppendAllText(logPath, line + Environment.NewLine);
        File.AppendAllText(projectLogPath, line + Environment.NewLine);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Логер] Помилка запису у файл: {ex.Message}");
    }
}

service.Changed += OnOrderChanged;

int changes = 0;
service.Changed += (_, _) => changes++;

Console.WriteLine("=== Сценарій сповіщень про зміни ===");
var created = service.CreateOrder("CUST-99");
service.AddLine(created.Id, "P-101", "Кабель UTP cat6", 25m, 10);
Console.WriteLine($"Подій отримано: {changes}");

service.Changed -= OnOrderChanged;

service.AddLine(created.Id, "P-102", "Конектор RJ-45", 5m, 20);
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine("\n=== Сценарії відмови (подія НЕ повинна виникати) ===");
int changesBefore = changes;

TryDo("додавання товару з від'ємною кількістю (-5)", () =>
    service.AddLine(created.Id, "P-999", "Помилковий товар", 100m, -5));

TryDo("операція над неіснуючим замовленням", () =>
    service.AddLine("NON-EXISTENT-ID", "P-999", "Помилковий товар", 100m, 1));

Console.WriteLine($"Зміна лічильника подій після збоїв: {changes - changesBefore} (жодної події не виникло!)");

service.Confirm(created.Id);

Console.WriteLine("\n=== Делегат Func<Order, bool> замість події (отримання вибірки) ===");
var filteredOrders = service.FindAll(o => o.Total > 200m);
Console.WriteLine($"Знайдено замовлень на суму > 200 грн: {filteredOrders.Count}");
foreach (var o in filteredOrders.Take(5))
{
    Console.WriteLine($"  {o.Id,-10} {o.CustomerId,-10} {o.Status,-11} Рядків: {o.Lines.Count,2} Сума: {o.Total,8:F2} грн");
}

Console.WriteLine("\n=== Додаткове завдання 1: INotificationSink через ручний DI ===");
var consoleSink = new ConsoleSink();
var sinkService = new OrderService(store, consoleSink);
var sinkOrder = sinkService.CreateOrder("CUST-SINK");
sinkService.AddLine(sinkOrder.Id, "P-SINK", "Тестовий товар для ConsoleSink", 150m, 2);

var sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
var fileSink = new FileSink(sinkLogPath);
var fileSinkService = new OrderService(store, fileSink);
var fileSinkOrder = fileSinkService.CreateOrder("CUST-FILESINK");
fileSinkService.AddLine(fileSinkOrder.Id, "P-FILESINK", "Тестовий товар для FileSink", 200m, 1);
Console.WriteLine($"[FileSink] Подію успішно записано у файл: logs/sink.log");

Console.WriteLine("\n=== Додаткове завдання 2: Action<string> при імпорті для звіту про рядки ===");
string sampleCsv = Path.Combine(AppContext.BaseDirectory, "data", "sample.csv");
if (File.Exists(sampleCsv))
{
    var importResult = ProductCsvImporter.Load(sampleCsv, onSkipped: msg =>
        Console.WriteLine($"  [Звіт імпорту] {msg}"));
    Console.WriteLine($"  Підсумок: успішно прочитано {importResult.Items.Count} товарів, з помилками {importResult.Errors.Count}");
}

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
