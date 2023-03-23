using config;
using consoleutils;
using DnsClient;
using fileutils;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace lineutils {

	internal class LineUtils {
		public static async Task<HashSet<string>> DNSCheck(string path, float linesCount) {
			Regex domainCheck = new(@"@([\w-]+(?:\.[\w-]+)+)(?::|$)");
			ParallelOptions options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
			string[] files = new string[5] { "BadDNS", "FixDomains", "FixZone", "GoodDNS", "TempMails" };
			HashSet<string> goodDNS = FileUtils.WriteToHashSet(files[3]);
			ConcurrentBag<string> badDNS = new(FileUtils.WriteToHashSet(files[0]));
			ConcurrentBag<string> domains = new();
			Console.Title = $"[2.1] 0% | Filtering DNS addresses.";
            double roundedCount = Math.Ceiling((double)linesCount / 100), i = 0;
            Parallel.ForEach(File.ReadLines(path), options, line => {
				if (i++ % roundedCount == 0)
					Console.Title = $"[2.1] {Math.Round(i / roundedCount, 0)}% | Filtering DNS addresses.";
				Match match = domainCheck.Match(line);
				if (!match.Success) return;
				string domain = match.Groups[1].Value.ToLower();
				if (!domains.Contains(domain) && !goodDNS.Contains(domain) && !badDNS.Contains(domain) && !FileUtils.tempDomains.Contains(domain))
					domains.Add(domain);
			});
            goodDNS = null;
            i = 0;
			ConsoleUtils.WriteColorized("[2.1] ", ConsoleColor.Green);
			Console.Write($"Done!");
			if (domains.IsEmpty) {
				Console.WriteLine(" No new DNS addresses found.");
				return badDNS.ToHashSet();
			}
			Console.Title = "[2.2] | Sorting DNS addresses.";
			ConsoleUtils.WriteColorized("\n[2.2] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Sorting DNS addresses.");
			List<List<string>> chunks = domains.ToList()
				.Select((domain, index) => new { domain, index })
				.GroupBy(x => x.index / 40)
				.Select(g => g.Select(x => x.domain).ToList())
				.ToList();
			domains = null;
			linesCount = chunks.Sum(chunk => chunk.Count);
			roundedCount = Math.Ceiling((double)linesCount / 100);
			ConsoleUtils.WriteColorized("[2.2] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is {linesCount} domains to check.");
			ConsoleUtils.WriteColorized("[2.3] ", ConsoleColor.DarkYellow);
			Console.WriteLine("Checking DNS's MX of domains. Please wait...");
			Console.Title = $"[2.3] 0% | Checking DNS's MX of domains.";
			StringBuilder badDNSBuilder = new(capacity: 10000000);
			StringBuilder goodDNSBuilder = new(capacity: 10000000);
			var client = new LookupClient(new LookupClientOptions {
				UseCache = true,
				UseTcpOnly = true,
				Timeout = TimeSpan.FromSeconds(1)
			});
			int badDNSCount = 0, goodDNSCount = 0, semaphoreCount = 100;
			if (chunks.Count < 100)
				semaphoreCount = chunks.Count;

			var semaphore = new SemaphoreSlim(semaphoreCount);
			var tasks = new List<Task>();
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
								Console.Title = $"[2.3] {Math.Round(i / roundedCount, 0)}% | Checking DNS's MX of domains.";
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
			ConsoleUtils.WriteColorized("[2.3] ", ConsoleColor.Green);
			Console.WriteLine($"Done! There is new {badDNSCount} bad DNS's and {goodDNSCount} good DNS's.");
			return badDNS.ToHashSet();
		}

		public static string DomainFix(string line, Dictionary<string, string> fixDomains, HashSet<string> domains) {
			string temp = line;
			if (!domains.Contains(line) && fixDomains.ContainsKey(line))
				temp = fixDomains[line];
			return temp;
		}

		public static char GetSplitter(string line) {
			if (line.Contains(';'))
				return ';';
			if (Regex.IsMatch(line, @"^[A-Za-z\d][\w.+-]*@([A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z]?[A-Za-z\d]{1,10}$"))
				return '@';
			return ':';
		}

		public static string HexFix(string line) {
			StringBuilder result = new();
			line = line.Replace("$HEX[", "").Replace("]", "");
			for (int i = 0; i < line.Length; i += 2)
				result.Append(Convert.ToChar(Convert.ToInt32(line.Substring(i, 2), 16)));
			return result.ToString();
		}

		public static string ProcessLine(string line, HashSet<string> badDNS) {
			char splitter = GetSplitter(line);
			bool typeOfLine = true;
			if (splitter == '@')
				typeOfLine = false;
			Regex htmlencodeRegex = new(@"&[A-Za-z\d]{2,6};");

			//ALL LINE FIX

			if (htmlencodeRegex.Matches(line).Count > 0)
				line = WebUtility.HtmlDecode(line);

			string[] gmails = new string[2] { "googlemail.com", "gmail.com" };
			string[] yandexs = new string[5] { "ya.ru", "yandex.com", "yandex.ru", "yandex.by", "yandex.kz" };
			string[] mailPass = line.Split(splitter);
			if (!typeOfLine)
				mailPass = new string[2] { line, "" };

			//PASS CHECK

			if (Config.removeEmptyPass && typeOfLine && mailPass[1] == "")
				return $"#EmptyPass#";

			string[] loginDomain = mailPass[0].ToLower().Split("@");

			//EMAIL CHECK

			if (Config.removeXumer && line.Contains("xrum"))
				return $"#Xrumer#";

			if (Config.removeEqualLoginPass && typeOfLine &&
				mailPass[1] == loginDomain[0] ||
				mailPass[1] == loginDomain[1] ||
				mailPass[1] == mailPass[0] ||
				mailPass[1] == $"{loginDomain[0]}@{loginDomain[1].Split('.')[0]}")
				return $"#PassIsLogin#";

			if (Config.removeXXXX &&
				gmails.Contains(loginDomain[1]) && loginDomain[0].Contains("xxxx"))
				return $"#GoogleXXXX#";

			if (Config.removeTempMail && FileUtils.tempDomains.Contains(loginDomain[1]))
				return $"#TempMail#";

			//DOMAIN FIX

			if (Config.checkDNS && badDNS.Contains(loginDomain[1]))
				return $"#BadDNS#";

			//LOGIN FIX

			if (Config.fixPlus && loginDomain[0].Contains('+'))
				loginDomain[0] = loginDomain[0].Split('+')[0];

			if (Config.fixDotsGmail && gmails.Contains(loginDomain[1]) && loginDomain[0].Contains('.'))
				loginDomain[0] = loginDomain[0].Replace(".", "");

			if (Config.fixDotsYandex && yandexs.Contains(loginDomain[1]) && loginDomain[0].Contains('.'))
				loginDomain[0] = loginDomain[0].Replace(".", "-");

			//PASS FIX

			if (typeOfLine && mailPass[1].Contains("{slash}") || mailPass[1].Contains("{eq}"))
				mailPass[1] = mailPass[1].Replace("{slash}", "/").Replace("{eq}", "=");

			if (typeOfLine && mailPass[1].Contains("$HEX["))
				mailPass[1] = HexFix(mailPass[1]);

			string result = $"{loginDomain[0]}@{loginDomain[1]}:{mailPass[1]}";
			if (!typeOfLine)
				result = $"{loginDomain[0]}@{loginDomain[1]}";

			if (typeOfLine && mailPass.Length > 2)
				foreach (string piece in mailPass.Skip(2))
					result += $":{piece}";

			return result;
		}

		public static string ZoneFix(string line, Dictionary<string, string> fixZones, HashSet<string> zones) {
			if (!line.StartsWith('.'))
				line = $".{line}";
			string temp = line;
			if (!zones.Contains(line))
				foreach (var zone in fixZones)
					if (Regex.IsMatch(line, zone.Key)) {
						temp = zone.Value;
						break;
					}
			return temp;
		}
	}
}