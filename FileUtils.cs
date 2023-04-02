using consoleutils;
using DnsClient;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using config;

namespace fileutils {

	public class FileUtils {
		public static readonly string[] files = new string[5] { "BadDNS", "FixDomains", "FixZone", "GoodDNS", "TempMails" };
		public static readonly HashSet<string> tempDomains = WriteToHashSet(files[4]);

		public static long GetLinesCount(string path) {
			long i = 0;
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

        public static Dictionary<string, string> WriteToDictionary(string fileName) {
			var result = new Dictionary<string, string>();
			foreach (var line in File.ReadLines(fileName)) {
				var parts = line.Split('=');
				if (parts.Length == 2)
					result[parts[0]] = parts[1];
			}
			return result;
		}

		public static async Task<HashSet<string>> DNSCheck(string path, float linesCount, int step) {
			string[] files = new string[5] { "BadDNS", "FixDomains", "FixZone", "GoodDNS", "TempMails" };
			HashSet<string> goodDNS = WriteToHashSet(files[3]);
			ConcurrentBag<string> badDNS = new(WriteToHashSet(files[0]));
			ConcurrentBag<string> domains = new();
			int sstep = 1;
            ConsoleUtils.WriteColorized($"[{step}.{sstep}] ", ConsoleColor.DarkYellow);
            Console.WriteLine("Filtering DNS addresses.");
            Console.Title = $"[{step}.{sstep}] 0% | Filtering DNS addresses.";
			double roundedCount = Math.Ceiling((double)linesCount / 100), i = 0;
			Parallel.ForEach(File.ReadLines(path), Config.options, line => {
				if (i++ % roundedCount == 0)
					Console.Title = $"[{step}] {Math.Round(i / roundedCount, 0)}% | Filtering DNS addresses.";
				Match match = Config.domainCheck.Match(line);
				if (!match.Success && !Config.domainRegex.IsMatch(line))
					return;
				string domain = match.Groups[1].Value.ToLower();
				if (!domains.Contains(domain) && !goodDNS.Contains(domain) && !badDNS.Contains(domain) && !FileUtils.tempDomains.Contains(domain))
					domains.Add(domain);
			});
			goodDNS = null;
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
			roundedCount = Math.Ceiling((double)linesCount / 100);
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
				Timeout = TimeSpan.FromSeconds(1)
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
							for (int y = 0; y < 2; ++y) {
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
			File.AppendAllText("BadDNS", badDNSBuilder.ToString());
			File.AppendAllText("GoodDNS", goodDNSBuilder.ToString());

			badDNSBuilder = null;
			goodDNSBuilder = null;
			ConsoleUtils.WriteColorized($"[{step}.{sstep}] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is new {badDNSCount} bad DNS's and {goodDNSCount} good DNS's.");
			return badDNS.ToHashSet();
		}

		public static void GenerDelete(string path, ref long badCount, ref HashSet<string> good, ref HashSet<string> bad) {
			HashSet<string> password = new();
			HashSet<string> dpassword = new();
			HashSet<string> login = new();
			HashSet<string> dlogin = new();
			HashSet<string> lines = new();
			using (StreamReader f = new(path)) {
				string i;
				while ((i = f.ReadLine()) != null) {
					string[] logpass = i.Split(':');
					string log = logpass[0].Split('@')[0];
					string pas = logpass[1];
					if (login.Contains(log))
						dlogin.Add(log);
					else if (password.Contains(pas))
						dpassword.Add(pas);
					login.Add(log);
					password.Add(pas);
					lines.Add(i);
				}
			}
			foreach (string i in lines) {
                string[] logpass = i.Split(':');
                string log = logpass[0].Split('@')[0];
                string pas = logpass[1];
				if (!dlogin.Contains(log) && !dpassword.Contains(pas))
					good.Add(i);
				else {
					bad.Add($"#Generated# {i}");
					badCount++;
                }
            }
		}

		public static void WriteSBToFile(StringBuilder sb, int length, string file) {
			if (sb.Length < length) return;
            File.AppendAllText(file, sb.ToString());
            sb.Clear();
        }
	}
}