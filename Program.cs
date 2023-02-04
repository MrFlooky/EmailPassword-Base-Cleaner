using System.Net;
using System.Text;
using System.Text.RegularExpressions;

bool check = false;
string[] files = new string[5] { "BadDNS", "FixDomains", "FixZone", "GoodDNS", "TempMails" };
foreach (string file in files)
	if (!File.Exists(file)) {
		Console.WriteLine($"\"{file}\" file not found.");
		if (!check) check = true;
	}
if (check) {
	Console.ReadLine();
	Environment.Exit(0);
}
string[] gmails = new string[2] { "googlemail.com", "gmail.com" };
string[] yandexs = new string[6] { "ya.ru", "yandex.com", "yandex.ru", "yandex.by", "yandex.kz", "yandex.ua" };
Config config = new();

static List<string> ReadFile(string fileName) {
    var list = new List<string>();
    using (var reader = new StreamReader(fileName)) {
        string line;
        while ((line = reader.ReadLine()) != null)
            list.Add(line);
    }
    return list;
}

List<string> fixdomains = ReadFile("FixDomains");
List<string> fixzones = ReadFile("FixZone");
List<string> tempdomains = ReadFile("TempMails");
List<string> bigdomains = ReadFile("BigDomains");

bool SetPartConfig(string msg) {
	string temp;
	bool tempb;
	while (true) {
		Console.WriteLine(msg);
		temp = Console.ReadLine();
		if (Regex.IsMatch(temp, "^[ynYN]$")) {
			tempb = temp == "y" || temp == "Y";
			break;
		}
		else Console.WriteLine("Invalid input, try again.");
	}
	return tempb;
}

void SetConfig(string msg) {
	Console.WriteLine(msg);
	Console.WriteLine("Note: If all answers is \"n\", program will fix only syntax errors.");
	config.checkdns = SetPartConfig("Check DNS of domains in emails? y / n");
	config.removexxxx = SetPartConfig("Remove \"xxxx\" in google mails? y / n");
	config.removeemptypass = SetPartConfig("Remove empty passwords? y / n");
	config.removexrumer = SetPartConfig("Remove xrumer (spam) mails? y / n");
	config.removeequalloginpass = SetPartConfig("Remove same login and pass? y / n");
	config.removetempmail = SetPartConfig("Remove temp mails? y / n");
	config.fixdotsgmail = SetPartConfig("Remove dots in google mails? y / n");
	config.fixdotsyandex = SetPartConfig("Replace dots to \"-\" in yandex mails? y / n");
	config.fixplus = SetPartConfig("Remove all after \"+\" in mails? y / n");
	
	string write = $"Check DNS: {config.checkdns}\nRemoveXXXX: {config.removexxxx}\n" +
		$"RemoveEmptyPass: {config.removeemptypass}\nRemoveXrumer: {config.removexrumer}\n" +
		$"RemoveEqualLoginPass: {config.removeequalloginpass}\nRemoveTempMail: {config.removetempmail}\n" +
		$"FixDotsGmail: {config.fixdotsgmail}\nFixDotsYandex: {config.fixdotsyandex}\n" +
		$"FixPlus: {config.fixplus}";
	File.WriteAllText("config.cfg", write);
}

bool CheckConfig() {
	if (!File.Exists("config.cfg")) {
		SetConfig("Config file not found.");
		Console.Clear();
		return false;
	}
	else
		try {
			string[] lines = File.ReadAllLines("config.cfg");
			config.checkdns = bool.Parse(lines[0].Split(": ")[1]);
			config.removexxxx = bool.Parse(lines[1].Split(": ")[1]);
			config.removeemptypass = bool.Parse(lines[2].Split(": ")[1]);
			config.removexrumer = bool.Parse(lines[3].Split(": ")[1]);
			config.removeequalloginpass = bool.Parse(lines[4].Split(": ")[1]);
			config.removetempmail = bool.Parse(lines[5].Split(": ")[1]);
			config.fixdotsgmail = bool.Parse(lines[6].Split(": ")[1]);
			config.fixdotsyandex = bool.Parse(lines[7].Split(": ")[1]);
			config.fixplus = bool.Parse(lines[8].Split(": ")[1]);
		}
		catch {
			SetConfig("Config file is corrupted.");
			Console.Clear();
			return false;
		}
	return true;
}

