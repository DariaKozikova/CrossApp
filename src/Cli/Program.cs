using Core;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;
using Cli;
using System.Text.Json;

if (args.Any(a => a.StartsWith("report-")))
{
    var store = StoreFactory.Create(args);
    var service = new OrderService(store);
    var items = service.All();

    Console.WriteLine($"Сховище: {store.GetType().Name}\n");

    Console.WriteLine("=== Демонстрація відкладеного виконання ===");
    var source = service.All().ToList();
    var query = ReportService.DeferredFilter(source, 1000m);
    Console.WriteLine($"Малих замовлень (< 1000 грн) до змін: {query.Count()}");

    var tempOrder = Order.Create("ORD-TMP", "CUST-TMP");
    tempOrder.AddLine("P-TMP", "Кабель USB", 200m, 1);
    source.Add(tempOrder);

    Console.WriteLine($"Малих замовлень після змін: {query.Count()}");
    var snapshot = query.ToList();
    source.Clear();
    Console.WriteLine($"У знімку лишилось: {snapshot.Count}\n");

    var command = args.FirstOrDefault(a => a.StartsWith("report-")) ?? "report-top";
    switch (command)
    {
        case "report-top":
            Console.WriteLine("=== Звіт: Топ-5 замовлень за сумою ===");
            Print(ReportService.TopOrders(items, 5));
            Console.WriteLine("\n=== Звіт: Топ-5 клієнтів за сумою ===");
            PrintCustomers(ReportService.TopCustomers(items, 5));
            break;

        case "report-low":
            Console.WriteLine("=== Звіт: Замовлення до 1000 грн (невеликі чеки) ===");
            Print(ReportService.LowTotal(items, 1000m));
            break;

        case "report-expensive":
            Console.WriteLine("=== Звіт: Замовлення дорожче 3000 грн ===");
            Print(ReportService.ExpensiveOrders(items, 3000m));
            break;

        case "report-groups":
            Console.WriteLine("=== Звіт: Розподіл замовлень за статусом ===");
            foreach (var g in ReportService.ByStatus(items))
                Console.WriteLine($" {g.Key,-11} замовлень: {g.Count,2}  сумарно: {g.TotalAmount,10:F2} грн");
            break;

        case "report-sum":
            Console.WriteLine("=== Звіт: Підсумкові показники ===");
            var t = ReportService.Totals(items);
            Console.WriteLine($" позицій {t.Items}, всього {t.Total:F2} грн, середнє {t.Average:F2} грн, макс {t.Max:F2} грн");
            break;

        case "report-customers":
            Console.WriteLine("=== Звіт: Клієнти та їхні замовлення (GroupJoin) ===");
            foreach (var c in ReportService.CustomerOrders(SampleData.Customers(), items))
                Console.WriteLine($" {c.CustomerId,-10} {c.FullName,-22} замовлень: {c.OrdersCount,2}  сума: {c.TotalAmount,10:F2} грн");
            break;

        case "report-groups-query":
            Console.WriteLine("=== Звіт: Статуси через синтаксис запитів ===");
            foreach (var g in ReportService.ByStatusQuery(items))
                Console.WriteLine($" {g.Key,-11} замовлень: {g.Count,2}  сумарно: {g.TotalAmount,10:F2} грн");
            break;

        case "report-benchmark":
            ReportBenchmark.Run();
            break;

        default:
            Console.WriteLine("report-top | report-low | report-expensive | report-groups | report-sum | report-customers | report-groups-query | report-benchmark");
            break;
    }
    return;
}

string dataPath = args.FirstOrDefault(a => !a.StartsWith("--") && a.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    ?? Path.Combine("data", "orders.json");

if (!File.Exists(dataPath))
{
    var projectData = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", dataPath));
    if (File.Exists(projectData))
        dataPath = projectData;
    else
    {
        var localData = Path.Combine(AppContext.BaseDirectory, dataPath);
        if (File.Exists(localData))
            dataPath = localData;
    }
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

if (args.Contains("--timeout"))
{
    cts.CancelAfter(TimeSpan.FromMilliseconds(10));
}

using var timeoutCts = args.Contains("--linked-timeout")
    ? new CancellationTokenSource(TimeSpan.FromMilliseconds(5))
    : null;
using var linkedCts = timeoutCts is null
    ? null
    : CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeoutCts.Token);

var tokenToUse = linkedCts?.Token ?? cts.Token;
var importStore = new InMemoryOrderStore();
var importService = new OrderService(importStore);

var streaming = args.Contains("--stream");
var progress = new SynchronousProgress<int>(value =>
{
    Console.Write(streaming ? $"\r[Прочитано записів: {value}]" : $"\r[Хід імпорту: {value,3}%]");
});

try
{
    var importer = new JsonOrderImporter();

    if (streaming)
    {
        var added = await importService.ImportStreamAsync(importer, dataPath, progress, tokenToUse);
        Console.WriteLine();
        Console.WriteLine($"Імпортовано через потік (IAsyncEnumerable): {added}");
    }
    else
    {
        var added = await importService.ImportAsync(importer, dataPath, progress, tokenToUse);
        Console.WriteLine();
        Console.WriteLine($"Імпортовано записів: {added}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("Імпорт скасовано.");
}
catch (DomainException ex)
{
    Console.WriteLine();
    Console.WriteLine($"Дані відхилено: {ex.Message}");
}
catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
{
    Console.WriteLine();
    Console.WriteLine($"Файл не знайдено: {dataPath}");
}
catch (JsonException ex)
{
    Console.WriteLine($"\nНекоректний JSON: {ex.Message}");
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Console.WriteLine($"\nНе вдалося прочитати файл: {ex.Message}");
}

static void Print(IReadOnlyList<OrderReportLine> lines)
{
    foreach (var l in lines)
    {
        Console.WriteLine($" {l.OrderId,-10} {l.CustomerId,-10} {l.Status,-11} Рядків: {l.LinesCount,2}  Сума: {l.Total,8:F2} грн");
    }
}

static void PrintCustomers(IReadOnlyList<CustomerSummary> customers)
{
    foreach (var c in customers)
    {
        Console.WriteLine($" Клієнт: {c.CustomerId,-10}  Замовлень: {c.OrdersCount,2}  Сума замовлень: {c.TotalSpent,9:F2} грн");
    }
}

sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
{
    public void Report(T value) => handler(value);
}
