namespace Core.Tests;

using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;
using Xunit;

public sealed class ImportTests
{
    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", cts.Token));
    }

    [Theory]
    [InlineData(false, """{"Id":"","CustomerId":"C","Lines":[],"Status":"Draft"}""")]
    [InlineData(false, """{"Id":"O","CustomerId":"C","Lines":[],"Status":"Confirmed"}""")]
    [InlineData(true, """{"Id":"O","CustomerId":"C","Lines":[],"Status":"Confirmed"}""")]
    [InlineData(false, """{"Id":"O","CustomerId":"C","Lines":[null],"Status":"Draft"}""")]
    [InlineData(true, "null")]
    public async Task Import_WhenRecordIsInvalid_ReportsRecordNumber(bool streaming, string record)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"invalid_orders_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, $"[{record}]");

            var service = new OrderService(new InMemoryOrderStore());
            var importer = new JsonOrderImporter();

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => streaming ? service.ImportStreamAsync(importer, tempFile)
                                : service.ImportAsync(importer, tempFile));

            Assert.Contains("Запис 1", ex.Message);
            Assert.Empty(service.All());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImportAsync_LoadsValidOrders_AndIsIdempotent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"valid_orders_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, """
            [
              {
                "Id": "TMP-ORD-1",
                "CustomerId": "CUST-01",
                "Lines": [
                  { "ProductId": "P-1", "Name": "Товар 1", "Price": 100, "Quantity": 2 }
                ],
                "Status": "Draft"
              },
              {
                "Id": "TMP-ORD-2",
                "CustomerId": "CUST-02",
                "Lines": [
                  { "ProductId": "P-2", "Name": "Товар 2", "Price": 50, "Quantity": 1 }
                ],
                "Status": "Confirmed"
              }
            ]
            """);

            var store = new InMemoryOrderStore();
            var service = new OrderService(store);
            var events = new List<string>();
            service.Changed += (_, e) => events.Add(e.OrderId);

            var added = await service.ImportAsync(new JsonOrderImporter(), tempFile);

            Assert.Equal(2, added);
            Assert.Equal(2, store.List().Count);

            var addedSecondTime = await service.ImportAsync(new JsonOrderImporter(), tempFile);

            Assert.Equal(0, addedSecondTime);
            Assert.Equal(2, store.List().Count);
            Assert.Equal(new[] { "TMP-ORD-1", "TMP-ORD-2" }, events);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImportAsync_WithLinkedToken_ProvesTimeoutTriggered()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new WaitingImporter(timeoutCts), "any.json", linkedCts.Token)
                .WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.True(timeoutCts.IsCancellationRequested, "Таймаут-токен мав перейти в стан скасування.");
        Assert.False(userCts.IsCancellationRequested, "Користувацький токен НЕ мав бути скасованим.");
        Assert.True(linkedCts.IsCancellationRequested, "Зв'язаний токен мав бути скасованим через таймаут.");
    }

    private sealed class WaitingImporter(CancellationTokenSource timeout) : IOrderImporter
    {
        public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct)
        {
            timeout.CancelAfter(TimeSpan.FromMilliseconds(20));
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return [];
        }

        public IAsyncEnumerable<Order> ReadAllAsync(string path, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task ImportAsync_ReportsProgressToSubscriber()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"progress_orders_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, """
            [
              { "Id": "PRG-1", "CustomerId": "C-1", "Lines": [], "Status": "Draft" },
              { "Id": "PRG-2", "CustomerId": "C-2", "Lines": [], "Status": "Draft" },
              { "Id": "PRG-3", "CustomerId": "C-3", "Lines": [], "Status": "Draft" },
              { "Id": "PRG-4", "CustomerId": "C-4", "Lines": [], "Status": "Draft" }
            ]
            """);

            var progress = new SyncProgress();
            var service = new OrderService(new InMemoryOrderStore());

            var added = await service.ImportAsync(new JsonOrderImporter(), tempFile, progress);

            Assert.Equal(4, added);
            Assert.NotEmpty(progress.Reports);
            Assert.Equal(100, progress.Reports.Last());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private sealed class SyncProgress : IProgress<int>
    {
        public List<int> Reports { get; } = [];
        public void Report(int value) => Reports.Add(value);
    }

    [Fact]
    public async Task ImportStreamAsync_ReadsAllOrdersViaAsyncEnumerable()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"stream_orders_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, """
            [
              { "Id": "STR-1", "CustomerId": "C-1", "Lines": [], "Status": "Draft" },
              { "Id": "STR-2", "CustomerId": "C-2", "Lines": [], "Status": "Draft" }
            ]
            """);

            var store = new InMemoryOrderStore();
            var service = new OrderService(store);

            var added = await service.ImportStreamAsync(new JsonOrderImporter(), tempFile);

            Assert.Equal(2, added);
            Assert.Equal(2, store.List().Count);
            Assert.NotNull(store.GetById("STR-1"));
            Assert.NotNull(store.GetById("STR-2"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
