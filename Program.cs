//using System.Text;
using System.Net;
using System.Text.RegularExpressions;

bool check = false;
if (!File.Exists("TempMails")) {
	Console.WriteLine("\"TempMails\" file not found.");
	check = true;
}
if (!File.Exists("GoodDNS")) {
	Console.WriteLine("\"GoodDNS\" file not found.");
	check = true;
}
if (!File.Exists("FixZone")) {
	Console.WriteLine("\"FixZone\" file not found.");
	check = true;
}
if (!File.Exists("FixDomains")) {
	Console.WriteLine("\"FixDomains\" file not found.");
	check = true;
}
if (!File.Exists("BigDomains")) {
	Console.WriteLine("\"BigDomains\" file not found.");
	check = true;
}
if (!File.Exists("BadMail")) {
	Console.WriteLine("\"BadMail\" file not found.");
	check = true;
}
if (!File.Exists("BadDomain")) {
	Console.WriteLine("\"BadDomain\" file not found.");
	check = true;
}
if (!File.Exists("BadDNS")) {
	Console.WriteLine("\"BadDNS\" file not found.");
	check = true;
}
if (check) {
	Console.ReadLine();
	Environment.Exit(0);
}

string? tmpline;
List<string> tempdomains = new();
using StreamReader reader = new("TempMails");
while ((tmpline = reader.ReadLine()) != null)
	tempdomains.Add(tmpline);
reader.Close();
List<string> gooddomains = new();
using StreamReader reader1 = new("GoodDNS");
while ((tmpline = reader1.ReadLine()) != null)
	gooddomains.Add(tmpline);
reader1.Close();
List<string> fixdomains = new();
using StreamReader reader2 = new("FixDomains");
while ((tmpline = reader2.ReadLine()) != null)
	fixdomains.Add(tmpline);
reader2.Close();
List<string> fixzones = new();
using StreamReader reader3 = new("FixZone");
while ((tmpline = reader3.ReadLine()) != null)
	fixzones.Add(tmpline);
reader3.Close();
int GetLinesCount(string path) {
	int i = 0;
	using (StreamReader sr = new(path))
		while (sr.ReadLine() != null)
			i++;
	return i;
}

async Task TempToTxtAsync(string file) {
	if (!File.Exists(file)) return;
	using (StreamReader reader = new(file)) {
		HashSet<string> lines = new();
		List<string> tempList = new();
		while (await reader.ReadLineAsync() is string line)
			if (!lines.Contains(line)) {
				lines.Add(line);
				tempList.Add(line);
			}
		lines.Clear();
		tempList.Sort();
		Clean();
		using StreamWriter writer = new(file.Replace(".tmp", ".txt"));
		foreach (string line in tempList)
			await writer.WriteLineAsync(line);
		writer.Close();
		tempList.Clear();
		Clean();
	}
	File.Delete(file);
}

void Clean() {
	GC.Collect();
	GC.WaitForPendingFinalizers();
}

string HexFix(string line) {
	line = line.Replace("$HEX[", "").Replace("]", "");
	return string.Join("", Enumerable.Range(0, line.Length)
		.Where(x => x % 2 == 0)
		.Select(x => (char)int.Parse(line.Substring(x, 2),
		System.Globalization.NumberStyles.HexNumber)));
}

async Task<List<string>> DNSCheck(string path, int lines_c) {
	List<string> bigdomains = new(), domains = new(), baddns = new();
	using StreamReader reader1 = new("BigDomains");
	while ((tmpline = reader1.ReadLine()) != null)
		bigdomains.Add(tmpline);
	reader1.Close();
	using StreamReader reader2 = new("BadDNS");
	while ((tmpline = reader2.ReadLine()) != null)
		baddns.Add(tmpline);
	reader2.Close();
	
	string? line;
	int i = 0;
	using StreamReader reader = new(path);
	while ((line = reader.ReadLine()) != null) {
		if ((float)i / lines_c * 100 % 2 == 0) {
			Clean();
			Console.Clear();
			Console.WriteLine($"Checking DNS addresses.\n{i} / {lines_c}");
			Console.Title = $"{Math.Round((float)i / lines_c * 100, 2)}% | {i} / {lines_c} | Checking DNS addresses.";
		}
		if (domainRegex().IsMatch(line) && !domains.Contains(domainRegex().Match(line).Groups[1].Value) && !bigdomains.Contains(domain1Regex().Match(line).Groups[1].Value) && !gooddomains.Contains(domain1Regex().Match(line).Groups[1].Value) && !baddns.Contains(domain1Regex().Match(line).Groups[1].Value) && !tempdomains.Contains(domain1Regex().Match(line).Groups[1].Value))
			domains.Add(domainRegex().Match(line).Groups[1].Value.ToLower());
		i++;
	}
	reader.Close();
	if (domains.Count == 0) return domains;
	Console.Clear();
	Console.WriteLine($"Checking DNS addresses.\nSorting.");
	domains.Sort();
	int max = domains.Count;
	i = 0;
	while (i < max) {
		if (i % 10 == 0) {
			Console.Clear();
			Console.WriteLine($"Checking DNS addresses.\nChecking TXT of {i} / {max}.");
			Console.Title = $"{Math.Round((float)i / max * 100, 2)}% | {i} / {max} | Checking DNS addresses. Checking TXT.";
		}
		if (domains[i] == "") {
			i++;
			continue;
		}
		try {
			CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(1));
			var txtRecords = await Dns.GetHostEntryAsync(domains[i], tokenSource.Token);
			if (txtRecords.AddressList.Length == 0) {
				baddns.Add(domains[i]);
				File.AppendAllText("BadDNS", $"{domains[i]}\r\n");
			}
			else
				File.AppendAllText("GoodDNS", $"{domains[i]}\r\n");
		}
		catch (Exception) {
			baddns.Add(domains[i]);
			File.AppendAllText("BadDNS", $"{domains[i]}\r\n");
		}
		i++;
	}
	Clean();
	return baddns;
}

