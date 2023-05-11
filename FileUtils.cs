using consoleutils;
using DnsClient;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using config;
using lineutils;

namespace fileutils {

	public class FileUtils {
		public static readonly HashSet<string> tempDomains = WriteToHashSet(Config.files[4]);

		public static double GetLinesCount(string path) {
			double i = 0;
			foreach (var line in File.ReadLines(path))
				i += 1;
			return i;
		}

		public static async Task TempToTxtAsync(string file) {
			if (!File.Exists(file) || !file.EndsWith(".tmp")) return;
			SortedSet<string> lines = new();
			using (StreamReader reader = new(file)) {
				string line;
				while ((line = await reader.ReadLineAsync()) != null)
					lines.Add(line);
			}
			await File.WriteAllLinesAsync(file.Replace(".tmp", ".txt"), lines);
			File.Delete(file);
		}

		public static HashSet<string> WriteToHashSet(string fileName) =>
			new(File.ReadLines(fileName));

		public static void WriteHashsetToFile(string filename, HashSet<string> lines, bool append) {
			using StreamWriter sw = new(filename, append);
			foreach (string line in lines)
				sw.WriteLine(line);
		}
		
		/*public static void WriteListToFile(string filename, List<string> lines, bool append) {
			using StreamWriter sw = new(filename, append);
			foreach (string line in lines)
				sw.WriteLine(line);
		}*/

		public static Dictionary<string, string> WriteToDictionary(string fileName) {
			Dictionary<string, string> result = new();
			foreach (string line in File.ReadLines(fileName)) {
				string[] parts = line.Split('=');
				if (parts.Length == 2)
					result[parts[0]] = parts[1];
			}
			return result;
		}

		public static async Task<HashSet<string>> DNSCheck(string path, double linesCount, int step) {
			HashSet<string> goodDNS = WriteToHashSet(Config.files[3]);
			HashSet<string> newTemps = new();
			ConcurrentBag<string> badDNS = new(WriteToHashSet(Config.files[0]));
			ConcurrentBag<string> domains = new();
			int sstep = 1;
			ConsoleUtils.WriteColorized($"[{step}.{sstep}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Filtering DNS addresses.");
			Console.Title = $"[{step}.{sstep}] 0% | Filtering DNS addresses.";
			double roundedCount = Math.Ceiling(linesCount / 100), i = 0;
			Parallel.ForEach(File.ReadLines(path), Config.options, line => {
				if (i++ % roundedCount == 0)
					Console.Title = $"[{step}] {Math.Round(i / roundedCount, 0)}% | Filtering DNS addresses.";
				Match match = Config.domainCheck.Match(line);
				if (!match.Success && !Config.domainRegex.IsMatch(line)) return;
				string domain = match.Groups[1].Value.ToLower();
				if (Config.tempRegex.IsMatch(domain)) {
					tempDomains.Add(domain);
					newTemps.Add(domain);
					return;
				}
				if (!domains.Contains(domain) && !goodDNS.Contains(domain) && !badDNS.Contains(domain) && !tempDomains.Contains(domain))
					domains.Add(domain);
			});
				goodDNS = null;
			if (newTemps.Count > 0)
				WriteHashsetToFile(Config.files[4], newTemps, true);
			newTemps = null;
			i = 0;
			ConsoleUtils.WriteColorized($"[{step}.{sstep}] ", ConsoleColor.Green);
			if (domains.IsEmpty) {
				Console.WriteLine("No new DNS addresses found.");
				return badDNS.ToHashSet();
			}
			List<List<string>> chunks = domains.ToList()
				.Select((domain, index) => new { domain, index })
				.GroupBy(x => x.index / 40)
				.Select(g => g.Select(x => x.domain).ToList())
				.ToList();
			domains = null;
			linesCount = chunks.Sum(chunk => chunk.Count);
			roundedCount = Math.Ceiling(linesCount / 100);
			Console.Write($"Done! There is {linesCount} domains to check.\n");
			sstep++;
			ConsoleUtils.WriteColorized($"[{step}.{sstep}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Checking DNS's MX of domains. Please wait...");
			Console.Title = $"[{step}.{sstep}] 0% | Checking DNS's MX of domains.";
			StringBuilder badDNSBuilder = new(capacity: 10000000);
			StringBuilder goodDNSBuilder = new(capacity: 10000000);
			LookupClient client = new(new LookupClientOptions {
				UseCache = true,
				UseTcpOnly = true,
				Timeout = TimeSpan.FromSeconds(2)
			});
			int badDNSCount = 0, goodDNSCount = 0, semaphoreCount = 100;
			if (chunks.Count < 100)
				semaphoreCount = chunks.Count;

			SemaphoreSlim semaphore = new(semaphoreCount);
			List<Task> tasks = new();
			foreach (var chunk in chunks)
				tasks.Add(Task.Run(async () => {
					foreach (string domain in chunk) {
						await semaphore.WaitAsync();
						try {
							for (int y = 0; y < 2; ++y)
								try {
									var result = await client.QueryAsync(domain, QueryType.MX);
									if (result.Answers.MxRecords().Any()) {
										goodDNSBuilder.AppendLine(domain);
										Interlocked.Increment(ref goodDNSCount);
									}
									else {
										badDNSBuilder.AppendLine(domain);
										badDNS.Add(domain);
										Interlocked.Increment(ref badDNSCount);
									}
									break;
								}
								catch (Exception) {
									if (y == 1) {
										badDNSBuilder.AppendLine(domain);
										badDNS.Add(domain);
										Interlocked.Increment(ref badDNSCount);
									}
								}
							i = goodDNSCount + badDNSCount;
							if (i % roundedCount == 0)
								Console.Title = $"[{step}.{sstep}] {Math.Round(i / roundedCount, 0)}% | Checking DNS's MX of domains.";
						}
						finally {
							semaphore.Release();
						}
					}
				}));
			await Task.WhenAll(tasks);
			File.AppendAllText(Config.files[0], badDNSBuilder.ToString());
			File.AppendAllText(Config.files[3], goodDNSBuilder.ToString());

			badDNSBuilder = null;
			goodDNSBuilder = null;
			ConsoleUtils.WriteColorized($"[{step}.{sstep}] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is new {badDNSCount} bad DNS's and {goodDNSCount} good DNS's.");
			return badDNS.ToHashSet();
		}

