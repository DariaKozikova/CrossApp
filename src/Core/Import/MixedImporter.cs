using System.Globalization;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed record MixedImportResult(
    ImportResult<ProductDto> Products,
    ImportResult<CustomerDto> Customers,
    ImportResult<OrderDto> Orders);

public static class MixedImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var productErrors = new List<string>();
        var customers = new List<CustomerDto>();
        var customerErrors = new List<string>();
        var orders = new List<OrderDto>();
        var orderErrors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            switch (parts)
            {
                case ["P", var id, var name, var price]
                    when !decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) || p < 0:
                    productErrors.Add($"рядок {number}: ціна '{price}' некоректна");
                    break;

                case ["P", var id, var name, var price]:
                    products.Add(new ProductDto(
                        id, name,
                        decimal.Parse(price, NumberStyles.Number, CultureInfo.InvariantCulture)));
                    break;

                case ["C", var id, var fullName]:
                    customers.Add(new CustomerDto(id, fullName));
                    break;

                case ["C", var id, var fullName, var email]:
                    customers.Add(new CustomerDto(id, fullName, email));
                    break;

                case ["O", var id, var customerId, var productId, var quantity]
                    when !int.TryParse(quantity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int q) || q <= 0:
                    orderErrors.Add($"рядок {number}: кількість '{quantity}' некоректна");
                    break;

                case ["O", var id, var customerId, var productId, var quantity]:
                    orders.Add(new OrderDto(
                        id,
                        customerId,
                        [new OrderLineDto(productId, "Unknown", 0m,
                            int.Parse(quantity, NumberStyles.Integer, CultureInfo.InvariantCulture))],
                        nameof(OrderStatus.Draft)));
                    break;

                case [var prefix, ..]:
                    productErrors.Add($"рядок {number}: невідомий префікс '{prefix}'");
                    break;

                default:
                    productErrors.Add($"рядок {number}: неправильна кількість колонок");
                    break;
            }
        }

        return new MixedImportResult(
            new ImportResult<ProductDto>(products, productErrors),
            new ImportResult<CustomerDto>(customers, customerErrors),
            new ImportResult<OrderDto>(orders, orderErrors));
    }
}