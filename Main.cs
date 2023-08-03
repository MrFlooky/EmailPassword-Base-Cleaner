using consoleutils;
using fileutils;
using lineutils;
using config;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;

namespace main {
	internal class Main {

		public static int step = 0;
		public static double lines = 0;
		public static long goodCount = 0;
		public static long shitCount = 0;
		public static double roundedCount = 0;

		public static bool workWithContext = false;
		public static bool workWithArgs = false;

		public static string[] Initialize(string output) {
			workWithArgs = !string.IsNullOrEmpty(output);
			string[] allFiles = new string[3] { $"./{output}_shit.tmp",
				$"./{output}.tmp", $"./{output}_tmp.tmp" };
			if (workWithContext)
				allFiles = new string[3] { $"{output}_shit.tmp",
					$"{output}.tmp", $"{output}_tmp.tmp" };
			string fileName = Path.GetFileNameWithoutExtension(Config.path);
			if (!workWithArgs) {
				string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH;mm");
				allFiles = new string[3] { $"./Results/{fileName}_shit {dateTime}.tmp",
					$"./Results/{fileName}_good {dateTime}.tmp", $"./Results/{fileName}.tmp" };
				Console.Clear();
				if (!Directory.Exists("./Results"))
					Directory.CreateDirectory("./Results");
			}
			lines = FileUtils.GetLinesCount(Config.path);
			ConsoleUtils.WriteColorized($"\n[{step}] ", ConsoleColor.Red);
			Console.WriteLine($"Working with file \"{fileName}\". Loaded {lines} lines.");
			foreach (string tmpFile in allFiles)
				if (File.Exists(tmpFile))
					File.Delete(tmpFile);
			step++;
			return allFiles;
		}

		public static void DomainFix(string[] allFiles) {
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Fixing domains and writing to temp file.");
			roundedCount = Math.Round(lines / 100);
			foreach (var item in Config.fixDomainsDictionary)
				Config.domains.Add(item.Value);
			foreach (var item in Config.fixZonesDictionary)
				Config.zones.Add(item.Value);
			StringBuilder tempGood = new();
			StringBuilder tempBad = new();
			object lockObj = new();
			Parallel.ForEach(File.ReadLines(Config.path), Config.options, (line, state, index) => {
				if (index % roundedCount == 0)
					Console.Title = $"[{step}] {Math.Round(index / roundedCount, 0)}% | Fixing domains.";
				try {
					line = LineUtils.FixLine(ref line);
					if (line.StartsWith('#'))
						lock (lockObj) {
							shitCount++; // С ЭТИМ ОШИБКА, ПЕРЕДЕЛАТЬ
							tempBad.AppendLine(line);
							FileUtils.WriteSBToFile(tempBad, 20480, allFiles[0]);
						}
					if (Config.loginpassRegex.IsMatch(line) || Config.mailRegex.IsMatch(line))
						lock (lockObj) {
							tempGood.AppendLine(line);
							FileUtils.WriteSBToFile(tempGood, 20480, allFiles[2]);
						}
					else {
						Match match = Config.loginpassPartialRegex.Match(line);
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
		}

		public static void StartCleanGener(string[] allFiles) {
			step++;
			Console.Title = $"[{step}] Cleaning generated lines.";
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Cleaning generated lines.");
			FileUtils.GenerDelete(allFiles[2], ref shitCount, out HashSet<string> goodGener, out HashSet<string> badGener);
			FileUtils.WriteHashsetToFile(allFiles[2], goodGener, false);
			FileUtils.WriteHashsetToFile(allFiles[0], badGener, true);
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.Green);
			Console.WriteLine("Done!");
		}

		public static void MainCleaner(string[] allFiles, HashSet<string> badDNS) {
			step++;
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Cleaning the base.");
			Console.Title = "0% | Cleaning the base.";
			roundedCount = Math.Round(lines / 100);
			StringBuilder tempGood = new(), tempBad = new();
			object lockObj = new();
			Parallel.ForEach(File.ReadLines(allFiles[2]), Config.options, (line, state, index) => {
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
		}

		public static void MakeResultFiles(string[] allFiles) {
			step++;
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Making result files.");
			Console.Title = $"[{step}] Making result files.";
			File.Delete(allFiles[2]);
			Task tmpBadToTxt = FileUtils.TempToTxtAsync(allFiles[0]);
			Task tmpGoodToTxt = FileUtils.TempToTxtAsync(allFiles[1]);
			Task.WaitAll(tmpBadToTxt, tmpGoodToTxt);
		}

		public static async Task MainWork(string output) {
			output = output.Split('.')[0];
			string[] allFiles = Initialize(output);
			Stopwatch stopWatch = new();
			stopWatch.Start();
			goodCount = 0;
			shitCount = 0;

			DomainFix(allFiles);
			if (!File.Exists(allFiles[2])) {
				Console.Title = "Idle.";
				Console.WriteLine($"No good mail(:pass(:other)) lines found!");
				return;
			}

			HashSet<string> badDNS = new();
			lines = FileUtils.GetLinesCount(allFiles[2]);
			if (Config.checkDNS)
				badDNS = await FileUtils.DNSCheck(allFiles[2]);
			if (Config.cleanSameMails)
				StartCleanGener(allFiles);

			MainCleaner(allFiles, badDNS);
			MakeResultFiles(allFiles);
			stopWatch.Stop();
			ConsoleUtils.WriteColorized($"[{step}] ", ConsoleColor.Green);
			TimeSpan time = TimeSpan.FromSeconds(stopWatch.Elapsed.TotalSeconds);
			Console.WriteLine($"Done! Elapsed {time:h\\h\\ m\\m\\ s\\s}.");
			Console.Title = "Idle.";
		}
	}
}