void PromptConfig() {
	Console.WriteLine("Do you want to change config? y / n: ");
	while (true) {
		tmpline = Console.ReadLine();
		if (Regex.IsMatch(tmpline, "^[YynN]$")) {
			if (tmpline == "y") {
				SetConfig("Changing config file.");
				Console.Clear();
			}
			break;
		}
		Console.WriteLine("Try again.");
	}
}

int GetLinesCount(string path) {
	int i = 0;
	using (StreamReader sr = new(path))
		while (sr.ReadLine() != null)
			i++;
	return i;
}

async Task<int> TempToTxtAsync(string file) {
	if (!File.Exists(file)) return 0;
	int i = 0;
	using (StreamReader reader = new(file)) {
		HashSet<string> lines = new();
		List<string> tempList = new(), loginlist = new();
		while (await reader.ReadLineAsync() is string line) {
			if (!lines.Contains(line) || (!loginlist.Contains(line) && gmails.Any(line.Contains))) {
				lines.Add(line);
				tempList.Add(line);
				loginlist.Add(line);
				i++;
			}
		}
		loginlist = null;
		lines = null;
		tempList.Sort();
		Clean();
		using StreamWriter writer = new(file.Replace(".tmp", ".txt"));
		foreach (string line in tempList)
			await writer.WriteLineAsync(line);
		writer.Close();
		tempList = null;
		Clean();
	}
	//reader.Close();
	File.Delete(file);
	return i;
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

async Task<List<string>> DNSCheck(string path, int lines_c, List<string> tempdomains) {
	List<string> bigdomains = new(), domains = new(), baddns = new();
	using StreamReader reader1 = new("BigDomains");
	string? tmpline;
	while ((tmpline = reader1.ReadLine()) != null)
		bigdomains.Add(tmpline);
	reader1.Close();
	using StreamReader reader2 = new("BadDNS");
	while ((tmpline = reader2.ReadLine()) != null)
		baddns.Add(tmpline);
	reader2.Close();
	List<string> gooddomains = new();
	using StreamReader reader3 = new("GoodDNS");
	while ((tmpline = reader3.ReadLine()) != null)
		gooddomains.Add(tmpline);
	reader3.Close();
	string? line;
	int i = 0;
    Console.Title = $"{Math.Round((float)i / lines_c * 100, 0)}% | {i} / {lines_c} | Checking DNS addresses.";
	using StreamReader reader = new(path);
	while ((line = reader.ReadLine()) != null) {
		Match match = domainCheck.Match(line);
		if (match.Success && !gooddns.Contains(match.Groups[1].Value.ToLower())) {
			string domain = match.Groups[1].Value.ToLower();
			if (!baddns.Contains(domain) && !tempdomains.Contains(domain))
				domains.Add(domain);
			if (Math.Round((float)i / lines_c * 100, 1) % 2 == 0 && (float)i / lines_c * 100 > 1)
				Console.Title = $"{Math.Round((float)i / lines_c * 100, 0)}% | {i} / {lines_c} | Checking DNS addresses.";
			i++;
		}
	}
	reader.Close();

	gooddns = null;
	if (domains.Count == 0) return baddns;
	domains = domains.Distinct().ToHashSet();
	Console.WriteLine($"[2.2] Sorting DNS addresses.");
	List<string> sortedDomains = domains.ToList();
	int max = sortedDomains.Count;
	i = 0;
	List<List<string>> chunks = sortedDomains
		.Select((domain, index) => new { domain, index })
		.GroupBy(x => x.index / 10)
		.Select(g => g.Select(x => x.domain).ToList())
		.ToList();
	List<Task<List<string>>> tasks = new();
    Console.WriteLine($"[2.3] Checking DNS's TXT of domains.");
    foreach (var chunk in chunks)
		tasks.Add(Task.Run(async () => {
			List<string> badDNSChunk = new();
			foreach (string domain in chunk) {
				++i;
				if ((float)i / max * 1000000 % 2 == 0)
					Console.Title = $"{Math.Round((float)i / max * 100, 0)}% | {i} / {max} | Checking TXT of DNS addresses.";
				try {
					CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(2));
					var txtRecords = await Dns.GetHostEntryAsync(domain, tokenSource.Token);
					if (txtRecords.AddressList.Length == 0)
						badDNSChunk.Add(domain);
					else
						File.AppendAllText("GoodDNS", $"{domain}\r\n");
				}
				catch (Exception) {
					badDNSChunk.Add(domain);
				}
			}
			return badDNSChunk;
		}));
	await Task.WhenAll(tasks);
	foreach (var task in tasks) {
		var taskResult = await task;
		foreach (var domain in taskResult) {
			baddns.Add(domain);
			File.AppendAllText("BadDNS", $"{domain}\r\n");
		}
	}
	sortedDomains = null;
	domains = null;
	return baddns;
}

