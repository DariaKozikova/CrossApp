# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення.

Доменні сутності: Customer (клієнт), Order (замовлення), OrderLine (рядок замовлення). Дані товарів для імпорту представлені ProductDto.

Призначення: оформлення замовлень і підрахунок сум.

## Середовище
.NET SDK 10.0.400 Windows 11 x64 (win-x64) Ubuntu 24.04.4

## Структура solution

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
    │   ├── EnvironmentInfo.cs
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
    │   ├── Import/
    │   │   ├── ProductCsvImporter.cs
    │   │   ├── ProductJsonImporter.cs
    │   │   └── MixedImporter.cs
    │   │
    │   └── Reports/
    │       ├── OrderReportLine.cs
    │       ├── CustomerSummary.cs
    │       ├── CustomerOrderSummary.cs
    │       ├── GroupSummary.cs
    │       ├── ReportExtensions.cs
    │       └── ReportService.cs
    │
    └── Cli/
        ├── Cli.csproj
        ├── Program.cs
        ├── ReportBenchmark.cs
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

### Поточний запуск

Без аргументів CLI запускає report-top на SampleData. Команди звітів наведені в розділі лабораторної №7 нижче.

Імпортери CSV, JSON і змішаних записів залишаються в Core/Import, а приклади вхідних даних — у data/. Поточний CLI звітів не приймає шлях до файлу як команду імпорту. Демонстрація імпорту належить до попередніх лабораторних.

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

- **Поточна точка входу:**
  - У лабораторній №7 Program.cs запускає звіти й демонстрацію відкладеного виконання. Підписники події, підрахунок подій і TryDo з демонстрації лабораторної №6 у поточному CLI не запускаються.
  - Подія Changed, механізм сповіщень та реалізації sink залишаються в коді й доступні для використання. Попередня демонстрація міститься в історії Git у коміті lab06.

- **Делегати:**
  - Func<Order, bool> використовується в методах пошуку OrderService.
  - Action<string> передається в ProductCsvImporter.Load для повідомлень про пропущені рядки та помилки.

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

## Лабораторна 7: LINQ-звіти для замовлень

Приклад складу з методички адаптовано до домену «Замовлення»: замість залишків використовуємо суму замовлення, замість одиниць виміру — статус. SampleData.Orders() містить 20 замовлень для 10 клієнтів і 3 статуси (Draft, Confirmed, Cancelled). Наявна модель має саме три статуси, тому додавати штучний четвертий не потрібно. Усі звіти враховують усі статуси: сума замовлень не означає оплачену виручку.

### Запити основного завдання

| Метод | Питання | Оператори |
|---|---|---|
| TopCustomers | Які 5 клієнтів мають найбільшу суму замовлень? | GroupBy, Select, Sum, OrderByDescending, ThenBy, Take, ToList |
| ExpensiveOrders | Які замовлення дорожчі за 3000 грн? | Where (>), OrderByDescending, ThenBy, Select, ToList |
| ByStatus | Скільки замовлень кожного статусу і яка їх сума? | GroupBy, Select, Count, Sum, OrderByDescending, ThenBy, ToList |
| Totals | Які кількість, загальна сума, середній чек і максимум? | ToList, Sum, Average, Max |
| LowTotal | Які замовлення дешевші за 1000 грн? | Where (<), OrderBy, ThenBy, Select, ToList |
| TopOrders | Які 5 замовлень найбільші за сумою? | OrderByDescending, ThenBy, Take, Select, ToList |

Методи приймають IEnumerable<Order>. Табличні звіти повертають IReadOnlyList незмінних record-DTO: OrderReportLine, CustomerSummary та GroupSummary; Totals повертає іменований кортеж, як у методичці. Грошові показники мають тип decimal. У Program.cs немає Where/GroupBy; у Core/Reports немає Console, файлового вводу-виводу чи AsParallel.

### Групування з проєкцією

