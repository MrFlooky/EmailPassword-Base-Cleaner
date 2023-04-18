using config;
using Microsoft.Win32;
using contextfunctions;

namespace consoleutils {

	internal class ConsoleUtils {
		private const string baseKeyPath = @"*\shell\BaseCleaner_by_flooks";
		private static readonly string[] subCommands = { "bCleanDups", "bCleanSameMails", "bCleanSamePass", "bMPtoLP", "bRandomize", "bGetPasswords", "bGetMails", "bGetLogins", "bGetDomains", "bSplitLines", "bSplitSize", "bReplaceSplitter" };
		private static readonly string[] nameCommands = { "Delete duplicate lines", "Delete duplicate mails", "Delete duplicate passwords", "MailPass to LoginPass", "Randomize lines", "Get passwords", "Get mails", "Get logins", "Get domains", "Split by lines", "Split by size", "Replace ; to :" };
		private static readonly Dictionary<string, string> firstValues = new() { { "Icon", "SHELL32.dll,134" }, { "MUIVerb", "Clean BD" }, { "SubCommands", string.Join(";", subCommands) } };

		public static void WriteColorized(string text, ConsoleColor color) {
			Console.ForegroundColor = color;
			Console.Write(text);
			Console.ResetColor();
		}

		public static void WriteColorizedYN() {
			Console.Write(" ( ");
			WriteColorized("y", ConsoleColor.Red);
			Console.Write(" / ");
			WriteColorized("n", ConsoleColor.Green);
			Console.WriteLine(" ):");
		}

		public static bool WorkWithArgs(string[] args, ref string input, ref string output) {
			int length = args.Length;
			if (length < 2 || length > 4 || !args.Contains("-input")) {
				Console.WriteLine("Wrong arguments!\nUse \"-input file.txt (-output file.txt)\" for correct work of the app.\n");
				Console.ReadLine();
				Environment.Exit(0);
			}
			if (length == 2) {
				input = args[1];
				if (!File.Exists(input)) {
					Console.WriteLine("Input file not found!");
					Console.ReadLine();
					Environment.Exit(0);
				}
				output = $"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_result.txt";
				Config.workWithcontext = true;
				return true;
			}
			if (length == 3 && subCommands.Contains(args[2])) {
				ContextFunctions.input = args[1];
				Console.Title = args[2][1..];
				switch (args[2]) {
					case "bCleanDups":
						ContextFunctions.DeleteDuplicates();
						break;
					case "bCleanSameMails":
						ContextFunctions.DeleteDuplicateEmails();
						break;
					case "bCleanSamePass":
						ContextFunctions.DeleteDuplicatePasswords();
						break;
					case "bMPtoLP":
						ContextFunctions.MailPassToLoginPass();
						break;
					case "bRandomize":
						ContextFunctions.RandomizeLines();
						break;
					case "bGetPasswords":
						ContextFunctions.GetPasswords();
						break;
					case "bGetMails":
						ContextFunctions.GetMails();
						break;
					case "bGetLogins":
						ContextFunctions.GetLogins();
						break;
					case "bGetDomains":
						ContextFunctions.GetDomains();
						break;
					case "bSplitLines":
						ContextFunctions.SplitByLines();
						break;
					case "bSplitSize":
						ContextFunctions.SplitBySize();
						break;
					case "bReplaceSplitter":
						ContextFunctions.ReplaceSplitter();
						break;
				}
			}
			input = args[1];
			output = args[3];
			if (args[2] == "-input") {
				input = args[3];
				output = args[1];
			}
			if (!File.Exists(input)) {
				Console.WriteLine("Input file not found!");
				Console.ReadLine();
				Environment.Exit(0);
			}
			return true;
		}

		public static bool CheckRegistry() {
			RegistryKey subKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default).OpenSubKey(baseKeyPath);
			if (subKey == null) return false;
			if ((string)Registry.GetValue($@"HKEY_CLASSES_ROOT\{baseKeyPath}", "SubCommands", null) != string.Join(";", subCommands)) return false;
			foreach (string key in subCommands) {
				string path = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\{key}";
				subKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(path);
				if (subKey == null) return false;
				path = (string)Registry.GetValue($@"HKEY_LOCAL_MACHINE\{path}\command", null, null);
				if (path == null || !File.Exists(path.Split("\" \"")[0].Trim('\"'))) return false;
			}
			return true;
		}

		public static void SetupContextMenu() {
			if (CheckRegistry()) return;
			Dictionary<string, string> additionalCommands = new();
			for (int i = 0; i < subCommands.Length; ++i)
				additionalCommands.Add(subCommands[i], nameCommands[i]);

			foreach (string key in firstValues.Keys)
				Registry.SetValue(@"HKEY_CLASSES_ROOT\" + baseKeyPath, key, firstValues[key]);

			foreach (string key in additionalCommands.Keys) {
				string path = $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\{key}";
				Registry.SetValue(path, "MUIVerb", additionalCommands[key]);
				Registry.SetValue($@"{path}\command", null, $"\"{Config.exePath}\" \"-input\" \"%1\" \"{key}\"");
			}
			Console.WriteLine("Software has been successfully added to the context menu!");
		}
	}
}