string ZoneFix(string line) {
	string line1 = "";
	for (int i = 1; i < line.Split('.').Length; i++)
		line1 += $".{line.Split('.')[i]}";
	foreach (string zone in fixzones)
		if (Regex.IsMatch(line1, zone.Split('=')[0])) {
			line1 = line1.Replace(line1, zone.Split('=')[1]);
			break;
		}
	return line1;
}

string DomainFix(string line) {
	foreach (string domain in fixdomains) {
		if (line.Contains(domain.Split('=')[0])) {
			line = line.Replace($"{line.Split('.')[0]}.", domain.Split('=')[1]);
			break;
		}
	}
	return line.Split('.')[0] + ZoneFix(line);
}

string ProcessLine(string line, List<string> baddns) {
	string[] gmails = new string[2] { "googlemail.com", "gmail.com" };
	string[] yandexs = new string[6] { "ya.ru", "yandex.com", "yandex.ru", "yandex.by", "yandex.kz", "yandex.ua" };

	//EMAIL REGEX CHECK

	if (!loginpassRegex().IsMatch(line))
		return $"#BadSyntax#";

	string[] mailpass = line.Split(":");

	//PASS CHECK

	if (mailpass[1] == "")
		return $"#EmptyPass#";

	string[] logindomain = mailpass[0].ToLower().Split("@");

	//EMAIL CHECK

	if (line.Contains("xrum"))
		return $"#Xrumer#";

	if (mailpass[1] == logindomain[0] ||
		mailpass[1] == logindomain[1] ||
		mailpass[1] == mailpass[0])
		return $"#PassIsLogin#";

	if (gmails.Contains(logindomain[1]) && logindomain[0].Contains("xxxx"))
		return $"#GoogleXXXX#";

	if (tempdomains.Contains(logindomain[1]))
		return $"#TempMail#";

	//DOMAIN FIX

	if (baddns.Contains(logindomain[1]))
		return $"#BadDNS#";

	//ALL LINE FIX

	if (htmlencodeRegex().Matches(line).Count > 0)
		line = WebUtility.HtmlDecode(line);

	//LOGIN FIX

	if (logindomain[0].Contains('+'))
		logindomain[0] = logindomain[0].Split('+')[0];

	if (gmails.Contains(logindomain[1]) && logindomain[0].Contains('.'))
		logindomain[0] = logindomain[0].Replace(".", "");

	if (yandexs.Contains(logindomain[1]) && logindomain[0].Contains('.'))
		logindomain[0] = logindomain[0].Replace(".", "-");

	//PASS FIX

	if (mailpass[1].Contains("{slash}") || mailpass[1].Contains("{eq}"))
		mailpass[1] = mailpass[1].Replace("{slash}", "/").Replace("{eq}", "=");

	if (mailpass[1].Contains("$HEX["))
		mailpass[1] = HexFix(mailpass[1]);

	//if (base64Regex().IsMatch(mailpass[1]))
	//	mailpass[1] = Encoding.UTF8.GetString(Convert.FromBase64String(mailpass[1]));

	return $"{logindomain[0]}@{logindomain[1]}:{mailpass[1]}";
}