string ZoneFix(string line, List<string> fixzones) {
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

string DomainFix(string line, List<string> fixdomains, List<string> fixzones) {
	foreach (string domain in fixdomains)
		if (line.Contains(domain.Split('=')[0])) {
			line = line.Replace($"{line.Split('.')[0]}.", domain.Split('=')[1]);
			break;
		}
	return line.Split('.')[0] + ZoneFix(line, fixzones);
}

string ProcessLine(string line, List<string> baddns, List<string> tempdomains) {
	string[] mailpass = line.Split(":");

	//PASS CHECK

	if (config.removeemptypass && mailpass[1] == "")
		return $"#EmptyPass#";

	string[] logindomain = mailpass[0].ToLower().Split("@");

	//EMAIL CHECK

	if (config.removexrumer && line.Contains("xrum"))
		return $"#Xrumer#";

	if (config.removeequalloginpass &&
		mailpass[1] == logindomain[0] ||
		mailpass[1] == logindomain[1] ||
		mailpass[1] == mailpass[0] ||
		mailpass[1] == $"{logindomain[0]}@{logindomain[1].Split('.')[0]}")
		return $"#PassIsLogin#";

	if (config.removexxxx &&
		gmails.Contains(logindomain[1]) && logindomain[0].Contains("xxxx"))
		return $"#GoogleXXXX#";

	if (config.removetempmail && tempdomains.Contains(logindomain[1]))
		return $"#TempMail#";

	//DOMAIN FIX

	if (config.checkdns && baddns.Contains(logindomain[1]))
		return $"#BadDNS#";

	//ALL LINE FIX

	if (htmlencodeRegex().Matches(line).Count > 0)
		line = WebUtility.HtmlDecode(line);

	//LOGIN FIX

	if (config.fixplus && logindomain[0].Contains('+'))
		logindomain[0] = logindomain[0].Split('+')[0];

	if (config.fixdotsgmail && gmails.Contains(logindomain[1]) && logindomain[0].Contains('.'))
		logindomain[0] = logindomain[0].Replace(".", "");

	if (config.fixdotsyandex && yandexs.Contains(logindomain[1]) && logindomain[0].Contains('.'))
		logindomain[0] = logindomain[0].Replace(".", "-");

	//PASS FIX

	if (mailpass[1].Contains("{slash}") || mailpass[1].Contains("{eq}"))
		mailpass[1] = mailpass[1].Replace("{slash}", "/").Replace("{eq}", "=");

	if (mailpass[1].Contains("$HEX["))
		mailpass[1] = HexFix(mailpass[1]);

	//if (base64Regex().IsMatch(mailpass[1]))
	//	mailpass[1] = Encoding.UTF8.GetString(Convert.FromBase64String(mailpass[1]));

	string result = $"{logindomain[0]}@{logindomain[1]}:{mailpass[1]}";
	if (mailpass.Length > 2)
		foreach (string piece in mailpass.Skip(2))
			result += $":{piece}";

	return result;
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
		if (i % 5000 == 0) {
			Console.Clear();
			Console.WriteLine($"Working.\nFixing domains and writing to temp file.\n{i} / {lines}");
			Console.Title = $"{Math.Round((float)i / (float)lines * 100, 2)}% | {i} / {lines} | Fixing domains and writing to temp file";
		}
		try {
			if (loginpassRegex().IsMatch(line)) {
				string templine = $"{line.Split(':')[0].Split('@')[0]}@{DomainFix(line.Split(':')[0].Split('@')[1], fixdomains, fixzones)}";
				if (line.Split(':').Length >= 2)
					foreach (string piece in line.Split(':').Skip(1))
						templine += $":{piece}";
				File.AppendAllText($"{filename}.txt", $"{templine}\r\n");
			}
			else
				File.AppendAllText($"{filename}_shit.tmp", $"#BadSyntax# {line}\r\n");
		}
		catch (Exception) {}
		i++;
	}
	reader1.Close();
	if (!File.Exists($"{filename}.txt")) {
		Console.Clear();
		Console.Title = $"Idle.";
		Console.WriteLine("No good mail:pass(:other) lines found!");
		return;
	}
	Console.Title = $"{Math.Round((float)i / lines * 100, 0)}% | {i} / {lines} | Fixing domains and writing to temp file";
	i = 0;
	List<string> baddns = await DNSCheck($"{filename}.txt", lines, tempdomains);
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
		result = ProcessLine(line, baddns, tempdomains);
		if (Regex.IsMatch(result, @"^#\w+#$")) {
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
	good_c = await TempToTxtAsync($"{filename}_good.tmp");
	await TempToTxtAsync($"{filename}_shit.tmp");
	shit_c = lines - good_c;
	Console.WriteLine("Done!");
	Console.Title = "Idle.";
}

Console.WriteLine("Created by @SilverBulletRU");
bool checkcfg = false;
while (true) {
	string? tmpline;
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
	List<string> tempdomains = new();
	using StreamReader reader4 = new("TempMails");
	while ((tmpline = reader4.ReadLine()) != null)
		tempdomains.Add(tmpline);
	reader4.Close();
	Console.Title = "Idle.";
	if (CheckConfig()) {
		Console.WriteLine("Do you want to change config? y / n: ");
		while (true) {
			tmpline = Console.ReadLine();
			if (Regex.IsMatch(tmpline, "^[yn]$")) {
				switch (tmpline) {
					case "y":
						SetConfig("Changing config file.");
						Console.Clear();
						break;
					case "n":
						break;
				}
				break;
			}
			else Console.WriteLine("Try again.");
		}
	}
	tmpline = null;
	Console.WriteLine("Drop a file here: ");
	string? path = Console.ReadLine().Replace("\"", "");
	Console.Clear();
	if (pathRegex().IsMatch(path) && File.Exists(path))
		MainWork(path).Wait();
	else {
		Console.WriteLine("Drop valid .txt file that exists.");
		continue;
	}
	Console.WriteLine("Do you want to exit? ( y / n ): ");
	path = Console.ReadLine();
	if (path == "y" || path == "Y") break;
	else Console.Clear();
}

partial class Program {
	[GeneratedRegex(@"^[A-Za-z]:(?:\\[^\\\/:*?""<>\|]+)*\\[^\\\/:*?""<>\|]+\.txt$")]
	private static partial Regex pathRegex();

	[GeneratedRegex(@"^[A-Za-z\d][\w.+-]*@([A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z][A-Za-z\d]{1,10}:.*?$")]
	private static partial Regex loginpassRegex();

	//[GeneratedRegex(@"^(?:[A-z\d+/]{4})*(?:[A-z\d+/]{2}==|[A-z\d+/]{3}=)?$")]
	//private static partial Regex base64Regex();

	[GeneratedRegex(@"&[A-Za-z\d]{2,6};")]
	private static partial Regex htmlencodeRegex();

	[GeneratedRegex(@"@([\w-]+(?:\.[\w-]+)+):")]
	private static partial Regex domainRegex();
}

class Config {
	public bool checkdns = true;
	public bool removexxxx = true;
	public bool removeemptypass = true;
	public bool removexrumer = true;
	public bool removeequalloginpass = true;
	public bool removetempmail = true;
	public bool fixdotsgmail = true;
	public bool fixdotsyandex = true;
    public bool fixplus = true;
}