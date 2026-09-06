# CrossApp

Наскрізний проєкт з крос-платформного програмування.
**Предметна область:** Замовлення.  
**Сутності:** Customer, Product, Order, OrderLine.  
**Призначення:** облік клієнтських замовлень, товарних позицій та розрахунок суми покупки.

## Запуск

```bash
dotnet build
dotnet run --project src/Cli
```
## Середовище
.NET SDK 10.0.400, Windows 11 x64 (win-x64)


## Порівняння self-contained

RID

**win-x64** 78MB

**linux-x64** 80MB

Публікація для win-x64 виявилася на 2 MB меншою, ніж для linux-x64.
