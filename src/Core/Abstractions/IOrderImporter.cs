namespace Core.Abstractions;

using Core.Domain;

public interface IOrderImporter
{
    Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default);

    IAsyncEnumerable<Order> ReadAllAsync(string path, CancellationToken ct = default);
}
