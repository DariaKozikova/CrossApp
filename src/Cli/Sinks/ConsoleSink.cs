namespace Cli.Sinks;

using Core.Abstractions;
using Core.Events;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs args)
    {
        Console.WriteLine($"[ConsoleSink] {args.At:HH:mm:ss} {args.Kind,-14} {args.OrderId} Рядків: {args.LinesCount,2} → Сума: {args.Total,8:F2} грн");
    }
}
