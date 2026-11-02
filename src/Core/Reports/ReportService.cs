namespace Core.Reports;

using Core.Domain;

public static class ReportService
{
    public static IReadOnlyList<OrderReportLine> LowTotal(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total < threshold)
            .OrderBy(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Lines.Count, o.Status.ToString()))
            .ToList();

    public static IReadOnlyList<OrderReportLine> ExpensiveOrders(IEnumerable<Order> source, decimal minTotal) =>
        source.Where(o => o.Total > minTotal)
            .OrderByDescending(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Lines.Count, o.Status.ToString()))
            .ToList();

    public static IReadOnlyList<OrderReportLine> TopOrders(IEnumerable<Order> source, int n) =>
        source.TopBy(o => o.Total, o => o.Id, n)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Lines.Count, o.Status.ToString()))
            .ToList();

    public static IReadOnlyList<CustomerSummary> TopCustomers(IEnumerable<Order> source, int n) =>
        source.GroupBy(o => o.CustomerId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .TopBy(c => c.TotalSpent, c => c.CustomerId, n)
            .ToList();

    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalAmount)
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<CustomerOrderSummary> CustomerOrders(
        IEnumerable<Customer> customers, IEnumerable<Order> orders) =>
        customers.GroupJoin(orders,
                c => c.Id, o => o.CustomerId,
                (c, matches) => new CustomerOrderSummary(
                    c.Id, c.FullName, matches.Count(), matches.Sum(o => o.Total)),
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(c => c.TotalAmount)
            .ThenBy(c => c.CustomerId, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<GroupSummary> ByStatusQuery(IEnumerable<Order> source) =>
        (from order in source
         group order by order.Status.ToString() into statusGroup
         select new GroupSummary(statusGroup.Key, statusGroup.Count(),
             statusGroup.Sum(order => order.Total)) into summary
         orderby summary.TotalAmount descending
         select summary).ThenBy(summary => summary.Key, StringComparer.Ordinal).ToList();

    public static IReadOnlyList<OrderReportLine> ExpensiveOrdersWithIntermediateList(
        IEnumerable<Order> source, decimal minTotal) =>
        source.Where(o => o.Total > minTotal)
            .ToList()
            .OrderByDescending(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Lines.Count, o.Status.ToString()))
            .ToList();

    public static (int Items, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0m, 0m, 0m)
            : (list.Count, list.Sum(o => o.Total), list.Average(o => o.Total), list.Max(o => o.Total));
    }

    public static IEnumerable<Order> DeferredFilter(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total < threshold);
}
