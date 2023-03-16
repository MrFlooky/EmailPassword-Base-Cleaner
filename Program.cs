using config;
using consoleutils;
using fileutils;
using System.Text.RegularExpressions;

Console.WriteLine("Created by @SilverBulletRU");
string[] files = FileUtils.files;
Config.CheckFiles(files);
if (Config.CheckConfig())
    Config.PromptConfig();

while (true) {
    Console.Title = "Idle.";
    Console.WriteLine("Drop a file here: ");
    string? path = Console.ReadLine().Replace("\"", "");
    Console.Clear();
    if (!(Regex.IsMatch(path, @"^[A-Za-z]:(?:\\[^\\\/:*?""<>\|]+)*\\[^\\\/:*?""<>\|]+\.txt$") && File.Exists(path))) {
        Console.WriteLine("Drop valid .txt file that exists.");
        continue;
    }
    Config.MainWork(path).Wait();
    Console.Write("Do you want to exit?");
    ConsoleUtils.WriteColorizedYN();
    path = Console.ReadLine();
    if (path == "y" || path == "Y") break;
    else Console.Clear();
}