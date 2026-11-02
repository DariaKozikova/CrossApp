namespace Cli.Sinks;

using Core.Abstractions;
using Core.Events;

public sealed class FileSink(string filePath) : INotificationSink
{
    private readonly string _filePath = filePath;

    public void Notify(OrderChangedEventArgs args)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var line = $"[FileSink] {args.At:HH:mm:ss} {args.Kind,-14} {args.OrderId} Рядків: {args.LinesCount,2} → Сума: {args.Total,8:F2} грн";
            File.AppendAllText(_filePath, line + Environment.NewLine);

            var rootLogsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "logs"));
            if (Directory.Exists(rootLogsDir))
            {
                File.AppendAllText(Path.Combine(rootLogsDir, "sink.log"), line + Environment.NewLine);
            }
        }
        catch
        {
        }
    }
}