```csharp
source.GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
    .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
    .OrderByDescending(s => s.TotalAmount)
    .ThenBy(s => s.Key, StringComparer.Ordinal)
    .ToList();
```

g.Key — назва статусу, g.Count() — кількість замовлень цієї групи, g.Sum(...) — їхня загальна сума. ThenBy забезпечує визначений порядок при однакових основних показниках; для топу клієнтів другим ключем є CustomerId, для замовлень — Id.

### Запуск

```bash
dotnet build
dotnet run --project src/Cli -- report-top
dotnet run --project src/Cli -- report-low
dotnet run --project src/Cli -- report-expensive
dotnet run --project src/Cli -- report-groups
dotnet run --project src/Cli -- report-sum
dotnet run --project src/Cli -- --file report-groups
dotnet run --project src/Cli -- --cached report-top
```

Без назви команди запускається report-top. --file та --cached передаються StoreFactory і не сприймаються як назви звітів. Файловий режим читає data/orders.json відносно каталогу зібраного застосунку: якщо файлу немає, сховище порожнє; SampleData використовується лише в режимі пам'яті. Імпортери попередніх лабораторних доступні як методи Core/Import; поточна точка входу запускає звіти.

### Відкладене виконання

Демо виконується перед вибором підкоманди звіту, як у прикладі методички. Для демонстрації достатньо запустити будь-який звіт:

```powershell
dotnet run --project src/Cli -- report-sum
```

DeferredFilter створює запит Where, але не перебирає джерело в момент оголошення. Перший Count() виконує запит і знаходить 5 замовлень із сумою меншою за 1000 грн; після додавання локального замовлення на 200 грн повторний Count() знову виконує запит і повертає 6. Виклик ToList() матеріалізує результат, тому після source.Clear() snapshot.Count залишається 6. Демо працює з локальним списком, не змінює сховище; цей знімок фіксує склад списку, але не створює глибоких копій об'єктів.

### Фактичний вивід команд (сховище в пам'яті)

Перед кожним звітом друкується демо:

```text
Сховище: InMemoryOrderStore
Малих замовлень (< 1000 грн) до змін: 5
Малих замовлень після змін: 6
У знімку лишилось: 6
```

report-top

```text
=== Звіт: Топ-5 замовлень за сумою ===
 ORD-001    CUST-01    Confirmed   Рядків:  2  Сума: 46200,00 грн
 ORD-002    CUST-02    Confirmed   Рядків:  1  Сума: 19000,00 грн
 ORD-006    CUST-05    Confirmed   Рядків:  2  Сума:  6450,00 грн
 ORD-010    CUST-08    Confirmed   Рядків:  1  Сума:  4200,00 грн
 ORD-011    CUST-09    Confirmed   Рядків:  1  Сума:  3700,00 грн

=== Звіт: Топ-5 клієнтів за сумою ===
 Клієнт: CUST-01     Замовлень:  3  Сума замовлень:  46910,00 грн
 Клієнт: CUST-02     Замовлень:  2  Сума замовлень:  20120,00 грн
 Клієнт: CUST-05     Замовлень:  2  Сума замовлень:   7450,00 грн
 Клієнт: CUST-03     Замовлень:  2  Сума замовлень:   4700,00 грн
 Клієнт: CUST-08     Замовлень:  2  Сума замовлень:   4640,00 грн
```

report-low

```text
=== Звіт: Замовлення до 1000 грн (невеликі чеки) ===
 ORD-005    CUST-01    Draft       Рядків:  1  Сума:   350,00 грн
 ORD-017    CUST-01    Draft       Рядків:  1  Сума:   360,00 грн
 ORD-018    CUST-07    Cancelled   Рядків:  1  Сума:   360,00 грн
 ORD-019    CUST-08    Cancelled   Рядків:  1  Сума:   440,00 грн
 ORD-020    CUST-09    Cancelled   Рядків:  1  Сума:   480,00 грн
```

report-expensive

