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
│   └── sample_mixed.csv
│
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   │
    │   ├── Dto/
    │   │   ├── ProductDto.cs
    │   │   ├── CustomerDto.cs
    │   │   ├── OrderDto.cs
    │   │   └── ImportResult.cs
    │   │
    │   ├── Domain/
    │   │   ├── Customer.cs
    │   │   ├── DomainMapper.cs
    │   │   ├── Order.cs
    │   │   ├── OrderLine.cs
    │   │   ├── OrderService.cs
    │   │   └── OrderStatus.cs
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

## Інваріанти доменної моделі (Лабораторна 4)

Сутність **Order**:
1. Ідентифікатор замовлення та клієнта не можуть бути порожніми (`ArgumentException`).
2. Не можна додати рядок з від'ємною ціною або від'ємною/нульовою кількістю (`ArgumentOutOfRangeException`).
3. Не можна змінювати замовлення (додавати рядки) після його підтвердження (`InvalidOperationException`).
4. Не можна підтвердити порожнє замовлення без рядків (`InvalidOperationException`).