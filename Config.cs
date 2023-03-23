using consoleutils;
using fileutils;
using lineutils;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace config {

	internal class Config {
		public static readonly string checkDNS_prompt = "Check DNS of domains in emails? y / n";
		public static readonly string fixDomains_prompt = "Fix domains and domain zones? y / n";
		public static readonly string fixDotsGmail_prompt = "Remove dots in google mails? y / n";
		public static readonly string fixDotsYandex_prompt = "Replace dots to \"-\" in yandex mails? y / n";
		public static readonly string fixPlus_prompt = "Remove all after \"+\" in mails? y / n";
		public static readonly string removeEmptyPass_prompt = "Remove empty passwords? y / n";
		public static readonly string removeEqualLoginPass_prompt = "Remove same login and pass? y / n";
		public static readonly string removeTempMail_prompt = "Remove temp mails? y / n";
		public static readonly string removeXumer_prompt = "Remove xrumer (spam) mails? y / n";
		public static readonly string removeXXXX_prompt = "Remove \"xxxx\" in google mails? y / n";
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

		public static bool CheckConfig() {
			if (!File.Exists("config.cfg")) {
				SetConfig("Config file not found.");
				Console.Clear();
				return false;
			}
			else {
				try {
					var settings = new Dictionary<string, bool>(); // create a dictionary to store the settings
					foreach (string line in File.ReadLines("config.cfg")) { // loop through each line in the file
						string[] parts = line.Split(": "); // split the line by ": "
						string name = parts[0]; // get the setting name
						bool value = bool.Parse(parts[1]); // get the setting value
						settings[name] = value; // add or update the setting in the dictionary
					}
					checkDNS = settings["checkDNS"];
					removeXXXX = settings["removeXXXX"];
					removeEmptyPass = settings["removeEmptyPass"];
					removeXumer = settings["removeXumer"];
					removeEqualLoginPass = settings["removeEqualLoginPass"];
					removeTempMail = settings["removeTempMail"];
					fixDotsGmail = settings["fixDotsGmail"];
					fixDotsYandex = settings["fixDotsYandex"];
					fixPlus = settings["fixPlus"];
					fixDomains = settings["fixDomains"];
				}
				catch {
					SetConfig("Config file is corrupted.");
					Console.Clear();
					return false;
				}
			}
			return true;
		}

		public static void CheckFiles(string[] files) {
			if (files.Any(file => !File.Exists(file))) { // check if any file does not exist
				foreach (string file in files) // loop through all files
					if (!File.Exists(file)) // print only the ones that do not exist
						Console.WriteLine($"\"{file}\" file not found.");
				Console.ReadLine();
				Environment.Exit(0);
			}
		}

		public static async Task MainWork(string path) {
			Regex loginpassRegex = new(@"^[A-Za-z\d][\w.+-]*@([A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z]?[A-Za-z\d]{1,10}[;:].*?$");
			Regex loginpassPartialRegex = new(@"[A-Za-z\d][\w.+-]*@(?:[A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z]?[A-Za-z\d]{1,10}[;:].*?(?:[;:]|$)");
			Regex mailRegex = new(@"^[A-Za-z\d][\w.+-]*@([A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z]?[A-Za-z\d]{1,10}$");
			ParallelOptions options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
			string fileName = Path.GetFileNameWithoutExtension(path);
			float i = 0;
			long lines = FileUtils.GetLinesCount(path);
			string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH;mm");
			string[] allFiles = new string[5] { $"./Results/{fileName}_shit {dateTime}.tmp",
		$"./Results/{fileName}_good {dateTime}.tmp", $"./Results/{fileName}_shit.txt",
		$"./Results/{fileName}_shit.txt", $"./Results/{fileName}.tmp" };
			if (!Directory.Exists("./Results"))
				Directory.CreateDirectory("./Results");
			foreach (string tmpFile in allFiles)
				if (File.Exists(tmpFile))
					File.Delete(tmpFile);
			Console.Clear();
			ConsoleUtils.WriteColorized("\n[INFO] ", ConsoleColor.Red);
			Console.WriteLine($"Working with file \"{fileName}\". Loaded {lines} lines.");
			ConsoleUtils.WriteColorized("[1] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Fixing domains and writing to temp file.");
			double roundedCount = Math.Round((double)lines / 100);
			Stopwatch stopWatch = new();
			stopWatch.Start();
			StringBuilder tempGood = new(), tempBad = new();
			object lockObj = new();

            var fixDomains = FileUtils.WriteToDictionary("FixDomains");
            var domains = new HashSet<string>();
			foreach (var item in fixDomains)
				domains.Add(item.Value);
            var fixZones = FileUtils.WriteToDictionary("FixZone");
            var zones = new HashSet<string>();
            foreach (var item in fixZones)
                zones.Add(item.Value);

            Parallel.ForEach(File.ReadLines(path), options, (line, state, index) => {
				if (index % roundedCount == 0)
					Console.Title = $"[1] {Math.Round(index / roundedCount, 0)}% | Fixing domains.";
				try {
					line = FileUtils.FixLines(line, fixDomains, domains, fixZones, zones);
					if (loginpassRegex.IsMatch(line) || mailRegex.IsMatch(line))
						lock (lockObj) {
							tempGood.AppendLine(line);
							if (tempGood.Length >= 20480) {
								File.AppendAllText(allFiles[4], tempGood.ToString());
								tempGood.Clear();
							}
						}
					else {
						Match match = loginpassPartialRegex.Match(line);
						if (match.Success)
							lock (lockObj) {
								tempGood.AppendLine(match.Value);
								if (tempGood.Length >= 20480) {
									File.AppendAllText(allFiles[4], tempGood.ToString());
									tempGood.Clear();
								}
							}
						else
							lock (lockObj) {
								tempBad.AppendLine($"#BadSyntax# {line}");
								if (tempBad.Length >= 20480) {
									File.AppendAllText(allFiles[0], tempBad.ToString());
									tempBad.Clear();
								}
							}
					}
				}
				catch { }
			});

            if (tempBad.Length > 0) {
                File.AppendAllText(allFiles[4], tempGood.ToString());
                tempBad.Clear();
            }
            if (tempGood.Length > 0) {
                File.AppendAllText(allFiles[0], tempBad.ToString());
                tempGood.Clear();
            }

			tempGood.Clear();
			tempBad.Clear();

			if (!File.Exists(allFiles[4])) {
				Console.Title = "Idle.";
				Console.WriteLine($"No good mail(:pass(:other)) lines found!");
				return;
			}
			Console.Title = $"[1] {i / (lines / 100)}% | Fixing domains.";
			i = 0;
			ConsoleUtils.WriteColorized("[1] ", ConsoleColor.Green);
			Console.WriteLine("Done!");
			HashSet<string> badDNS = new();
			if (checkDNS) {
				ConsoleUtils.WriteColorized("[2.1] ", ConsoleColor.DarkYellow);
				Console.WriteLine("Checking DNS addresses.");
				lines = FileUtils.GetLinesCount(allFiles[4]);
				badDNS = await LineUtils.DNSCheck(allFiles[4], lines);
			}
			ConsoleUtils.WriteColorized("[3] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Cleaning the base.");
			Console.Title = "0% | Cleaning the base.";
			roundedCount = Math.Round((double)lines / 100);
			int goodCount = 0, shitCount = 0;

			Parallel.ForEach(File.ReadLines(allFiles[4]), options, (line, state, index) => {
				if (index % roundedCount == 0)
					Console.Title = $"[3] {index / roundedCount}% | Cleaning the base.";
				try {
					string result = LineUtils.ProcessLine(line, badDNS);
					if (result.StartsWith('#'))
						lock (lockObj) {
							tempBad.AppendLine($"{result} {line}");
							shitCount++;
							if (tempBad.Length >= 20480) {
								File.AppendAllLines(allFiles[0], tempBad.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
								tempBad.Clear();
							}
						}
					else
						lock (lockObj) {
							tempGood.AppendLine($"{result}");
							goodCount++;
							if (tempGood.Length >= 20480) {
								File.AppendAllLines(allFiles[1], tempGood.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
								tempGood.Clear();
							}
						}
				}
				catch { }
			});

			if (tempBad.Length > 0) {
                File.AppendAllText(allFiles[0], tempBad.ToString());
				tempBad.Clear();
			}
			if (tempGood.Length > 0) {
                File.AppendAllText(allFiles[1], tempGood.ToString());
                tempGood.Clear();
			}
			
			Console.Title = $"[3] 100% | Cleaning the base. Good: {goodCount}, Bad: {shitCount}";
			
			ConsoleUtils.WriteColorized("[3] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is {shitCount} bad lines and {goodCount} good lines.");
			ConsoleUtils.WriteColorized("[4] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Making result files.");
			Console.Title = "[4] Making result files.";
			File.Delete(allFiles[4]);
			Task tmpGoodToTxt = FileUtils.TempToTxtAsync(allFiles[1]);
			Task tmpBadToTxt = FileUtils.TempToTxtAsync(allFiles[0]);
			Task.WaitAll(tmpBadToTxt, tmpGoodToTxt);
			stopWatch.Stop();
            ConsoleUtils.WriteColorized("[4] ", ConsoleColor.Green);
            TimeSpan time = TimeSpan.FromSeconds(stopWatch.Elapsed.TotalSeconds);
			Console.WriteLine($"Done! Elapsed {time:h\\h\\ m\\m\\ s\\s}.\r\n");
			Console.Title = "Idle.";
		}

		public static void PromptConfig() {
			Console.Write("Do you want to change config?");
			ConsoleUtils.WriteColorizedYN();
			bool answer = SetPartConfigBool(""); // call the SetPartConfigBool method with an empty prompt
			if (answer) { // check if the answer is true
				SetConfig("Changing config file."); // call the SetConfig method
				Console.Clear();
			}
		}

		public static void SetConfig(string msg) {
			Console.WriteLine(msg);
			Console.WriteLine("Note: If all answers is \"n\", program will fix only syntax errors.");
			var settings = new Dictionary<string, bool>(); // create a dictionary to store the settings
			var prompts = new Dictionary<string, string> {
				// add the setting names and prompts to the dictionaries
				["checkDNS"] = checkDNS_prompt,
				["removeXXXX"] = removeXXXX_prompt,
				["removeEmptyPass"] = removeEmptyPass_prompt,
				["removeXumer"] = removeXumer_prompt,
				["removeEqualLoginPass"] = removeEqualLoginPass_prompt,
				["removeTempMail"] = removeTempMail_prompt,
				["fixDotsGmail"] = fixDotsGmail_prompt,
				["fixDotsYandex"] = fixDotsYandex_prompt,
				["fixPlus"] = fixPlus_prompt,
				["fixDomains"] = fixDomains_prompt
			}; // create a dictionary to store the prompts
			foreach (string name in prompts.Keys) { // loop through all setting names
				string prompt = prompts[name]; // get the prompt for each setting
				bool value = SetPartConfigBool(prompt); // get the value for each setting
				settings[name] = value; // add or update the setting in the dictionary
			}
			string write = ""; // create an empty string to write to the file
			foreach (string name in settings.Keys) { // loop through all setting names
				bool value = settings[name]; // get the value for each setting
				write += $"{name}: {value}\n"; // append each setting name and value to the string with a newline character
			}
			File.WriteAllText("config.cfg", write); // write the string to the file
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
	}
}