namespace Core.Dto;

public record OrderDto(
    string Id,
    string CustomerId,
    IReadOnlyList<OrderLineDto> Lines,
    string Status);