```text
=== Звіт: Замовлення дорожче 3000 грн ===
 ORD-001    CUST-01    Confirmed   Рядків:  2  Сума: 46200,00 грн
 ORD-002    CUST-02    Confirmed   Рядків:  1  Сума: 19000,00 грн
 ORD-006    CUST-05    Confirmed   Рядків:  2  Сума:  6450,00 грн
 ORD-010    CUST-08    Confirmed   Рядків:  1  Сума:  4200,00 грн
 ORD-011    CUST-09    Confirmed   Рядків:  1  Сума:  3700,00 грн
 ORD-012    CUST-10    Draft       Рядків:  1  Сума:  3600,00 грн
 ORD-003    CUST-03    Draft       Рядків:  2  Сума:  3250,00 грн
 ORD-004    CUST-04    Confirmed   Рядків:  1  Сума:  3200,00 грн
```

report-groups

```text
=== Звіт: Розподіл замовлень за статусом ===
 Confirmed   замовлень:  9  сумарно:   87600,00 грн
 Draft       замовлень:  8  сумарно:   14030,00 грн
 Cancelled   замовлень:  3  сумарно:    1280,00 грн
```

report-sum

```text
=== Звіт: Підсумкові показники ===
 позицій 20, всього 102910,00 грн, середнє 5145,50 грн, макс 46200,00 грн
```

## Лабораторна 7: додаткове завдання

Виконано всі чотири опційні пункти методички для предметної області «Замовлення».

### 1. GroupJoin: клієнти та замовлення

Метод `ReportService.CustomerOrders(customers, orders)` з'єднує дві окремі колекції за Customer.Id та Order.CustomerId без урахування регістру. Результат — незмінний DTO CustomerOrderSummary із повним ім'ям клієнта, кількістю замовлень і загальною сумою. GroupJoin зберігає клієнтів без замовлень; для них Count і Sum дорівнюють нулю. Замовлення, клієнта якого немає у переданому довіднику, у цей звіт не потрапляє; усі статуси замовлень враховуються.

```csharp
customers.GroupJoin(orders,
    c => c.Id, o => o.CustomerId,
    (c, matches) => new CustomerOrderSummary(
        c.Id, c.FullName, matches.Count(), matches.Sum(o => o.Total)),
    StringComparer.OrdinalIgnoreCase)
```

SampleData.Customers() містить 11 клієнтів, зокрема CUST-11 без замовлень. Це окремий демонстраційний довідник, не відновлення імен із CustomerId. У режимі --file звіт так само використовує цей довідник: клієнтів з іншими ID потрібно окремо додати до нього.

```bash
dotnet run --project src/Cli -- report-customers
```

Фрагмент фактичного результату:

```text
CUST-01    Іван Петренко          замовлень:  3  сума:   46910,00 грн
CUST-02    Марія Коваль           замовлень:  2  сума:   20120,00 грн
CUST-11    Віктор Поліщук         замовлень:  0  сума:       0,00 грн
```

### 2. Власний метод розширення TopBy<T>

ReportExtensions.TopBy приймає послідовність довільного типу T, селектор числового показника Func<T, decimal>, селектор унікального рядкового ключа і кількість n. Для грошей використано decimal замість int із прикладу методички. Усередині виконуються OrderByDescending, ThenBy з StringComparer.Ordinal та Take; n = 0 дає порожню вибірку, від'ємне n відхиляється. Запит залишається відкладеним до матеріалізації у звіті.

Метод використовується у двох звітах:

```csharp
source.TopBy(o => o.Total, o => o.Id, n)
customerSummaries.TopBy(c => c.TotalSpent, c => c.CustomerId, n)
```

### 3. Групування в синтаксисі запитів

ByStatusQuery повертає ті самі DTO в тому самому порядку, що й ByStatus. Назви статусів походять із enum, тому ключі вже мають однаковий регістр. Додаткове сортування після запиту явно використовує Ordinal, як і методний варіант.

