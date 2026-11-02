namespace Core.Reports;

public sealed record OrderReportLine(
    string OrderId,
    string CustomerId,
    decimal Total,
    int LinesCount,
    string Status);
