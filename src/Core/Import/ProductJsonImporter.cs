using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        var errors = new List<string>();
        string json;

        try
        {
            json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            errors.Add($"не вдалося прочитати файл: {ex.Message}");
            return new ImportResult<ProductDto>(new List<ProductDto>(), errors);
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        List<ProductDto> items;
        try
        {
            items = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
        }
        catch (JsonException ex)
        {
            errors.Add($"помилка розбору JSON: {ex.Message}");
            return new ImportResult<ProductDto>(new List<ProductDto>(), errors);
        }

        // Проста валідація вже розібраних об'єктів (аналогічно CSV-парсеру)
        var valid = new List<ProductDto>();
        for (int i = 0; i < items.Count; i++)
        {
            ProductDto p = items[i];
            switch (p)
            {
                case { Name.Length: 0 }:
                    errors.Add($"елемент {i + 1}: назва порожня");
                    break;
                case { Price: < 0 }:
                    errors.Add($"елемент {i + 1}: ціна '{p.Price}' від'ємна");
                    break;
                default:
                    valid.Add(p);
                    break;
            }
        }

        return new ImportResult<ProductDto>(valid, errors);
    }
}