```csharp
(from order in source
 group order by order.Status.ToString() into statusGroup
 select new GroupSummary(statusGroup.Key, statusGroup.Count(),
     statusGroup.Sum(order => order.Total)) into summary
 orderby summary.TotalAmount descending
 select summary)
 .ThenBy(summary => summary.Key, StringComparer.Ordinal)
 .ToList();
```

У методному записі GroupBy → Select → OrderByDescending явно показує послідовність операторів і дозволяє зручно передати компаратори. У синтаксисі запитів group ... by ... into краще підкреслює утворення групи, а select ... into дозволяє далі працювати вже з DTO. Обидва записи використовують LINQ to Objects; рівність результатів перевірена також для порожньої колекції й однакових сум груп.

```bash
dotnet run --project src/Cli -- report-groups-query
```

```text
Confirmed   замовлень:  9  сумарно:   87600,00 грн
Draft       замовлень:  8  сумарно:   14030,00 грн
Cancelled   замовлень:  3  сумарно:    1280,00 грн
```

### 4. Stopwatch на 100 000 замовлень

```bash
dotnet run --project src/Cli -c Release -- report-benchmark
```

SampleData.GenerateOrders(100_000) створює відтворювані замовлення з унікальними ID, різними сумами та трьома статусами. ReportBenchmark порівнює ExpensiveOrders (Where → OrderByDescending → ThenBy → Select → ToList) і ExpensiveOrdersWithIntermediateList (Where → **ToList** → OrderByDescending → ThenBy → Select → ToList). Обидва варіанти виконуються повністю та повертають однакові DTO; кінцевий ToList потрібен в обох, відрізняється лише проміжна матеріалізація.

Генерація, перевірка рівності й консольний вивід не входять у вимірювання. Перед серією є три прогрівальні запуски кожного варіанта. Потім Stopwatch вимірює сім запусків кожного, порядок варіантів чергується, показник — медіана. Вимірювання виконане в Release на Windows x64 / .NET 10; це навчальний експеримент, а не універсальний рейтинг швидкодії.

Фактичний запуск:

| Варіант | Медіана, мс | Рядків результату |
|---|---:|---:|
| Без проміжного ToList | 52,258 | 87290 |
| З проміжним ToList | 52,095 | 87290 |

У цьому запуску час майже однаковий: різниця близько 0,3%, тож висновок про прискорення робити не можна. Проміжний ToList створює додатковий список посилань на відібрані замовлення, але не скорочує сортування чи обчислення звіту. Повторний запуск може дати інші числа через JIT, GC і навантаження системи; без повторного використання проміжного результату цей список тут не потрібен.

## Лабораторна 8: async/await, скасування, доменні винятки, unit-тести Core

### 1. Нові компоненти та асинхронність
- **`DomainException` (`src/Core/DomainException.cs`):** доменний виняток для порушення бізнес-правил (наприклад, спроба повторно підтвердити замовлення, підтвердити порожнє замовлення або перевищення ліміту відкритих замовлень). Перевірки вхідних аргументів лишаються `ArgumentException` та `ArgumentOutOfRangeException`.
- **`IOrderImporter` (`src/Core/Abstractions/`):** асинхронний контракт для читання замовлень `Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)`.
- **`JsonOrderImporter` (`src/Core/Storage/`):** справжнє неблокувальне асинхронне читання за допомогою `FileStream(..., useAsync: true)` та `JsonSerializer.DeserializeAsync(stream, cancellationToken: ct)` без використання псевдоасинхронного `Task.Run`. Підтримує перевірку `CancellationToken` перед читанням і на кожній ітерації розбору.
- **`OrderService.ImportAsync` (`src/Core/Services/OrderService.cs`):** асинхронний метод імпорту з підтримкою `CancellationToken`. Гарантує ідемпотентність (пропускає замовлення, які вже є в сховищі) та піднімає подію `Changed` для кожного доданого замовлення.
- **`Program.cs` (`src/Cli/Program.cs`):** підтримує top-level `await`, перехоплення комбінації `Ctrl+C` через `Console.CancelKeyPress` та роздільну обробку `OperationCanceledException` (стоїть першим), `DomainException` та `FileNotFoundException`.

