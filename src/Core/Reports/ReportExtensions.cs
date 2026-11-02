namespace Core.Reports;

public static class ReportExtensions
{
    public static IEnumerable<T> TopBy<T>(this IEnumerable<T> source,
        Func<T, decimal> valueSelector, Func<T, string> idSelector, int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(valueSelector);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return source.OrderByDescending(valueSelector)
            .ThenBy(idSelector, StringComparer.Ordinal)
            .Take(n);
    }
}
