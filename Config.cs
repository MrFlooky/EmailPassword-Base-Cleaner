using consoleutils;
using fileutils;
using main;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace config {

	internal class Config {
		public static string path = "";
		public static string currentDir = AppDomain.CurrentDomain.BaseDirectory;
		public static string exePath = Process.GetCurrentProcess().MainModule.FileName;
		public static string[] files = new string[5] { $"{currentDir}BadDNS",
			$"{currentDir}FixDomains", $"{currentDir}FixZone", $"{currentDir}GoodDNS",
			$"{currentDir}TempMails" };
		public static string configFile = $"{currentDir}config.cfg";

		public static bool checkDNS = true;
		public static bool fixDomains = true;
		public static bool fixDotsGmail = true;
		public static bool fixDotsYandex = true;
		public static bool fixPlus = true;
		public static bool removeEmptyPass = true;
		public static bool removeEqualLoginPass = true;
		public static bool removeTempMail = true;
		public static bool removeXumer = true;
		public static bool removeXXXX = true;
		public static bool cleanSameMails = true;
		public static bool query = true;
		public static int threads = Environment.ProcessorCount;


		private static readonly string domainPartRegex = @"(?:[A-Za-z\d][A-Za-z\d-]*\.)+([A-Za-z]?[A-Za-z\d]{1,10}|xn--[a-z\d]{4,18})";
		private static readonly string loginPartRegex = @"[A-Za-z\d](?!.*[-._]{2,})(?!(?:[^+]*\+){2})[\w.+-]*";
		private static readonly string mailPartRegex = loginPartRegex + '@' + domainPartRegex;
		
		//public static Regex hashRegex = new(@"^(?=(?:.*[a-f]){3})[\da-f$.]{16,}$");
		
		public static Regex loginRegex = new('^' + loginPartRegex + '$');
		public static Regex loginpassRegex = new('^' + mailPartRegex + "[;:].*?$");
		public static Regex loginpassPartialRegex = new(mailPartRegex + "[;:].*?(?:[;:]|$)");
		public static Regex mailRegex = new('^' + mailPartRegex + '$');
		
		public static Regex domainCheck = new(@"@([\w-]+(?:\.[\w-]+)+)(?::|$)");
		public static Regex domainRegex = new('^' + domainPartRegex + '$');
		
		public static Dictionary<string, string> fixDomainsDictionary = new();
		public static Dictionary<string, string> fixZonesDictionary = new();
		public static HashSet<string> domains = new();
		public static HashSet<string> zones = new();
		public static ParallelOptions options = new() { MaxDegreeOfParallelism = threads };

		public static void ThreadsInit() {
			if (threads == 1) return;
			if (threads <= 4) {
				threads -= 1;
				return;
			}
			if (threads >= 8) {
				threads /= 2;
				return;
			}
			threads -= 2;
		}
		
		public static bool CheckConfig() {
			if (!File.Exists(configFile)) {
				SetConfig("Config file not found.");
				if (!Main.workWithArgs)
					Console.Clear();
				return false;
			} ь
			try {
				var settings = new Dictionary<string, string>();
				foreach (string line in File.ReadLines(configFile)) {
					string[] parts = line.Split(": ");
					settings[parts[0]] = parts[1];
				}
				checkDNS = bool.Parse(settings["checkDNS"]);
				removeXXXX = bool.Parse(settings["removeXXXX"]);
				removeEmptyPass = bool.Parse(settings["removeEmptyPass"]);
				removeXumer = bool.Parse(settings["removeXumer"]);
				removeEqualLoginPass = bool.Parse(settings["removeEqualLoginPass"]);
				removeTempMail = bool.Parse(settings["removeTempMail"]);
				fixDotsGmail = bool.Parse(settings["fixDotsGmail"]);
				fixDotsYandex = bool.Parse(settings["fixDotsYandex"]);
				fixPlus = bool.Parse(settings["fixPlus"]);
				fixDomains = bool.Parse(settings["fixDomains"]);
				cleanSameMails = bool.Parse(settings["cleanSameMails"]);
				query = bool.Parse(settings["query"]);
				threads = int.Parse(settings["threads"]);
				options = new() { MaxDegreeOfParallelism = threads };
			}
			catch {
				SetConfig("Config file is corrupted.");
				if (!Main.workWithArgs)
					Console.Clear();
				return false;
			}
			return true;
		}

		public static void CheckFiles() {
			if (files.Any(file => !File.Exists(file))) {
				foreach (string file in files)
					if (!File.Exists(file))
						Console.WriteLine($"\"{Path.GetFileName(file)}\" file not found.");
				if (!Main.workWithArgs)
					Console.ReadLine();
				Environment.Exit(0);
			}
			fixDomainsDictionary = FileUtils.WriteToDictionary(files[1]);
			fixZonesDictionary = FileUtils.WriteToDictionary(files[2]);
		}

		public static void PromptConfig() {
			if (!query) return;
			Console.Write("Do you want to change config?");
			ConsoleUtils.WriteColorizedYN();
			bool answer = SetPartConfigBool("");
			if (answer) {
				SetConfig("Changing config file.");
				Console.Clear();
			}
		}

		public static void SetConfig(string msg) {
			if (Main.workWithArgs) {
				msg += " Reload app without arguments to setup config.";
				Console.WriteLine(msg);
				Environment.Exit(0);
			}
			Console.WriteLine(msg);
			Console.WriteLine("Note: If all answers is \"n\", program will fix only syntax errors.");
			var settings = new Dictionary<string, bool>();
			var prompts = new Dictionary<string, string> {
				["checkDNS"] = "Check DNS of domains in emails? y / n",
				["removeXXXX"] = "Remove \"xxxx\" in google mails? y / n",
				["removeEmptyPass"] = "Remove empty passwords? y / n",
				["removeXumer"] = "Remove xrumer (spam) mails? y / n",
				["removeEqualLoginPass"] = "Remove same login and pass? y / n",
				["removeTempMail"] = "Remove temp mails? y / n",
				["fixDotsGmail"] = "Remove dots in google mails? y / n",
				["fixDotsYandex"] = "Replace dots to \"-\" in yandex mails? y / n",
				["fixPlus"] = "Remove all after \"+\" in mails? y / n",
				["fixDomains"] = "Fix domains and domain zones? y / n",
				["cleanSameMails"] = "Remove lines with generated lines? y / n",
				["query"] = "Request a change of settings in the future? (! THIS IS PERMANENT OPTION !) y / n"
			};
			bool isAllTrue = SetPartConfigBool("Set all \"y\"?");
			foreach (var item in prompts) {
				string name = item.Key;
				settings[name] = isAllTrue || name == "query" || SetPartConfigBool(item.Value);
			}
			int threadsTemp = Environment.ProcessorCount;
			while (true) {
				int thr = SetPartConfigInt("How many threads of your processor to use? (integer number, 0 - auto amount):");
				if (thr > threadsTemp || thr < 0) {
					Console.WriteLine("Invalid input, try again.");
					continue;
				}
				if (thr == 0) threadsTemp = threads;
				else threadsTemp = thr;
				options = new() { MaxDegreeOfParallelism = threadsTemp };
				break;
			}
			string write = "";
			foreach (var item in settings) {
				string name = item.Key;
				bool value = item.Value;
				write += $"{name}: {value}\n";
			}
			write += "threads: " + threadsTemp;
			File.WriteAllText(configFile, write);
		}

		public static bool SetPartConfigBool(string msg) {
			while (true) {
				if (msg != "")
					Console.WriteLine(msg);
				var key = Console.ReadKey();
				Console.WriteLine();
				if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.N)
					return key.Key == ConsoleKey.Y;
				Console.WriteLine("Invalid input, try again.");
			}
		}

		public static int SetPartConfigInt(string msg) {
			while (true) {
				if (msg != "")
					Console.WriteLine(msg);
				string key = Console.ReadLine();
				Console.WriteLine();
				if (int.TryParse(key, out int result))
					return result;
				Console.WriteLine("Invalid input, try again.");
			}
		}
	}
}