using config;
using consoleutils;
using fileutils;

Console.WriteLine("Created by @NoNameDevLog");

string input = "", output = "";
if (args.Length > 0)
    Config.workWithArgs = ConsoleUtils.WorkWithArgs(args, ref input, ref output);

string[] files = FileUtils.files;
Config.CheckFiles(files);

if (Config.CheckConfig())
    Config.PromptConfig();

while (true) {
    Console.Title = "Idle.";
    string path = input;
    if (!Config.workWithArgs) {
        Console.WriteLine("Drop a file here: ");
        path = Console.ReadLine().Replace("\"", "");
        Console.Clear();
    }
    if (Path.GetExtension(path) != ".txt" || !File.Exists(path)) {
        Console.WriteLine("Give me a valid .txt file that exists.");
        if (!Config.workWithArgs) continue;
        else break;
    }
    Config.MainWork(path, output).Wait();
    if (!Config.workWithArgs) {
        Console.Write("Do you want to exit?");
        ConsoleUtils.WriteColorizedYN();
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Y) break;
        else Console.Clear();
    }
    else break;
}