### 2. Запуск тестів та перевірка скасування

#### Запуск модульних тестів:
```bash
dotnet test
```
Усі 18 тестових випадків проєкту `tests/Core.Tests` проходять успішно (інваріанти, перевірка події, звіти, скасування, імпорт із тимчасових файлів).

#### Запуск імпорту файлу:
```bash
dotnet run --project src/Cli -- --file
```
Вивід: `Імпортовано записів: 19`

#### Перевірка сценарію скасування (CancellationToken / Ctrl+C):
```bash
dotnet run --project src/Cli -- --file --timeout
```
Вивід: `Імпорт скасовано.` (застосунок коректно перехоплює `OperationCanceledException` без аварійного завершення та без виведення стектрейсу).

#### Перевірка обробки доменної помилки в пошкодженому файлі:
```bash
dotnet run --project src/Cli -- data/sample_invalid.json
```
Вивід: `Дані відхилено: Запис 1: Ідентифікатор замовлення обов'язковий (Parameter 'id')`

### 3. Додаткові завдання (Лабораторна 8)

1. **Потоковий імпорт через `IAsyncEnumerable<Order>` та `await foreach`:**
   - У `IOrderImporter` та `JsonOrderImporter` реалізовано метод `ReadAllAsync`. Атрибут `[EnumeratorCancellation]` стоїть на параметрі токена в реалізації.
   - Застосовано `JsonSerializer.DeserializeAsyncEnumerable<OrderDto>` для неблокувального читання великих JSON-масивів без завантаження всього списку в оперативну пам'ять одразу.
   - У `OrderService` додано `ImportStreamAsync`, який обходить елементи через `await foreach`.
   - Перевірка через CLI:
     ```bash
     dotnet run --project src/Cli -- --stream
     ```
     Вивід: `Імпортовано через потік (IAsyncEnumerable): 19`

2. **Зв'язані токени (`CreateLinkedTokenSource`) та доведення спрацювання таймауту:**
   - У CLI додано демонстрацію `--linked-timeout`:
     ```bash
     dotnet run --project src/Cli -- --linked-timeout
     ```
   - У `ImportTests.cs` реалізовано unit-тест `ImportAsync_WithLinkedToken_ProvesTimeoutTriggered`, який запускає таймер під час очікування в імпортері та перевіряє джерело скасування:
     - `timeoutCts.IsCancellationRequested == true`
     - `userCts.IsCancellationRequested == false`
     - `linkedCts.IsCancellationRequested == true`

3. **`IProgress<int>` для звіту про хід імпорту (відсотки):**
   - У методи `ImportAsync` та `ImportStreamAsync` додано параметр `IProgress<int>? progress = null`.
   - Пакетний імпорт знає загальну кількість і показує відсоток. Потоковий імпорт показує кількість прочитаних записів, бо загальна кількість наперед невідома.
   - У `Program.cs` підписник `IProgress<int>` оновлює рядок у реальному часі: `\r[Хід імпорту:  XX%]`.
   - Реалізовано тест `ImportAsync_ReportsProgressToSubscriber`.

4. **Звіт про тестове покриття (`XPlat Code Coverage`):**
   - Команда запуску збору покриття:
     ```bash
     dotnet test tests/Core.Tests/Core.Tests.csproj --collect:"XPlat Code Coverage"
     ```
   - **Фактичне покриття Core у Release:** 30,19% рядків (199 із 659) і 26,45% гілок (59 із 223).
   - Тести охоплюють сценарії замовлень, події, LINQ-звіти й асинхронний імпорт. Допоміжні компоненти попередніх робіт, зокрема синхронні імпортери, файлове сховище та інформація про середовище, залишаються поза перевірками цієї лабораторної. Методичка не встановлює мінімального відсотка покриття.
