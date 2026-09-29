namespace Core.Domain;

public sealed class Customer
{
    public string Id { get; }
    public string FullName { get; }
    
    private Customer(string id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }
    
    public static Customer Create(string id, string fullName)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Ім'я обов'язкове", nameof(fullName));
        return new Customer(id.Trim(), fullName.Trim());
    }
}
