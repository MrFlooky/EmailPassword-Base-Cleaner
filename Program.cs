using System.Net;
using System.Text;
using System.Text.RegularExpressions;

bool check = false;
if (!File.Exists("GoodDNS")) {
	Console.WriteLine("\"GoodDNS\" file not found.");
	check = true;
}
if (!File.Exists("BadDNS")) {
	Console.WriteLine("\"BadDNS\" file not found.");
	check = true;
}
if (!File.Exists("BadMail")) {
	Console.WriteLine("\"BadMail\" file not found.");
	check = true;
}
if (!File.Exists("TempMails")) {
	Console.WriteLine("\"TempMails\" file not found.");
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
    HashSet<string> lines = new();
    List<string> tempList = new();
    using (StreamReader reader = new(file)) {
        string line;
        while ((line = await reader.ReadLineAsync()) != null)
            if (lines.Add(line)) {
                tempList.Add(line);
                i++;
            }
    }
    tempList.Sort();
    using (StreamWriter writer = new(file.Replace(".tmp", ".txt")))
        foreach (string line in tempList)
            await writer.WriteLineAsync(line);
    tempList = null;
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

async Task<List<string>> DNSCheck(string path, int lines_c) {
	List<string> domains = new();
    List<string> baddns = ReadFile("BadDNS");
    List<string> gooddomains = ReadFile("GoodDNS");
    string? line;
	int i = 0;
	using StreamReader reader = new(path);
	while ((line = reader.ReadLine()) != null) {
		if (i % 2000 == 0) {
			Console.Clear();
			Console.WriteLine($"Checking DNS addresses.\n{i} / {lines_c}");
			Console.Title = $"{Math.Round((float)i / (float)lines_c * 100, 2)}% | {i} / {lines_c} | Checking DNS addresses.";
		}
		if (domainRegex().IsMatch(line) && !bigdomains.Contains(domain1Regex().Match(line).Groups[1].Value) && !gooddomains.Contains(domainRegex().Match(line).Groups[1].Value) && !baddns.Contains(domainRegex().Match(line).Groups[1].Value) && !tempdomains.Contains(domainRegex().Match(line).Groups[1].Value))
			domains.Add(domainRegex().Match(line).Groups[1].Value.ToLower());
		i++;
	}
	reader.Close();
	bigdomains = null;
	gooddomains = null;
	Clean();
	if (domains.Count == 0) return baddns;
	domains = domains.Distinct().ToList();
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
	domains = null;
	Clean();
	return baddns;
}

string ZoneFix(string line) {
    var lineParts = line.Split('.');
    var sb = new StringBuilder();
    for (int i = 1; i < lineParts.Length; i++)
        sb.Append("." + lineParts[i]);
    string line1 = sb.ToString();
    foreach (string zone in fixzones)
        if (Regex.IsMatch(line1, zone.Split('=')[0])) {
            line1 = line1.Replace(line1, zone.Split('=')[1]);
            break;
        }
    return line1;
}

string DomainFix(string line) {
	foreach (string domain in fixdomains)
		if (line.Contains(domain.Split('=')[0])) {
			line = line.Replace($"{line.Split('.')[0]}.", domain.Split('=')[1]);
			break;
		}
	return line.Split('.')[0] + ZoneFix(line);
}

string ProcessLine(string line, List<string> baddns) {
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
    var extensions = new string[] { "_shit.txt", "_shit.tmp", "_good.txt", "_good.tmp", ".txt" };
    foreach (var extension in extensions)
        if (File.Exists($"{filename}{extension}"))
            File.Delete($"{filename}{extension}");
    using StreamReader reader1 = new(path);
	while ((line = reader1.ReadLine()) != null) {
		if (i % 5000 == 0) {
			Console.Clear();
			Console.WriteLine($"Working.\nFixing domains and writing to temp file.\n{i} / {lines}");
			Console.Title = $"{Math.Round((float)i / (float)lines * 100, 2)}% | {i} / {lines} | Fixing domains and writing to temp file";
		}
		try {
            if (loginpassRegex().IsMatch(line)) {
                var stringBuilder = new StringBuilder($"{line.Split(':')[0].Split('@')[0]}@{DomainFix(line.Split(':')[0].Split('@')[1])}");
                if (line.Split(':').Length >= 2)
                    foreach (string piece in line.Split(':').Skip(1))
                        stringBuilder.Append($":{piece}");
                File.AppendAllText($"{filename}.txt", $"{stringBuilder}\r\n");
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
        File.AppendAllText(Regex.IsMatch(result, @"^#\w+#$") ? $"{filename}_shit.tmp" : $"{filename}_good.tmp", $"{result} {line}\r\n");
        if (Regex.IsMatch(result, @"^#\w+#$")) shit_c++;
        else good_c++;
    }
	reader.Close();
	Console.Clear();
	Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c} | % {Math.Round(((float)good_c / (float)i) * 100, 2)} / {Math.Round(((float)shit_c / (float)i) * 100, 2)} %");
	Console.Title = $"Working with {filename}. {i} / {lines} | Good: {good_c} | Shit: {shit_c}";
	File.Delete($"{filename}.txt");
	good_c = await TempToTxtAsync($"{filename}_good.tmp");
	await TempToTxtAsync($"{filename}_shit.tmp");
	shit_c = lines - good_c;
	Console.Clear();
	Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c} | % {Math.Round(((float)good_c / (float)i) * 100, 2)} / {Math.Round(((float)shit_c / (float)i) * 100, 2)} %");
	Console.Title = $"Working with {filename}. {i} / {lines} | Good: {good_c} | Shit: {shit_c}";
	Console.Title = "Idle.";
}

Console.WriteLine("Created for @SilverBulletRU");

while (true) {
	string? tmpline;
	Console.Title = "Idle.";
	if (CheckConfig()) {
		Console.WriteLine("Do you want to change config? y / n: ");
		while (true) {
			tmpline = Console.ReadLine();
			if (Regex.IsMatch(tmpline, "^[Yyn]$")) {
				if (tmpline == "y" || tmpline == "Y") {
					SetConfig("Changing config file.");
					Console.Clear();
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

	[GeneratedRegex(@"^[^._+-][\w\.\+-]+@([A-z\d-]+\.)+[A-z\d]{2,11}:.*?$")]
	private static partial Regex loginpassRegex();

	//[GeneratedRegex(@"^(?:[A-z\d+/]{4})*(?:[A-z\d+/]{2}==|[A-z\d+/]{3}=)?$")]
	//private static partial Regex base64Regex();

	[GeneratedRegex(@"&[A-Za-z\d]{2,6};")]
	private static partial Regex htmlencodeRegex();

	[GeneratedRegex(@"@([\w-]+(?:\.[\w-]+)+):")]
	private static partial Regex domainRegex();

	[GeneratedRegex(@"@([\w-]+)\..*?:")]
	private static partial Regex domain1Regex();
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