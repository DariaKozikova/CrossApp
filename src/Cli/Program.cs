using System.Runtime.InteropServices;
using System.Text.Json;
using System;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var info = new
{
    OSDescription = RuntimeInformation.OSDescription,
    EnvironmentOS = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Замовлення (Customer, Product, Order, OrderLine)"
};

if (args.Contains("--json"))
{
    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студентка: Козікова Дар'я, група ФЕІ-36с");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)  : {info.OSDescription}");
    Console.WriteLine($"ОС (Environment)    : {info.EnvironmentOS}");
    Console.WriteLine($"Архітектура процесу : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)    : {info.DotNetVersion}");
    Console.WriteLine($"Runtime             : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку  : {info.AppDirectory}");
    Console.WriteLine($"Поточний каталог    : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область   : {info.Domain}");
}