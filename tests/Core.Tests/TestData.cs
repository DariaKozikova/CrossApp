namespace Core.Tests;

using Core.Domain;

public static class TestData
{
    public static IReadOnlyList<Order> Orders()
    {
        var list = new List<Order>();

        var o1 = Order.Create("TEST-ORD-1", "CUST-01");
        o1.AddLine("P-1", "Товар 1", 5000m, 1);
        o1.Confirm();
        list.Add(o1);

        var o2 = Order.Create("TEST-ORD-2", "CUST-02");
        o2.AddLine("P-2", "Товар 2", 5000m, 1);
        o2.Confirm();
        list.Add(o2);

        var o3 = Order.Create("TEST-ORD-3", "CUST-01");
        o3.AddLine("P-3", "Товар 3", 500m, 1);
        list.Add(o3);

        var o4 = Order.Create("TEST-ORD-4", "CUST-03");
        o4.AddLine("P-4", "Товар 4", 250m, 1);
        list.Add(o4);

        return list;
    }
}