async Task MainWork(string path) {
	string? line, result, filename = Path.GetFileNameWithoutExtension(path);
	int i = 0, shit_c = 0, good_c = 0, lines = GetLinesCount(path);
	if (File.Exists($"{filename}_shit.txt")) File.Delete($"{filename}_shit.txt");
	if (File.Exists($"{filename}_shit.tmp")) File.Delete($"{filename}_shit.tmp");
	if (File.Exists($"{filename}_good.txt")) File.Delete($"{filename}_good.txt");
	if (File.Exists($"{filename}_good.tmp")) File.Delete($"{filename}_good.tmp");
	if (File.Exists($"{filename}.txt")) File.Delete($"{filename}.txt");
	using StreamReader reader1 = new(path);
	while ((line = reader1.ReadLine()) != null) {
		if (i % 2000 == 0) {
			Console.Clear();
			Console.WriteLine($"Working.\nFixing domains and writing to temp file.\n{i} / {lines}");
			Console.Title = $"{Math.Round((float)i / (float)lines * 100, 2)}% | {i} / {lines} | Fixing domains and writing to temp file";
		}
		try {
			if (loginpassRegex().IsMatch(line)) {
				line = $"{line.Split(':')[0].Split('@')[0]}@{DomainFix(line.Split(':')[0].Split('@')[1])}:{line.Split(':')[1]}";
				File.AppendAllText($"{filename}.txt", $"{line}\r\n");
			}
		}
		catch (Exception) {}
		i++;
	}
	reader1.Close();
	Console.Clear();
	Console.WriteLine($"Working.\nFixing domains and writing to temp file.\n{i} / {lines}");
	Console.Title = $"{Math.Round((float)i / (float)lines * 100, 2)}% | {i} / {lines} | Fixing domains and writing to temp file";
	i = 0;
	List<string> baddns = await DNSCheck($"{filename}.txt", lines);
	lines = GetLinesCount($"{filename}.txt");
	Console.Clear();
	Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c} | % 0 / 0 %");
	Console.Title = $"Working with {filename}. {i} / {lines} | Good: {good_c} | Shit: {shit_c}";
	using StreamReader reader = new($"{filename}.txt");
	while ((line = reader.ReadLine()) != null) {
		i++;
		if (i % 2000 == 0) {
			if (i % 10000 == 0)
				Clean();
			Console.Clear();
			Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c} | % {Math.Round(((float)good_c / (float)i) * 100, 2)} / {Math.Round(((float)shit_c / (float)i) * 100, 2)} %");
			Console.Title = $"{Math.Round((float)i / lines * 100, 2)}% | {i} / {lines} | {filename} | Good / Shit: {good_c} / {shit_c}";
		}
		result = ProcessLine(line, baddns);
		if (shitRegex().IsMatch(result)) {
			File.AppendAllText($"{filename}_shit.tmp", $"{result} {line}\r\n");
			shit_c++;
		}
		else {
			File.AppendAllText($"{filename}_good.tmp", $"{result}\r\n");
			good_c++;
		}
	}
	reader.Close();
	Console.Clear();
	Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c} | % {Math.Round(((float)good_c / (float)i) * 100, 2)} / {Math.Round(((float)shit_c / (float)i) * 100, 2)} %");
	Console.Title = $"Working with {filename}. {i} / {lines} | Good: {good_c} | Shit: {shit_c}";
	File.Delete($"{filename}.txt");
	await TempToTxtAsync($"{filename}_shit.tmp");
	TempToTxtAsync($"{filename}_good.tmp").Wait();
	Console.Title = "Idle.";
}

Console.WriteLine("Created for @SilverBulletRU");
while (true) {
	Console.Title = "Idle.";
	Console.WriteLine("Drop a file here: ");
	string? path = Console.ReadLine().Replace("\"", "");
	Console.Clear();
	if (pathRegex().IsMatch(path) && File.Exists(path)) {
		MainWork(path).Wait();
	}
	else {
		Console.WriteLine("Drop valid .txt file that exists.");
		continue;
	}
	Console.WriteLine("Do you want to exit? ( y / n ): ");
	path = Console.ReadLine();
	if (path == "y" || path == "Y")
		break;
	else Console.Clear();
}

partial class Program {
	[GeneratedRegex(@"^[A-Za-z]:(?:\\[^\\\/:*?""<>\|]+)*\\[^\\\/:*?""<>\|]+\.txt$")]
	private static partial Regex pathRegex();

	[GeneratedRegex(@"^(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\]):.*?$")]
	private static partial Regex loginpassRegex();

	[GeneratedRegex(@"^#\w+#$")]
	private static partial Regex shitRegex();

	[GeneratedRegex(@"^(?:[A-z\d+/]{4})*(?:[A-z\d+/]{2}==|[A-z\d+/]{3}=)?$")]
	private static partial Regex base64Regex();

	[GeneratedRegex(@"&[A-Za-z\d]{2,6};")]
	private static partial Regex htmlencodeRegex();

	[GeneratedRegex(@"@(?:[a-z\d](?:[a-z\d-]*[a-z\d])?\.)+[a-z\d](?:[a-z\d-]*[a-z\d])?|\[(?:(?:(2(5[0-5]|[0-4]\d)|1\d\d|[1-9]?\d))\.){3}(?:(2(5[0-5]|[0-4]\d)|1\d\d|[1-9]?\d)|[a-z\d-]*[a-z\d]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\]:")]
	private static partial Regex domainRegex();

	[GeneratedRegex(@"@([a-z\d](?:[a-z\d-]*[a-z\d])?)\..*?:")]
	private static partial Regex domain1Regex();
}