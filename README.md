# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення.

Сутності: Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).

Призначення: оформлення замовлень і підрахунок сум.

## Середовище
.NET SDK 10.0.400 Windows 11 x64 (win-x64) Ubuntu 24.04.4

## Структура solution

```
## Структура проєкту

```text
CrossApp/
├── CrossApp.slnx
├── README.md
├── .gitignore
│
├── data/
│   ├── sample.csv
│   ├── sample.json
│   ├── sample_mixed.csv
│   └── orders.json
│
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── SampleData.cs
    │   │
    │   ├── Abstractions/
    │   │   └── IOrderStore.cs
    │   │
    │   ├── Dto/
    │   │   ├── ProductDto.cs
    │   │   ├── CustomerDto.cs
    │   │   ├── OrderDto.cs
    │   │   ├── OrderLineDto.cs
    │   │   └── ImportResult.cs
    │   │
    │   ├── Domain/
    │   │   ├── Customer.cs
    │   │   ├── DomainMapper.cs
    │   │   ├── Order.cs
    │   │   ├── OrderLine.cs
    │   │   └── OrderStatus.cs
    │   │
    │   ├── Storage/
    │   │   ├── InMemoryOrderStore.cs
    │   │   ├── FileOrderStore.cs
    │   │   ├── CachingOrderStore.cs
    │   │   └── StoreFactory.cs
    │   │
    │   ├── Services/
    │   │   └── OrderService.cs
    │   │
    │   └── Import/
    │       ├── ProductCsvImporter.cs
    │       ├── ProductJsonImporter.cs
    │       └── MixedImporter.cs
    │
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

`Core` — class library (немає точки входу, результат збірки — `Core.dll`).
`Cli` — консольний застосунок, має `ProjectReference` на `Core`.
Залежність одностороння: `Cli → Core`. `Core` ніколи не посилається на `Cli`.


### Збірка
```bash
dotnet build
dotnet build src/Core/Core.csproj
dotnet run --project src/Cli
```

### Запуск із файлом за замовчуванням:

```bash
dotnet run --project src/Cli
```
Використовується: data/sample.csv

### Запуск із власним шляхом:

```bash
dotnet run --project src/Cli -- data/sample.csv
```

### JSON

```bash
dotnet run --project src/Cli -- data/sample.json
```

### Різнорідний CSV
```bash
dotnet run --project src/Cli -- data/sample_mixed.csv
```

###  Перевірка неіснуючого файлу
```bash
dotnet run --project src/Cli -- data/does-not-exist.csv
```

## Порівняння режимів публікації

| RID       | Режим                | Розмір publish | Потрібен runtime |
|-----------|----------------------|----------------|------------------|
| win-x64   | self-contained       | ~78 МБ         | ні               |
| win-x64   | framework-dependent  | ~0.2 МБ        | так (.NET 10)    |
| linux-x64 | self-contained       | ~78.8 МБ       | ні               |
| linux-x64 | framework-dependent  | ~0.11 МБ       | так (.NET 10)    

## Плани щодо доменної моделі


- **`Core/Dto/`** (тиждень 3) — record-типи для передачі даних:
  `CustomerDto`, `ProductDto`, `OrderDto`, `OrderLineDto`
- **`Core/Domain/`** (тиждень 4) — сутності з поведінкою та інваріантами:
  `Customer` (клієнт), `Product` (товар), `Order` (замовлення), `OrderLine` (рядок
  замовлення) — з логікою підрахунку сум і перевіркою правил.
- **`Core/Storage/`** (тиждень 5) — реалізації сховищ для збереження/пошуку
  замовлень і клієнтів

## Сервісний шар та сховища (Лабораторна 5)

- **Абстракція (контракт):** `IOrderStore` (`src/Core/Abstractions/IOrderStore.cs`)
  - Методи: `List()`, `GetById(string id)`, `Add(Order item)`, `Update(Order item)`, `bool Remove(string id)`.
  - Повертає `IReadOnlyList<Order>`, щоб унеможливити мутацію сховища в обхід методів.

- **Реалізації сховища:**
  1. `InMemoryOrderStore` (`src/Core/Storage/InMemoryOrderStore.cs`) — збереження в оперативній пам'яті на базі `Dictionary<string, Order>`. Підтримує початкове заповнення (seed) через конструктор (з `SampleData.Orders()`).
  2. `FileOrderStore` (`src/Core/Storage/FileOrderStore.cs`) — файлове збереження у форматі JSON (`data/orders.json`). Використовує кеш у пам'яті, дозавантаження при першому зверненні та запис на диск (`Flush`) після кожної зміни (`Add`, `Update`, `Remove`). Серіалізація відбувається через `OrderDto` з мапінгом `ToDto()` / `FromDto()`.

- **Сервіс:** `OrderService` (`src/Core/Services/OrderService.cs`)
  - Використовує ручне впровадження залежностей (Dependency Injection) через первинний конструктор: залежить виключно від інтерфейсу `IOrderStore`, а не від конкретних класів.
  - Операції: `CreateOrder`, `AddLine`, `Confirm`, `All`, `Find`.

- **Composition Root:** `src/Cli/Program.cs`
  - Єдине місце вибору конкретної реалізації сховища за аргументом командного рядка:
    - Без аргументів: `InMemoryOrderStore` (дані живуть лише під час виконання):
      ```bash
      dotnet run --project src/Cli
      ```
    - З прапорцем `--file`: `FileOrderStore` (дані накопичуються у `data/orders.json` між запусками):
      ```bash
      dotnet run --project src/Cli -- --file
      ```
  - Перехоплення винятків у блоках `try/catch` з виведенням зрозумілих повідомлень без stack trace.

### Додаткові завдання (Лабораторна 5)

1. **Декоратор `CachingOrderStore` (`src/Core/Storage/CachingOrderStore.cs`):**
   - Реалізує патерн Декоратор: приймає інший `IOrderStore` у конструкторі, реалізує той самий інтерфейс `IOrderStore`, додаючи прозоре кешування сутностей у пам'яті для оптимізації повторних запитів.

2. **Пошук за предикатом `Func<Order, bool>`:**
   - В інтерфейс `IOrderStore`, усі реалізації сховищ та `OrderService` додано метод `Find(Func<Order, bool> predicate)`.
   - Дозволяє виконувати гнучкі вибірки (наприклад, `o => o.Status == OrderStatus.Confirmed && o.Total > 3000m`), що є підготовкою до LINQ-запитів 6–7 тижнів.

3. **Фабрика `StoreFactory` (`src/Core/Storage/StoreFactory.cs`):**
   - Метод `StoreFactory.Create(args)` інкапсулює логіку вибору та конфігурації сховища на основі прапорців `--file` та `--cached`.
   - **Порівняння:**
     - *«Усе в Program.cs»:* простіше для крихітних застосунків, не потребує додаткових класів при одному простому if/тернарному операторі.
     - *«StoreFactory»:* виносить логіку вибору сховища в окремий модуль, спрощує Composition Root у `Program.cs`, легко покривається модульними тестами та масштабується у разі появи нових параметрів конфігурації (наприклад, `--cached`).