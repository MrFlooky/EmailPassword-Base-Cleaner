using config;
using consoleutils;
using main;
using System.Diagnostics;

Console.WriteLine("Created by bhf.gg/members/217675");
Config.ThreadsInit();
ConsoleUtils.SetupContextMenu();
string input = "", output = "";
if (args.Length > 0)
	Main.workWithArgs = ConsoleUtils.WorkWithArgs(args, ref input, ref output);
Config.CheckFiles();
if (Config.CheckConfig())
    Config.PromptConfig();

while (true) {
    Console.Title = "Idle.";
    Config.path = input;
    if (!Main.workWithArgs) {
        Console.WriteLine("Drop a file here: ");
        Config.path = Console.ReadLine().Replace("\"", "");
        Console.Clear();
    }
    if (Config.path.ToLower() == "link") {
        Console.WriteLine("Hey! It's not a valid path :)");
        Process.Start(new ProcessStartInfo("cmd", $"/c start http://bhf.gg/members/217675/") { CreateNoWindow = true });
        continue;
    }
	if (Path.GetExtension(Config.path) != ".txt" || !File.Exists(Config.path)) {
        Console.WriteLine("Give me a valid .txt file that exists.");
        if (!Main.workWithArgs) continue;
        else break;
    }
	Main.MainWork(output).Wait();
    if (!Main.workWithArgs) {
        Console.Write("Do you want to exit?");
        ConsoleUtils.WriteColorizedYN();
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Y) break;
        else Console.Clear();
    }
    else break;
}