namespace Core.Storage;

using Core.Abstractions;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args, string? customPath = null)
    {
        bool useFile = args.Contains("--file");
        bool useCached = args.Contains("--cached");

        string dataPath = customPath ?? Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(SampleData.Orders());

        if (useCached)
        {
            store = new CachingOrderStore(store);
        }

        return store;
    }
}
