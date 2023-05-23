using consoleutils;
using fileutils;
using lineutils;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace config {

	internal class Config {
		public static string path = "";
		public static string currentDir = AppDomain.CurrentDomain.BaseDirectory;
		public static string exePath = Process.GetCurrentProcess().MainModule.FileName;
		public static string[] files = new string[6] { $"{currentDir}BadDNS",
			$"{currentDir}FixDomains", $"{currentDir}FixZone", $"{currentDir}GoodDNS",
			$"{currentDir}TempMails", $"{currentDir}config.cfg" };

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
		public static readonly string cleanSameMails_prompt = "Remove lines with generated lines? y / n";
		public static readonly string query_prompt = "Request a change of settings in the future? (! THIS IS PERMANENT OPTION !) y / n";
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

		public static bool workWithArgs = false;
		public static bool workWithcontext = false;

		private static readonly string domainPartRegex = @"(?:[A-Za-z\d][A-Za-z\d-]*\.)+([A-Za-z]?[A-Za-z\d]{1,10}|xn--[a-z\d]{4,18})";
		private static readonly string loginPartRegex = @"[A-Za-z\d](?!.*[-._]{2,})(?!(?:[^+]*\+){2})[\w.+-]*";
		private static readonly string mailPartRegex = loginPartRegex + '@' + domainPartRegex;
		public static Regex hashRegex = new(@"^(?=(?:.*[a-f]){3})[\da-f$.]{16,}$");
		public static Regex loginRegex = new('^' + loginPartRegex + '$');
		public static Regex htmlencodeRegex = new(@"&[A-Za-z\d]{2,6};");
		public static Regex loginpassRegex = new('^' + mailPartRegex + "[;:].*?$");
		public static Regex loginpassPartialRegex = new(mailPartRegex + "[;:].*?(?:[;:]|$)");
		public static Regex mailRegex = new('^' + mailPartRegex + '$');
		public static Regex domainCheck = new(@"@([\w-]+(?:\.[\w-]+)+)(?::|$)");
		public static Regex domainRegex = new('^' + domainPartRegex + '$');
		public static Regex tempRegex = new(@"^(?:(?:[A-Za-z\d][A-Za-z\d-]*\.){5,}[A-Za-z]?[A-Za-z\d]{1,10}|(?:[\da-zA-Z]+\.)+[a-zA-Z]{3}(?:\.co)?\.kr|[a-z]\.[A-Za-z\d]+\.ro|gmail\.com(?:\.?[a-zA-Z\d]+)+|worldcup2019-\d+\.xyz|.*?(?:spam.*?|(?:\.com?){2,}|(?:temp|trash).*(?:e?mail|(?:in)?box).*?|\.(?:(?:creo|oazis)\.site|(?:ddnsfree|epizy|emlpro|emlhub|emltmp|anonaddy|ezyro|email-temp|33mail|t(?:mp)?eml|ourhobby|urbanban|mailinator|thumoi|unaux|chickenkiller|ignorelist|anhaysuka|ibaloch|twilightparadox|boxmaill|xxi2|somee|luk2|mintemail|mooo|batikbantul|dnsabr|kozow|3utilities|servegame|giize|theworkpc|gettrials|x24hr)\.com|(?:dropmail|anonaddy|bgsaddrmwn|myddns|nctu|bccto)\.me|(?:freeml|anonbox|dns-cloud|ll47|sytes|teml|dynu|bounceme)\.net|(?:heliohost|craigslist|zapto|eu)\.org|(?:ml|tk|cf|ga|gq)|(?:usa|nut|flu|cu)\.cc|(?:vuforia|hmail)\.us|(?:web|my)\.id|(?:yomail|toh)\.info|10mail\.(?:org|tk)|567map\.xyz|cloudns\.(?:cc|ph|nz|cx|asia|cl)|ddns\.(?:net|info|me\.uk)|esy\.es|igg\.biz|lofteone\.ru|mailr\.eu|pp\.ua|spymail\.one)))$", RegexOptions.IgnorePatternWhitespace);
		public static Dictionary<string, string> fixDomainsDictionary = new();
		public static Dictionary<string, string> fixZonesDictionary = new();
		public static HashSet<string> domains = new();
		public static HashSet<string> zones = new();
		public static ParallelOptions options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

		public static bool CheckConfig() {
			if (!File.Exists(files[5])) {
				SetConfig("Config file not found.");
				if (!workWithArgs)
					Console.Clear();
				return false;
			}
			else {
				try {
					var settings = new Dictionary<string, bool>(); // create a dictionary to store the settings
					foreach (string line in File.ReadLines(files[5])) { // loop through each line in the file
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
					cleanSameMails = settings["cleanSameMails"];
					query = settings["query"];
				}
				catch {
					SetConfig("Config file is corrupted.");
					if (!workWithArgs)
						Console.Clear();
					return false;
				}
			}
			return true;
		}

		public static void CheckFiles(string[] files) {
			if (files.Any(file => !File.Exists(file))) {
				foreach (string file in files)
					if (!File.Exists(file))
						Console.WriteLine($"\"{Path.GetFileName(file)}\" file not found.");
				if (!workWithArgs)
					Console.ReadLine();
				Environment.Exit(0);
			}
			fixDomainsDictionary = FileUtils.WriteToDictionary(files[1]);
			fixZonesDictionary = FileUtils.WriteToDictionary(files[2]);
		}

		public static async Task MainWork(string output) {
			double lines = FileUtils.GetLinesCount(path);
			workWithArgs = !string.IsNullOrEmpty(output);
			string fileName = Path.GetFileNameWithoutExtension(path);
			output = output.Split('.')[0];
			string[] allFiles = new string[3] { $"./{output}_shit.tmp", 
				$"./{output}.tmp", $"./{output}_tmp.tmp" };
			if (workWithcontext)
				allFiles = new string[3] { $"{output}_shit.tmp",
					$"{output}.tmp", $"{output}_tmp.tmp" };
			if (!workWithArgs) {
				string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH;mm");
				allFiles = new string[3] { $"./Results/{fileName}_shit {dateTime}.tmp",
					$"./Results/{fileName}_good {dateTime}.tmp", $"./Results/{fileName}.tmp" };
				Console.Clear();
				if (!Directory.Exists("./Results"))
					Directory.CreateDirectory("./Results");
			}
			int step = 0;
			foreach (string tmpFile in allFiles)
				if (File.Exists(tmpFile))
					File.Delete(tmpFile);
			ConsoleUtils.WriteColorized($"\n[{step}] ", ConsoleColor.Red);
			Console.WriteLine($"Working with file \"{fileName}\". Loaded {lines} lines.");
			step++;
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Fixing domains and writing to temp file.");
			double roundedCount = Math.Round(lines / 100);
			Stopwatch stopWatch = new();
			stopWatch.Start();
			StringBuilder tempGood = new(), tempBad = new();
			long goodCount = 0, shitCount = 0;
			object lockObj = new();

			foreach (var item in fixDomainsDictionary)
				domains.Add(item.Value);
			foreach (var item in fixZonesDictionary)
				zones.Add(item.Value);

			Parallel.ForEach(File.ReadLines(path), options, (line, state, index) => {
				if (index % roundedCount == 0)
					Console.Title = $"[{step}] {Math.Round(index / roundedCount, 0)}% | Fixing domains.";
				try {
					line = LineUtils.FixLine(ref line);
					if (line.StartsWith('#'))
						lock (lockObj) {
							shitCount++;
							tempBad.AppendLine(line);
							FileUtils.WriteSBToFile(tempBad, 20480, allFiles[0]);
						}
					if (loginpassRegex.IsMatch(line) || mailRegex.IsMatch(line))
						lock (lockObj) {
							tempGood.AppendLine(line);
							FileUtils.WriteSBToFile(tempGood, 20480, allFiles[2]);
						}
					else {
						Match match = loginpassPartialRegex.Match(line);
						if (match.Success)
							lock (lockObj) {
								tempGood.AppendLine(match.Value);
								FileUtils.WriteSBToFile(tempGood, 20480, allFiles[2]);
							}
					}
				}
				catch { }
			});
			Console.Title = $"[{step}] 100% | Fixing domains.";
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is {shitCount} lines with bad syntax.");

			FileUtils.WriteSBToFile(tempBad, 0, allFiles[0]);
			FileUtils.WriteSBToFile(tempGood, 0, allFiles[2]);

			if (!File.Exists(allFiles[2])) {
				Console.Title = "Idle.";
				Console.WriteLine($"No good mail(:pass(:other)) lines found!");
				return;
			}
			HashSet<string> badDNS = new();
			lines = FileUtils.GetLinesCount(allFiles[2]);
			if (checkDNS) {
				step++;
				badDNS = await FileUtils.DNSCheck(allFiles[2], lines, step);
			}
			if (cleanSameMails) {
				step++;
				Console.Title = $"[{step}] Cleaning generated lines.";
				ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
				Console.WriteLine("Cleaning generated lines.");
				HashSet<string> goodGener = new();
				HashSet<string> badGener = new();
				FileUtils.GenerDelete(allFiles[2], ref shitCount, ref goodGener, ref badGener);
				FileUtils.WriteHashsetToFile(allFiles[2], goodGener, false);
				FileUtils.WriteHashsetToFile(allFiles[0], badGener, true);
				ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.Green);
				Console.WriteLine("Done!");
			}
			step++;
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Cleaning the base.");
			Console.Title = "0% | Cleaning the base.";
			roundedCount = Math.Round(lines / 100);
			Parallel.ForEach(File.ReadLines(allFiles[2]), options, (line, state, index) => {
				if (index % roundedCount == 0)
					Console.Title = $"[{step}] {index / roundedCount}% | Cleaning the base.";
				try {
					string result = LineUtils.ProcessLine(line, badDNS);
					if (result.StartsWith('#'))
						lock (lockObj) {
							tempBad.AppendLine($"{result} {line}");
							shitCount++;
							FileUtils.WriteSBToFile(tempBad, 20480, allFiles[0]);
						}
					else
						lock (lockObj) {
							tempGood.AppendLine($"{result}");
							goodCount++;
							FileUtils.WriteSBToFile(tempGood, 20480, allFiles[1]);
						}
				}
				catch { }
			});
			if (tempBad.Length > 0)
				FileUtils.WriteSBToFile(tempBad, 0, allFiles[0]);
			if (tempGood.Length > 0)
				FileUtils.WriteSBToFile(tempGood, 0, allFiles[1]);

			Console.Title = $"[{step}] 100% | Cleaning the base. Good: {goodCount}, Bad: {shitCount}";

			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is {shitCount} bad lines and {goodCount} good lines.");
			step++;
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Making result files.");
			Console.Title = $"[{step}] Making result files.";
			File.Delete(allFiles[2]);
			Task tmpGoodToTxt = FileUtils.TempToTxtAsync(allFiles[1]);
			Task tmpBadToTxt = FileUtils.TempToTxtAsync(allFiles[0]);
			Task.WaitAll(tmpBadToTxt, tmpGoodToTxt);
			stopWatch.Stop();
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.Green);
			TimeSpan time = TimeSpan.FromSeconds(stopWatch.Elapsed.TotalSeconds);
			Console.WriteLine($"Done! Elapsed {time:h\\h\\ m\\m\\ s\\s}.");
			Console.Title = "Idle.";
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
			if (workWithArgs) {
				msg += " Reload app without arguments to setup config.";
				Console.WriteLine(msg);
				Environment.Exit(0);
			}
			Console.WriteLine(msg);
			Console.WriteLine("Note: If all answers is \"n\", program will fix only syntax errors.");
			var settings = new Dictionary<string, bool>();
			var prompts = new Dictionary<string, string> {
				["checkDNS"] = checkDNS_prompt,
				["removeXXXX"] = removeXXXX_prompt,
				["removeEmptyPass"] = removeEmptyPass_prompt,
				["removeXumer"] = removeXumer_prompt,
				["removeEqualLoginPass"] = removeEqualLoginPass_prompt,
				["removeTempMail"] = removeTempMail_prompt,
				["fixDotsGmail"] = fixDotsGmail_prompt,
				["fixDotsYandex"] = fixDotsYandex_prompt,
				["fixPlus"] = fixPlus_prompt,
				["fixDomains"] = fixDomains_prompt,
				["cleanSameMails"] = cleanSameMails_prompt,
				["query"] = query_prompt
			};
			bool isAllTrue = SetPartConfigBool("Set all \"y\"?");
			foreach (string name in prompts.Keys) {
				if (isAllTrue) {
					settings[name] = true;
					if (name != "query") continue;
				}
				string prompt = prompts[name];
				bool value = SetPartConfigBool(prompt);
				settings[name] = value;
			}
			string write = "";
			foreach (string name in settings.Keys) {
				bool value = settings[name];
				write += $"{name}: {value}\n";
			}
			File.WriteAllText(files[5], write);
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