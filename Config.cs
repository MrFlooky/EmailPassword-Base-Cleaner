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
			else
				try {
					string[] lines = File.ReadAllLines("config.cfg");
					checkDNS = bool.Parse(lines[0].Split(": ")[1]);
					removeXXXX = bool.Parse(lines[1].Split(": ")[1]);
					removeEmptyPass = bool.Parse(lines[2].Split(": ")[1]);
					removeXumer = bool.Parse(lines[3].Split(": ")[1]);
					removeEqualLoginPass = bool.Parse(lines[4].Split(": ")[1]);
					removeTempMail = bool.Parse(lines[5].Split(": ")[1]);
					fixDotsGmail = bool.Parse(lines[6].Split(": ")[1]);
					fixDotsYandex = bool.Parse(lines[7].Split(": ")[1]);
					fixPlus = bool.Parse(lines[8].Split(": ")[1]);
					fixDomains = bool.Parse(lines[9].Split(": ")[1]);
				}
				catch {
					SetConfig("Config file is corrupted.");
					Console.Clear();
					return false;
				}
			return true;
		}

		public static void CheckFiles(string[] files) {
			bool check = false;
			foreach (string file in files) {
				if (File.Exists(file)) continue;
				Console.WriteLine($"\"{file}\" file not found.");
				if (!check) check = true;
			}
			if (check) {
				Console.ReadLine();
				Environment.Exit(0);
			}
		}

		public static async Task MainWork(string path) {
			Regex loginpassRegex = new(@"^[A-Za-z\d][\w.+-]*@([A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z]?[A-Za-z\d]{1,10}[;:].*?$");
			Regex loginpassPartialRegex = new(@"[A-Za-z\d][\w.+-]*@(?:[A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z]?[A-Za-z\d]{1,10}[;:].*?(?:[;:]|$)");
			Regex mailRegex = new(@"^[A-Za-z\d][\w.+-]*@([A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z]?[A-Za-z\d]{1,10}$");
			ParallelOptions options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
			string? line, fileName = Path.GetFileNameWithoutExtension(path);
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
			Console.WriteLine($"Working with file \"{fileName}\"");
			ConsoleUtils.WriteColorized("[1] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Fixing domains and writing to temp file.");
			double roundedCount = Math.Round((double)lines / 100);
			Stopwatch stopWatch = new();
			stopWatch.Start();
			StringBuilder tempGood = new(), tempBad = new();
			object lockObj = new();
			Parallel.ForEach(File.ReadLines(path), options, (line, state, index) => {
				if (index % roundedCount == 0)
					Console.Title = $"[1] {Math.Round(index / roundedCount, 0)}% | Fixing domains.";
				try {
					line = FileUtils.FixLines(line);
					if (loginpassRegex.IsMatch(line) || mailRegex.IsMatch(line))
						lock (lockObj)
							tempGood.AppendLine(line);
					else {
						Match match = loginpassPartialRegex.Match(line);
						if (match.Success)
							lock (lockObj)
								tempGood.AppendLine(match.Value);
						else
							lock (lockObj)
								tempBad.AppendLine($"#BadSyntax# {line}");
					}
					if (tempGood.Length >= 20480) {
						File.AppendAllText(allFiles[4], tempGood.ToString());
						tempGood.Clear();
					}
					if (tempBad.Length >= 20480) {
						File.AppendAllText(allFiles[0], tempBad.ToString());
						tempBad.Clear();
					}
				}
				catch { }
			});
			if (tempGood.Length > 0)
				File.AppendAllText(allFiles[4], tempGood.ToString());
			if (tempBad.Length > 0)
				File.AppendAllText(allFiles[0], tempBad.ToString());
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
					if (result.StartsWith('#')) {
						lock (lockObj) {
							tempBad.AppendLine($"{result} {line}");
							shitCount++;
							if (tempBad.Length >= 20480) {
								File.AppendAllLines(allFiles[0], tempBad.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
								tempBad.Clear();
							}
						}
					}
					else {
						lock (lockObj) {
							tempGood.AppendLine($"{result}");
							goodCount++;
							if (tempGood.Length >= 20480) {
								File.AppendAllLines(allFiles[1], tempGood.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
								tempGood.Clear();
							}
						}
					}
				}
				catch { }
			});

			if (tempBad.Length > 0) 
				File.AppendAllLines(allFiles[0], tempBad.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
			if (tempGood.Length > 0)
				File.AppendAllLines(allFiles[1], tempGood.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
			Console.Title = $"[3] 100% | Cleaning the base. Good: {goodCount}, Bad: {shitCount}";
			if (tempBad.Length > 0) {
				File.AppendAllLines(allFiles[0], tempBad.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
				tempBad.Clear();
			}
			if (tempGood.Length > 0) {
				File.AppendAllLines(allFiles[1], tempGood.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
				tempGood.Clear();
			}
			ConsoleUtils.WriteColorized("[3] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is {shitCount} bad lines and {goodCount} good lines.");
			ConsoleUtils.WriteColorized("[4] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Making result files.");
			Console.Title = "[4] Making result files.";
			File.Delete(allFiles[4]);
			Task tmpGoodToTxt = FileUtils.TempToTxtAsync(allFiles[1]);
			Task tmpBadToTxt = FileUtils.TempToTxtAsync(allFiles[0]);
			await Task.WhenAll(tmpGoodToTxt, tmpBadToTxt);
			stopWatch.Stop();
			double seconds = stopWatch.Elapsed.TotalSeconds;
			double minutes = 0, hours = 0;
			while (true) {
				if (seconds >= 60) {
					minutes++;
					seconds -= 60;
				}
				else if (minutes >= 60) {
					hours++;
					minutes -= 60;
				}
				else break;
			}
			line = "Done! Elapsed";
			if (hours > 0) line += $" {hours}h";
			if (minutes > 0) line += $" {minutes}m";
			if (seconds > 0) line += $" {Math.Round(seconds, 0)}s";
			ConsoleUtils.WriteColorized("[4] ", ConsoleColor.Green);
			Console.WriteLine($"{line}.\r\n");
			Console.Title = "Idle.";
		}

		public static void PromptConfig() {
			Console.Write("Do you want to change config?");
			ConsoleUtils.WriteColorizedYN();
			while (true) {
				string? tmpLine = Console.ReadLine()?.Trim().ToLower();
				if (tmpLine == "y" || tmpLine == "n") {
					if (tmpLine == "y") {
						SetConfig("Changing config file.");
						Console.Clear();
					}
					break;
				}
				Console.WriteLine("Try again.");
			}
		}

		public static void SetConfig(string msg) {
			Console.WriteLine(msg);
			Console.WriteLine("Note: If all answers is \"n\", program will fix only syntax errors.");
			checkDNS = SetPartConfigBool(checkDNS_prompt);
			removeXXXX = SetPartConfigBool(removeXXXX_prompt);
			removeEmptyPass = SetPartConfigBool(removeEmptyPass_prompt);
			removeXumer = SetPartConfigBool(removeXumer_prompt);
			removeEqualLoginPass = SetPartConfigBool(removeEqualLoginPass_prompt);
			removeTempMail = SetPartConfigBool(removeTempMail_prompt);
			fixDotsGmail = SetPartConfigBool(fixDotsGmail_prompt);
			fixDotsYandex = SetPartConfigBool(fixDotsYandex_prompt);
			fixPlus = SetPartConfigBool(fixPlus_prompt);
			fixDomains = SetPartConfigBool(fixDomains_prompt);

			string write = $"Check DNS: {checkDNS}\n" +
				$"RemoveXXXX: {removeXXXX}\n" +
				$"RemoveEmptyPass: {removeEmptyPass}\nRemoveXrumer: {removeXumer}\n" +
				$"RemoveEqualLoginPass: {removeEqualLoginPass}\nRemoveTempMail: {removeTempMail}\n" +
				$"FixDotsGmail: {fixDotsGmail}\nFixDotsYandex: {fixDotsYandex}\n" +
				$"FixPlus: {fixPlus}\nFixDomains: {fixDomains}";
			File.WriteAllText("config.cfg", write);
		}
		public static bool SetPartConfigBool(string msg) {
			while (true) {
				Console.WriteLine(msg);
				string input = Console.ReadLine().Trim().ToLower();
				if (input == "y" || input == "n")
					return input == "y";
				Console.WriteLine("Invalid input, try again.");
			}
		}
	}
}