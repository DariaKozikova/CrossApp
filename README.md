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
├── logs/
│   └── app.log
│
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── SampleData.cs
    │   │
    │   ├── Abstractions/
    │   │   ├── IOrderStore.cs
    │   │   └── INotificationSink.cs
    │   │
    │   ├── Events/
    │   │   ├── OrderChangeKind.cs
    │   │   └── OrderChangedEventArgs.cs
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
        ├── Program.cs
        └── Sinks/
            ├── ConsoleSink.cs
            └── FileSink.cs
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


## Доменні події, делегати Action/Func, лямбди (Лабораторна 6)

- **Типи події в Core:**
  - `OrderChangeKind` (`src/Core/Events/OrderChangeKind.cs`): перелічення типів змін (`OrderCreated`, `LineAdded`, `OrderConfirmed`, `OrderCancelled`).
  - `OrderChangedEventArgs` (`src/Core/Events/OrderChangedEventArgs.cs`): дані події (успадковує `EventArgs`), містить `OrderId`, `Kind`, `LinesCount`, `Total` та `At` (`DateTimeOffset.Now`).

- **Подія в `OrderService`:**
  - Оголошена як `public event EventHandler<OrderChangedEventArgs>? Changed;`.
  - Приватний допоміжний метод `OnChanged(order, kind)` викликає `Changed?.Invoke(...)`.
  - Подія піднімається **виключно після успішної зміни стану** у методах `CreateOrder`, `AddLine`, `Confirm`, `Cancel`. Якщо інваріант кидає виняток — подія не виникає.

- **Підписники в `Cli/Program.cs`:**
  - **Перший підписник (локальний метод `OnOrderChanged`):** форматує рядок події та записує його одночасно в консоль і в файл `logs/app.log`. Демонструється відписка за допомогою `-=` (після чого операції виконуються тихо).
  - **Другий підписник (анонімна лямбда із замиканням):** `service.Changed += (_, _) => changes++;` веде підрахунок усіх згенерованих подій. Залишається активним навіть після відписки першого підписника.

- **Використання делегатів `Func` та `Action`:**
  - Делегат `Func<Order, bool>` у методі `OrderService.FindAll(predicate)` для фільтрації списку (де потрібне повернення результату).
  - Делегат `Action` у допоміжному методі `TryDo(title, action)` для безпечного виконання сценаріїв відмови без виведення stack trace.

- **Чистота домену:**
  - `src/Core` не містить жодних викликів `Console` чи звернень до файлової системи для логування. Усі сповіщення передаються через `event`.

### Додаткові завдання (Лабораторна 6)

1. **Інтерфейс `INotificationSink` з реалізаціями `ConsoleSink` та `FileSink`:**
   - Інтерфейс оголошено в `Core.Abstractions`: `void Notify(OrderChangedEventArgs args);`.
   - Реалізації `ConsoleSink` та `FileSink` винесено в `src/Cli/Sinks/` (щоб не засмічувати Core залежностями від консолі чи лог-файлів).
   - Сервіс `OrderService` приймає опційний `INotificationSink?` через конструктор (ручний DI).
   - **Порівняння з подією (`event`):**
     - *Подія (`event`):* реалізує патерн Observer (один видавець — багато підписників). Підписники підключаються динамічно під час виконання (`+=`, `-=`), сервіс взагалі не знає про їх існування та кількість.
     - *Sink (`INotificationSink`):* реалізує патерн Strategy/Sink (жорсткіша залежність 1-до-1 або вимагає CompositeSink). Передається під час створення об'єкта, складніше динамічно підключати/відключати кількох отримувачів без DI-контейнера.

2. **Делегат `Action<string>` як параметр методу імпорту:**
   - У `ProductCsvImporter.Load(path, onSkipped)` додано опційний зворотний виклик `Action<string>? onSkipped = null`.
   - Дозволяє викликачу отримувати поточні сповіщення про пропущені рядки (заголовок, порожні рядки, коментарі `#`) та некоректні дані безпосередньо під час парсингу без прив'язки імпортера до консолі.

3. **Письмове пояснення: Чому async-обробник події — це `async void`, і чим це небезпечно:**
   - **Чому `async void`:** Стандартна сигнатура подій у .NET — `delegate void EventHandler<TEventArgs>(object? sender, TEventArgs e)`. Оскільки сигнатура вимагає повернення `void`, будь-який асинхронний метод, підписаний на цю подію, змушений бути саме `async void`, а не `async Task`.
   - **Чим це небезпечно:**
     1. **Неможливість перехоплення винятків:** Викликач події (`Changed?.Invoke(...)`) не може очікувати (`await`) завершення асинхронного обробника. Якщо всередині обробника після першого `await` виникає помилка, блок `try/catch` навколо `Invoke` її **не зловить**.
     2. **Аварійне завершення процесу:** Неперехоплений виняток в `async void` спливає безпосередньо в `SynchronizationContext` / пул потоків, що за замовчуванням призводить до падіння всього процесу (`AppDomain.UnhandledException`).
     3. **Втрата контролю виконання (Fire-and-Forget):** Видавець події продовжує виконувати свій код, не знаючи, чи завершився обробник і чи завершився він успішно.
   - **Як цьому запобігти:** Усередині `async void` обробника завжди огортати весь асинхронний код у `try/catch`, або використовувати патерн асинхронних подій (`Func<object, TEventArgs, Task>`).