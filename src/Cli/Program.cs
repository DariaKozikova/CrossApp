using Core.Dto;
using Core.Import;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (path.EndsWith("_mixed.csv"))
{
    MixedImportResult mixed = MixedImporter.Load(path);
    Console.WriteLine($"Товари: {mixed.Products.Items.Count}, помилок: {mixed.Products.Errors.Count}");
    foreach (string e in mixed.Products.Errors)
        Console.WriteLine($"  ! {e}");

    Console.WriteLine($"Клієнти: {mixed.Customers.Items.Count}, помилок: {mixed.Customers.Errors.Count}");
    foreach (string e in mixed.Customers.Errors)
        Console.WriteLine($"  ! {e}");

    Console.WriteLine($"Замовлення: {mixed.Orders.Items.Count}, помилок: {mixed.Orders.Errors.Count}");
    foreach (string e in mixed.Orders.Errors)
        Console.WriteLine($"  ! {e}");

    return 0;
}

ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    var ext => throw new NotSupportedException($"формат '{ext}' не підтримується")
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (ProductDto p in result.Items.Take(5))
    Console.WriteLine($"  {p.Id,-6} {p.Name,-30} {p.Price,10:F2}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

PrintStats(result);

return 0;

static void PrintStats(ImportResult<ProductDto> result)
{
    int total = result.Items.Count + result.Errors.Count;
    double errorRate = total == 0 ? 0 : (double)result.Errors.Count / total * 100;

    Console.WriteLine(
        $"Статистика: усього {total}, прийнято {result.Items.Count}, " +
        $"пропущено {result.Errors.Count}, помилок {errorRate:F1}%");
}