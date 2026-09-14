# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення.

Сутності: Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).

Призначення: оформлення замовлень і підрахунок сум.

## Середовище
.NET SDK 10.0.400 Windows 11 x64 (win-x64) Ubuntu 24.04.4

## Структура Solution та плани на семестр
CrossApp/
├── src/
│   ├── Core/         # Спільна логіка проєкту   
│   └── Cli/          # Консольний інтерфейс


## Команди
- **Збірка:** `dotnet build`
- **Запуск:** `dotnet run --project src/Cli`
- **Публікація (Self-contained):** `dotnet publish src/Cli -c Release -r win-x64 --self-contained true`
- **Публікація (Framework-dependent):** `dotnet publish src/Cli -c Release -r win-x64 --self-contained false`

## Порівняння режимів публікації

| RID       | Режим                | Розмір publish | Потрібен runtime |
|-----------|----------------------|----------------|------------------|
| win-x64   | self-contained       | ~78 МБ         | ні               |
| win-x64   | framework-dependent  | ~0.2 МБ        | так (.NET 10)    |
| linux-x64 | self-contained       | ~78.8 МБ       | ні               |
| linux-x64 | framework-dependent  | ~0.11 МБ       | так (.NET 10)    
