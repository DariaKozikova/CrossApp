# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення.

Сутності: Customer (клієнт), Product (товар), Order (замовлення), OrderLine (рядок замовлення).

Призначення: оформлення замовлень і підрахунок сум.

## Середовище
.NET SDK 10.0.400 Windows 11 x64 (win-x64) Ubuntu 24.04.4

## Структура solution

```
CrossApp/
  CrossApp.slnx
  README.md
  .gitignore
  src/
    Core/
      Core.csproj
      EnvironmentInfo.cs
    Cli/
      Cli.csproj
      Program.cs
```
`Core` — class library (немає точки входу, результат збірки — `Core.dll`).
`Cli` — консольний застосунок, має `ProjectReference` на `Core`.
Залежність одностороння: `Cli → Core`. `Core` ніколи не посилається на `Cli`.


### Збірка
```bash
dotnet build
dotnet build src/Core/Core.csproj
```

### Запуск
```bash
dotnet run --project src/Cli
```

### Multi-targeting (Крок 5)
У `src/Core/Core.csproj`: `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`
```bash
dotnet build
```
### Публікація (Крок 6)
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
dotnet publish src/Cli -c Release -r linux-x64 --self-contained false
```

Вимірювання розміру:
```powershell
(Get-ChildItem -Recurse src\Cli\bin\Release\net10.0\win-x64\publish | Measure-Object Length -Sum).Sum / 1MB
```

### Запуск з publish (Крок 7)
```powershell
cd src\Cli\bin\Release\net10.0\win-x64\publish
.\Cli.exe
```
### Додаткове завдання
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true
```

## Порівняння режимів публікації

| RID       | Режим                | Розмір publish | Потрібен runtime |
|-----------|----------------------|----------------|------------------|
| win-x64   | self-contained       | ~78 МБ         | ні               |
| win-x64   | framework-dependent  | ~0.2 МБ        | так (.NET 10)    |
| linux-x64 | self-contained       | ~78.8 МБ       | ні               |
| linux-x64 | framework-dependent  | ~0.11 МБ       | так (.NET 10)    
