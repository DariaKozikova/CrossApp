using System.Diagnostics;
using Core;
using Core.Reports;

namespace Cli;

internal static class ReportBenchmark
{
    public static void Run()
    {
        const int count = 100_000;
        const int repetitions = 7;
        const decimal threshold = 3000m;
        var orders = SampleData.GenerateOrders(count);
        var expected = ReportService.ExpensiveOrders(orders, threshold);
        var buffered = ReportService.ExpensiveOrdersWithIntermediateList(orders, threshold);
        if (!expected.SequenceEqual(buffered))
            throw new InvalidOperationException("Варіанти звіту дали різні результати.");

        for (var i = 0; i < 3; i++)
        {
            ReportService.ExpensiveOrders(orders, threshold);
            ReportService.ExpensiveOrdersWithIntermediateList(orders, threshold);
        }

        var directTimes = new List<double>();
        var bufferedTimes = new List<double>();
        for (var i = 0; i < repetitions; i++)
        {
            if (i % 2 == 0)
            {
                directTimes.Add(Measure(() => ReportService.ExpensiveOrders(orders, threshold)));
                bufferedTimes.Add(Measure(() => ReportService.ExpensiveOrdersWithIntermediateList(orders, threshold)));
            }
            else
            {
                bufferedTimes.Add(Measure(() => ReportService.ExpensiveOrdersWithIntermediateList(orders, threshold)));
                directTimes.Add(Measure(() => ReportService.ExpensiveOrders(orders, threshold)));
            }
        }

        Console.WriteLine($"=== Stopwatch: {count:N0} замовлень, {repetitions} вимірювань ===");
        Console.WriteLine($"Результати однакові: так; рядків: {expected.Count}");
        Console.WriteLine($"Без проміжного ToList: {Median(directTimes):F3} мс (медіана)");
        Console.WriteLine($"З проміжним ToList:    {Median(bufferedTimes):F3} мс (медіана)");
        Console.WriteLine("Обидва варіанти мають кінцевий ToList. Час залежить від середовища та GC.");
    }

    private static double Measure(Func<IReadOnlyList<OrderReportLine>> report)
    {
        var timer = Stopwatch.StartNew();
        var result = report();
        timer.Stop();
        GC.KeepAlive(result);
        return timer.Elapsed.TotalMilliseconds;
    }

    private static double Median(IReadOnlyList<double> values) =>
        values.OrderBy(value => value).ElementAt(values.Count / 2);
}
