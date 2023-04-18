using config;
using Microsoft.Win32;

namespace consoleutils {

	internal class ConsoleUtils {
		private const string BaseKeyPath = @"*\shell\BaseCleaner_by_flooks";
		private const string CommandKeyPath = BaseKeyPath + @"\command";

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
			if (length < 2 || length > 4 || length == 3 || !args.Contains("-input")) {
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
				Config.workWithContex = true;
				return true;
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

		private static void AddCommandInRegistry(bool whatIs) {
			RegistryKey subKey = Registry.ClassesRoot.CreateSubKey(CommandKeyPath);
			subKey.SetValue(null, $"\"{Config.exePath}\" \"-input\" \"%1\"");
			if (whatIs)
				Console.WriteLine("Fixed context menu!");
			else
				Console.WriteLine("Software is successfully added to the context menu!");
		}

		public static void SetupContexMenu() {
			RegistryKey subKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default).OpenSubKey(BaseKeyPath);
			if (subKey == null) {
				subKey = Registry.ClassesRoot.CreateSubKey(BaseKeyPath);
				subKey.SetValue(null, "Clean BD");
				subKey.SetValue("Icon", "SHELL32.dll,134");
				AddCommandInRegistry(false);
				return;
			}
			if (RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default).OpenSubKey(CommandKeyPath) == null) {
				AddCommandInRegistry(false);
				return;
			}
			string value = (string)Registry.GetValue("HKEY_CLASSES_ROOT\\" + CommandKeyPath, null, null);
			if (!File.Exists(value.Split("\" \"")[0].Trim('\"')))
				AddCommandInRegistry(true);
		}
	}
}