		public static void GenerDelete(string path, ref long badCount, ref HashSet<string> good, ref HashSet<string> bad) {
			HashSet<string> mail = new();
			HashSet<string> dmail = new();
			HashSet<string> lines = new();
			using (StreamReader f = new(path)) {
				string i;
				while ((i = f.ReadLine()) != null) {
					char splitter = LineUtils.GetSplitter(i);
					if (splitter == '@') continue;
					string[] logpass = i.Split(splitter);
					string log = logpass[0];
					if (mail.Contains(log))
						dmail.Add(log);
					mail.Add(log);
					lines.Add(i);
				}
			}
			foreach (string i in lines) {
				char splitter = LineUtils.GetSplitter(i);
				if (splitter == '@') continue;
				string[] logpass = i.Split(splitter);
				string log = logpass[0];
				if (!dmail.Contains(log))
					good.Add(i);
				else {
					bad.Add($"#DupMail# {i}");
					badCount++;
				}
			}
		}

		/*public static void GenerDelete(string path, ref long badCount, ref HashSet<string> lines, ref HashSet<string> bad) {
			lines = WriteToHashSet(path);
			HashSet<string> passwords = new();
			HashSet<string> badpasswords = new();
			HashSet<string> mails = new();
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				if (splitter == '@') continue;
				string[] lp = line.Split(splitter);
				if (Config.hashRegex.IsMatch(lp[1])) continue;
				if (passwords.Contains(lp[1]) && mails.Contains(lp[0])) continue;
				mails.Add(lp[0].Split('@')[0]);
				passwords.Add(lp[1]);
			}
			/*try {
				for (int i = 0; i < passwords.Count; ++i) {
					string pass1 = passwords.ElementAt(i);
					string mail1 = mails.ElementAt(i);
					for (int j = 0; j < passwords.Count; ++j) {
						string pass2 = passwords.ElementAt(j);
						string mail2 = mails.ElementAt(j);
						if (pass2 == "111111123") {
							string asd = "";
						}
						if (badpasswords.Contains(passwords.ElementAt(j))) continue;
						if (pass1 == pass2 && mail1 != mail2) continue;
						if (LineUtils.StringCompare(pass1, pass2) <= 3) {
							badpasswords.Add(pass2);
							badCount++;
							break;
						}
					}
				}
			}
			catch {
				string asd = "";
			}*/

			/*foreach (string pass1 in passwords) {
				foreach (string pass2 in passwords) {
					if (badpasswords.Contains(pass2)) continue;
					if (pass1 == pass2) continue;
					int sum = LineUtils.StringCompare(pass1, pass2);
					if (sum <= 3) {
						badpasswords.Add(pass2);
						badCount++;
						break;
					}
				}
			}

			bad = GetBadPasswordLines(lines, badpasswords);
			DeleteBadPasswords(lines, badpasswords);
		}*/

		/*public static void DeleteBadPasswords(HashSet<string> lines, HashSet<string> badpasswords) =>
			lines.RemoveWhere(line => {
				char splitter = LineUtils.GetSplitter(line);
				if (splitter == '@') return false;
				return badpasswords.Contains(line[(line.IndexOf(splitter) + 1)..]);
			});

		public static HashSet<string> GetBadPasswordLines(HashSet<string> lines, HashSet<string> badpasswords) {
			ConcurrentBag<string> result = new();
			Parallel.ForEach(lines, line => {
				char splitter = LineUtils.GetSplitter(line);
				if (splitter == '@') return;
				string pass = line.Split(splitter)[1];
				if (badpasswords.Contains(pass))
					result.Add($"#Gener# {line}");
			});
			return new HashSet<string>(result);
		}
		*/
		public static void WriteSBToFile(StringBuilder sb, int length, string file) {
			if (sb.Length < length) return;
			File.AppendAllText(file, sb.ToString());
			sb.Clear();
		}
	}
}