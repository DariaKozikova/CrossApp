namespace Core.Storage;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

public sealed class JsonOrderImporter : IOrderImporter
{
    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        var dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(
            stream, cancellationToken: ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<Order>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(MapOrder(dtos[i], i + 1));
        }

        return result;
    }

    public async IAsyncEnumerable<Order> ReadAllAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        var dtos = JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(
            stream, cancellationToken: ct);

        var index = 0;
        await foreach (var dto in dtos.WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return MapOrder(dto, ++index);
        }
    }

    private static Order MapOrder(OrderDto? dto, int index)
    {
        try
        {
            return Order.FromDto(dto ?? throw new ArgumentException("Порожній елемент у списку."));
        }
        catch (Exception ex) when (ex is ArgumentException or DomainException)
        {
            throw new DomainException($"Запис {index}: {ex.Message}", ex);
        }
    }
}
