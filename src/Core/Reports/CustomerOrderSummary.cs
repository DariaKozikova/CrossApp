namespace Core.Reports;

public sealed record CustomerOrderSummary(
    string CustomerId,
    string FullName,
    int OrdersCount,
    decimal TotalAmount);
