using consoleutils;
using DnsClient;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using config;
using main;
using lineutils;

namespace fileutils {

	public class FileUtils {
		public static readonly HashSet<string> tempDomains = WriteToHashSet(Config.files[4]);
		public static readonly Regex tempRegex = new(@"^(?:(?:[A-Za-z\d][A-Za-z\d-]*\.){5,}[A-Za-z]?[A-Za-z\d]{1,10}|(?:[\da-zA-Z]+\.)+[a-zA-Z]{3}(?:\.co)?\.kr|[a-z]\.[A-Za-z\d]+\.ro|gmail\.com(?:\.?[a-zA-Z\d]+)+|worldcup2019-\d+\.xyz|.*?(?:spam.*?|(?:\.com?){2,}|(?:temp|trash).*(?:e?mail|(?:in)?box).*?|\.(?:(?:creo|oazis)\.site|(?:ddnsfree|epizy|emlpro|emlhub|emltmp|anonaddy|ezyro|email-temp|33mail|t(?:mp)?eml|ourhobby|urbanban|mailinator|thumoi|unaux|chickenkiller|ignorelist|anhaysuka|ibaloch|twilightparadox|boxmaill|xxi2|somee|luk2|mintemail|mooo|batikbantul|dnsabr|kozow|3utilities|servegame|giize|theworkpc|gettrials|x24hr)\.com|(?:dropmail|anonaddy|bgsaddrmwn|myddns|nctu|bccto)\.me|(?:freeml|anonbox|dns-cloud|ll47|sytes|teml|dynu|bounceme)\.net|(?:heliohost|craigslist|zapto|eu)\.org|(?:ml|tk|cf|ga|gq)|(?:usa|nut|flu|cu)\.cc|(?:vuforia|hmail)\.us|(?:web|my)\.id|(?:yomail|toh)\.info|10mail\.(?:org|tk)|567map\.xyz|cloudns\.(?:cc|ph|nz|cx|asia|cl)|ddns\.(?:net|info|me\.uk)|esy\.es|igg\.biz|lofteone\.ru|mailr\.eu|pp\.ua|spymail\.one)))$", RegexOptions.IgnorePatternWhitespace);

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

		public static async Task<HashSet<string>> DNSCheck(string path) {
			Main.step++;
			HashSet<string> goodDNS = WriteToHashSet(Config.files[3]);
			ConcurrentBag<string> badDNS = new(WriteToHashSet(Config.files[0]));
			HashSet<string> newTemps = new();
			ConcurrentDictionary<string, byte> domains = new();
			int sstep = 1;
			ConsoleUtils.WriteColorized($"[{Main.step}.{sstep}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Filtering DNS addresses.");
			Console.Title = $"[{Main.step}.{sstep}] 0% | Filtering DNS addresses.";
			double roundedCount = Math.Ceiling(Main.lines / 100), i = 0;
			Parallel.ForEach(File.ReadLines(path), Config.options, line => {
				if (i++ % roundedCount == 0)
					Console.Title = $"[{Main.step}] {Math.Round(i / roundedCount, 0)}% | Filtering DNS addresses.";
				Match match = Config.domainCheck.Match(line);
				if (!match.Success && !Config.domainRegex.IsMatch(line)) return;
				string domain = match.Groups[1].Value.ToLower();
				if (tempRegex.IsMatch(domain)) {
					tempDomains.Add(domain);
					newTemps.Add(domain);
					return;
				}
				if (!domains.ContainsKey(domain) && !goodDNS.Contains(domain) && !badDNS.Contains(domain) && !tempDomains.Contains(domain))
					domains.TryAdd(domain, 0);
			});
			goodDNS = null;
			if (newTemps.Count > 0)
				WriteHashsetToFile(Config.files[4], newTemps, true);
			newTemps = null;
			i = 0;
			ConsoleUtils.WriteColorized($"[{Main.step}.{sstep}] ", ConsoleColor.Green);
			if (domains.IsEmpty) {
				Console.WriteLine("No new DNS addresses found.");
				return badDNS.ToHashSet();
			}
			List<List<string>> chunks = domains.Keys.ToList()
				.Select((domain, index) => new { domain, index })
				.GroupBy(x => x.index / 40)
				.Select(g => g.Select(x => x.domain).ToList())
				.ToList();
			domains = null;
			Main.lines = chunks.Sum(chunk => chunk.Count);
			roundedCount = Math.Ceiling(Main.lines / 100);
			Console.Write($"Done! There is {Main.lines} domains to check.\n");
			sstep++;
			ConsoleUtils.WriteColorized($"[{Main.step}.{sstep}] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Checking DNS's MX of domains. Please wait...");
			Console.Title = $"[{Main.step}.{sstep}] 0% | Checking DNS's MX of domains.";
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
								badDNSBuilder.AppendLine(domain);
								badDNS.Add(domain);
								Interlocked.Increment(ref badDNSCount);
							}
							i = goodDNSCount + badDNSCount;
							if (i % roundedCount == 0)
								Console.Title = $"[{Main.step}.{sstep}] {Math.Round(i / roundedCount, 0)}% | Checking DNS's MX of domains.";
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
			ConsoleUtils.WriteColorized($"[{Main.step}.{sstep}] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is new {badDNSCount} bad DNS's and {goodDNSCount} good DNS's.");
			return badDNS.ToHashSet();
		}

		public static void GenerDelete(string path, ref long badCount, out HashSet<string> good1, out HashSet<string> bad1) {
			HashSet<string> good = new(), bad = new(), mail = new(), dmail = new();
			List<string> lines = new();
			using (StreamReader f = new(path)) {
				string line;
				while ((line = f.ReadLine()) != null) {
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '@' || splitter == '\0') continue;
					string[] logpass = line.Split(splitter);
					string log = logpass[0];
					if (!mail.Add(log))
						dmail.Add(log);
					lines.Add(line);
				}
			}
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				if (splitter == '@' || splitter == '\0') continue;
				string[] logpass = line.Split(splitter);
				string log = logpass[0];
				if (!dmail.Contains(log))
					good.Add(line);
				else {
					bad.Add($"#DupMail# {line}");
					badCount++;
				}
			}
			bad1 = bad;
			good1 = good;
		}

		public static void WriteSBToFile(StringBuilder sb, int length, string file) {
			if (sb.Length < length) return;
			File.AppendAllText(file, sb.ToString());
			sb.Clear();
		}

		public static void WriteCBToFile(ConcurrentBag<string> cb, int length, string file) {
			if (cb.Count < length) return;
			File.AppendAllText(file, string.Join(Environment.NewLine, cb));
			cb.Clear();
		}
	}
}