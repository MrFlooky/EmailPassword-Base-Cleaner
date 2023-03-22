using config;
using consoleutils;
using fileutils;

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
    if (!(Path.GetExtension(path) == ".txt" && File.Exists(path))) {
        Console.WriteLine("Drop valid .txt file that exists.");
        continue;
    }
    Config.MainWork(path).Wait();
    Console.Write("Do you want to exit?");
    ConsoleUtils.WriteColorizedYN();
    var key = Console.ReadKey();
    if (key.Key == ConsoleKey.Y) break;
    else Console.Clear();
}