using config;
using consoleutils;
using System.Diagnostics;

Console.WriteLine("Created by bhf.gg/members/217675");
ConsoleUtils.SetupContextMenu();
string input = "", output = "";
if (args.Length > 0)
    Config.workWithArgs = ConsoleUtils.WorkWithArgs(args, ref input, ref output);
Config.CheckFiles(Config.files);
if (Config.CheckConfig())
    Config.PromptConfig();

while (true) {
    Console.Title = "Idle.";
    Config.path = input;
    if (!Config.workWithArgs) {
        Console.WriteLine("Drop a file here: ");
        Config.path = Console.ReadLine().Replace("\"", "");
        Console.Clear();
    }
    if (Config.path.ToLower() == "link") {
        Console.WriteLine("Hey! It's not valid path :)");
        Process.Start(new ProcessStartInfo("cmd", $"/c start http://bhf.gg/members/217675/") { CreateNoWindow = true });
        continue;
    }
	if (Path.GetExtension(Config.path) != ".txt" || !File.Exists(Config.path)) {
        Console.WriteLine("Give me a valid .txt file that exists.");
        if (!Config.workWithArgs) continue;
        else break;
    }
    Config.MainWork(output).Wait();
    if (!Config.workWithArgs) {
        Console.Write("Do you want to exit?");
        ConsoleUtils.WriteColorizedYN();
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Y) break;
        else Console.Clear();
    }
    else break;
}