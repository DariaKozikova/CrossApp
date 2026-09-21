namespace Core.Dto;

public record OrderDto(
    string Id,
    string CustomerId,
    string ProductId,
    int Quantity);
