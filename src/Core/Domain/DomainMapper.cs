using Core.Dto;

namespace Core.Domain;

public static class DomainMapper
{
    public static ImportResult<Order> MapOrders(ImportResult<OrderDto> dtoResult)
    {
        var valid = new List<Order>();
        var errors = new List<string>(dtoResult.Errors);

        foreach (var dto in dtoResult.Items)
        {
            try
            {
                valid.Add(Order.FromDto(dto));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                errors.Add($"Замовлення {dto.Id}: {ex.Message.Split('\n')[0].TrimEnd()}");
            }
        }

        return new ImportResult<Order>(valid, errors);
    }
}