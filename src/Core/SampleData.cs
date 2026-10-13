namespace Core;

using Core.Domain;

public static class SampleData
{
    public static IReadOnlyList<Order> Orders()
    {
        var list = new List<Order>();

        var o1 = Order.Create("ORD-001", "CUST-01");
        o1.AddLine("P-01", "Ноутбук ASUS ROG", 45000m, 1);
        o1.AddLine("P-02", "Миша ігрова", 1200m, 1);
        o1.Confirm();
        list.Add(o1);

        var o2 = Order.Create("ORD-002", "CUST-02");
        o2.AddLine("P-03", "Монітор 27 cali 144Hz", 9500m, 2);
        o2.Confirm();
        list.Add(o2);

        var o3 = Order.Create("ORD-003", "CUST-03");
        o3.AddLine("P-04", "Клавіатура механічна", 2800m, 1);
        o3.AddLine("P-05", "Килимок для миші", 450m, 1);
        list.Add(o3);

        var o4 = Order.Create("ORD-004", "CUST-04");
        o4.AddLine("P-06", "Навушники бездротові", 3200m, 1);
        o4.Confirm();
        list.Add(o4);

        var o5 = Order.Create("ORD-005", "CUST-01");
        o5.AddLine("P-07", "Кабель HDMI 2.1", 350m, 3);
        list.Add(o5);

        var o6 = Order.Create("ORD-006", "CUST-05");
        o6.AddLine("P-08", "SSD накопичувач 1TB", 3100m, 2);
        o6.AddLine("P-09", "Термопаста Arctic MX-4", 250m, 1);
        o6.Confirm();
        list.Add(o6);

        var o7 = Order.Create("ORD-007", "CUST-06");
        o7.AddLine("P-10", "Веб-камера FullHD", 1950m, 1);
        list.Add(o7);

        var o8 = Order.Create("ORD-008", "CUST-07");
        o8.AddLine("P-11", "Wi-Fi Роутер Gigabit", 2400m, 1);
        o8.Confirm();
        list.Add(o8);

        var o9 = Order.Create("ORD-009", "CUST-02");
        o9.AddLine("P-12", "Флешка USB 64GB", 280m, 4);
        list.Add(o9);

        var o10 = Order.Create("ORD-010", "CUST-08");
        o10.AddLine("P-13", "Блок живлення 750W", 4200m, 1);
        o10.Confirm();
        list.Add(o10);

        var o11 = Order.Create("ORD-011", "CUST-09");
        o11.AddLine("P-14", "Оперативна пам'ять 16GB", 1850m, 2);
        o11.Confirm();
        list.Add(o11);

        var o12 = Order.Create("ORD-012", "CUST-10");
        o12.AddLine("P-15", "Корпус для ПК", 3600m, 1);
        list.Add(o12);

        var o13 = Order.Create("ORD-013", "CUST-03");
        o13.AddLine("P-16", "Кулер процесорний", 1450m, 1);
        o13.Confirm();
        list.Add(o13);

        var o14 = Order.Create("ORD-014", "CUST-04");
        o14.AddLine("P-17", "Колонки 2.0", 1100m, 1);
        list.Add(o14);

        var o15 = Order.Create("ORD-015", "CUST-05");
        o15.AddLine("P-18", "Підставка для ноутбука", 650m, 1);
        o15.AddLine("P-07", "Кабель HDMI 2.1", 350m, 1);
        o15.Confirm();
        list.Add(o15);

        var o16 = Order.Create("ORD-016", "CUST-06");
        o16.AddLine("P-19", "Мікрофон USB", 2300m, 1);
        list.Add(o16);

        return list;
    }
}
