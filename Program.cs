using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
//using System.Diagnostics.Metrics;
//using System.Reflection;
//using Microsoft.Win32;

Console.WriteLine("Created by @SilverBulletRU");
bool check = false;
string[] files = new string[5] { "BadDNS", "FixDomains", "FixZone", "GoodDNS", "TempMails" };
//if (!Directory.Exists(@"C:\Windows\BaseCleaner"))
//	Directory.CreateDirectory(@"C:\Windows\BaseCleaner");
foreach (string file in files) {
	/*if (File.Exists(file)) {
		if (args.Length > 0)
		//if (Assembly.GetExecutingAssembly().Location.Contains(@"C:\Windows"))
			continue;
		File.Delete($@"C:\Windows\BaseCleaner\{file}");
		File.Copy(file, $@"C:\Windows\BaseCleaner\{file}");
		continue;
	}*/
	if (File.Exists(file)) continue;
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

bool checkCfg = false;
if (CheckConfig() && !checkCfg) {
	checkCfg = true;
	PromptConfig();
}
//if (!Assembly.GetExecutingAssembly().Location.Contains(@"C:\Windows"))
//	File.Copy("config.cfg", $@"C:\Windows\BaseCleaner\config.cfg", true);

HashSet<string> fixDomains = WriteToHash(files[1]);
HashSet<string> fixZones = WriteToHash(files[2]);
HashSet<string> tempDomains = WriteToHash(files[4]);

/*int AddContextMenuItem() {
	string currentFileName = @"C:\Windows\BaseCleaner\basecleaner1.3.exe";
	int status = 0;
	if (!File.Exists(currentFileName)) {
		File.Copy(Assembly.GetExecutingAssembly().Location, currentFileName, true);
		status = 1;
	}
	
	if (Registry.GetValue(@"HKEY_CLASSES_ROOT\*\shell\BaseCleaner", "", null) == null) {
		RegistryKey key = Registry.ClassesRoot.CreateSubKey("*\\shell\\BaseCleaner");
		key.SetValue("", "Clean the base");
		key.SetValue("Icon", "SHELL32.dll,312");
		key = key.CreateSubKey("command");
		key.SetValue("", $"\"{currentFileName}\" \"%1\"");
		key.Close();
		status = 2;
	}
	return status;
}*/

void WriteColorized(string text, ConsoleColor color) {
	Console.ForegroundColor = color;
	Console.Write(text);
	Console.ResetColor();
}

HashSet<string> WriteToHash(string fileName) {
	string? tmpLine;
	HashSet<string> list = new((int)GetLinesCount(fileName));
	using (StreamReader reader = new(fileName))
		while ((tmpLine = reader.ReadLine()) != null)
			list.Add(tmpLine);
	return list;
}

bool SetPartConfigBool(string msg) {
	string temp;
	bool tempb;
	while (true) {
		Console.WriteLine(msg);
		temp = Console.ReadLine();
		if (Regex.IsMatch(temp, "^[yn]$")) {
			tempb = temp == "y";
			break;
		}
		Console.WriteLine("Invalid input, try again.");
	}
	return tempb;
}

void SetConfig(string msg) {
	Console.WriteLine(msg);
	Console.WriteLine("Note: If all answers is \"n\", program will fix only syntax errors.");
	config.checkDNS = SetPartConfigBool("Check DNS of domains in emails? y / n");
	config.removeXXXX = SetPartConfigBool("Remove \"xxxx\" in google mails? y / n");
	config.removeEmptyPass = SetPartConfigBool("Remove empty passwords? y / n");
	config.removeXumer = SetPartConfigBool("Remove xrumer (spam) mails? y / n");
	config.removeEqualLoginPass = SetPartConfigBool("Remove same login and pass? y / n");
	config.removeTempMail = SetPartConfigBool("Remove temp mails? y / n");
	config.fixDotsGmail = SetPartConfigBool("Remove dots in google mails? y / n");
	config.fixDotsYandex = SetPartConfigBool("Replace dots to \"-\" in yandex mails? y / n");
    config.fixPlus = SetPartConfigBool("Remove all after \"+\" in mails? y / n");

    string write = $"Check DNS: {config.checkDNS}\nRemoveXXXX: {config.removeXXXX}\n" +
		$"RemoveEmptyPass: {config.removeEmptyPass}\nRemoveXrumer: {config.removeXumer}\n" +
		$"RemoveEqualLoginPass: {config.removeEqualLoginPass}\nRemoveTempMail: {config.removeTempMail}\n" +
		$"FixDotsGmail: {config.fixDotsGmail}\nFixDotsYandex: {config.fixDotsYandex}\n" +
		$"FixPlus: {config.fixPlus}";
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
			config.checkDNS = bool.Parse(lines[0].Split(": ")[1]);
			config.removeXXXX = bool.Parse(lines[1].Split(": ")[1]);
			config.removeEmptyPass = bool.Parse(lines[2].Split(": ")[1]);
			config.removeXumer = bool.Parse(lines[3].Split(": ")[1]);
			config.removeEqualLoginPass = bool.Parse(lines[4].Split(": ")[1]);
			config.removeTempMail = bool.Parse(lines[5].Split(": ")[1]);
			config.fixDotsGmail = bool.Parse(lines[6].Split(": ")[1]);
			config.fixDotsYandex = bool.Parse(lines[7].Split(": ")[1]);
            config.fixPlus = bool.Parse(lines[8].Split(": ")[1]);
        }
		catch {
			SetConfig("Config file is corrupted.");
			Console.Clear();
			return false;
		}
	return true;
}

void PromptConfig() {
	Console.Write("Do you want to change config? ( ");
	WriteColorized("y", ConsoleColor.Red);
	Console.Write(" / ");
	WriteColorized("n", ConsoleColor.Green);
	Console.WriteLine(" ):");
	while (true) {
		string? tmpLine = Console.ReadLine();
		if (Regex.IsMatch(tmpLine, "^[YynN]$")) {
			if (tmpLine == "y") {
				SetConfig("Changing config file.");
				Console.Clear();
			}
			break;
		}
		Console.WriteLine("Try again.");
	}
}

float GetLinesCount(string path) {
	float i = 0;
	using (StreamReader sr = new(path))
		while (sr.ReadLine() != null)
			i += 1;
	return i;
}

async Task<int> TempToTxtAsync(string file) {
	if (!File.Exists(file)) return 0;
	int i = 0;
	using (StreamReader reader = new(file)) {
		HashSet<string> tempList = new();
		while (await reader.ReadLineAsync() is string line)
			if (tempList.Add(line))
				i++;
		using StreamWriter writer = new(file.Replace(".tmp", ".txt"));
		foreach (string line in tempList.OrderBy(x => x))
			await writer.WriteLineAsync(line);
		writer.Close();
		tempList = null;
	}
	File.Delete(file);
	return i;
}

string HexFix(string line) {
	line = line.Replace("$HEX[", "").Replace("]", "");
	return string.Join("", Enumerable.Range(0, line.Length)
		.Where(x => x % 2 == 0)
		.Select(x => (char)int.Parse(line.Substring(x, 2),
		System.Globalization.NumberStyles.HexNumber)));
}

async Task<HashSet<string>> DNSCheck(string path, float linesCount) {
	Regex domainCheck = domainRegex();
    HashSet<string> goodDNS = WriteToHash(files[3]);
    ConcurrentBag<string> badDNS = new(WriteToHash(files[0]));
    ConcurrentBag<string> domains = new();
	string? line;
	int i = 0, j = 0;
	Console.Title = $"[2.1] 0% | Filtering DNS addresses.";
	using StreamReader reader = new(path);
	while ((line = reader.ReadLine()) != null) {
		Match match = domainCheck.Match(line);
        i++;
        if (match.Success && !goodDNS.Contains(match.Groups[1].Value.ToLower())) {
			string domain = match.Groups[1].Value.ToLower();
			if (!domains.Contains(domain) && !badDNS.Contains(domain) && !tempDomains.Contains(domain)) {
				domains.Add(domain);
				++j;
			}
            if (i % (linesCount / 100) == 0)
                Console.Title = $"[2.1] {i / (linesCount / 100)}% | Filtering DNS addresses.";
		}
	}
	reader.Close();
	goodDNS = null;
	i = 0;
	WriteColorized("[2.1] ", ConsoleColor.Green);
	Console.Write($"Done!");
	if (domains.IsEmpty) {
        Console.WriteLine(" No new DNS addresses found.");
        return badDNS.ToHashSet();
    }
	Console.Title = "[2.2] | Sorting DNS addresses.";
	WriteColorized("\n[2.2] ", ConsoleColor.DarkYellow);
	Console.WriteLine("Sorting DNS addresses.");
	List<List<string>> chunks = domains.ToList()
        .Select((domain, index) => new { domain, index })
		.GroupBy(x => x.index / 40)
		.Select(g => g.Select(x => x.domain).ToList())
		.ToList();
	domains = null;
	linesCount = chunks.Count * 40;
	WriteColorized("[2.2] ", ConsoleColor.Green);
	Console.WriteLine($"Done! There is {linesCount} domains to check.");
	WriteColorized("[2.3] ", ConsoleColor.DarkYellow);
	Console.WriteLine("Checking DNS's TXT of domains. Please wait...");
    Console.Title = $"[2.3] 0% | Checking DNS's TXT of domains.";
    int badDNSCount = 0, goodDNSCount = 0;
	List<Task> tasks = new();
	StringBuilder badDNSBuilder = new(capacity: 10000000);
	StringBuilder goodDNSBuilder = new(capacity: 10000000);
	foreach (var chunk in chunks)
		tasks.Add(Task.Run(async () => {
			foreach (string domain in chunk) {
				Interlocked.Increment(ref i);
				try {
					CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(2));
					var txtRecords = await Dns.GetHostEntryAsync(domain, tokenSource.Token);
					if (txtRecords.AddressList.Length == 0) {
						badDNSBuilder.AppendLine(domain);
						badDNS.Add(domain);
                        Interlocked.Increment(ref badDNSCount);
					}
					else {
						goodDNSBuilder.AppendLine(domain);
						Interlocked.Increment(ref goodDNSCount);
					}
				}
				catch (Exception) {
					badDNSBuilder.AppendLine(domain);
                    badDNS.Add(domain);
                    Interlocked.Increment(ref badDNSCount);
				}
			}
			if (i % (linesCount / 100) == 0)
				Console.Title = $"[2.3] {i / (linesCount / 100)}% | Checking DNS's TXT of domains.";
		}));
	await Task.WhenAll(tasks);
	File.AppendAllText("BadDNS", badDNSBuilder.ToString());
	File.AppendAllText("GoodDNS", goodDNSBuilder.ToString());
	badDNSBuilder = null;
	goodDNSBuilder = null;
	WriteColorized("[2.3] ", ConsoleColor.Green);
	Console.WriteLine($"Done! There is new {badDNSCount} bad DNS's and {goodDNSCount} good DNS's.");
	return badDNS.ToHashSet();
}

string ZoneFix(string line) {
	string[] lineSplit = line.Split('.');
	string line1 = $".{string.Join('.', lineSplit, 1, lineSplit.Length - 1)}";
	foreach (string zone in fixZones) {
		string[] zoneSplit = zone.Split('=');
		if (Regex.IsMatch(line1, zoneSplit[0])) {
			line1 = line1.Replace(line1, zoneSplit[1]);
			break;
		}
	}
	return line1;
}

string DomainFix(string line) {
	string subDomain = line[..line.IndexOf('.')];
	foreach (string domain in fixDomains) {
		string[] parts = domain.Split('=');
		if (line.Contains(parts[0])) {
			line = line.Replace(subDomain + ".", parts[1]);
			break;
		}
	}
	return subDomain + ZoneFix(line);
}

string ProcessLine(string line, HashSet<string> badDNS, char splitter) {
	string[] mailPass = line.Split(splitter);

	//PASS CHECK

	if (config.removeEmptyPass && mailPass[1] == "")
		return $"#EmptyPass#";

	string[] loginDomain = mailPass[0].ToLower().Split("@");

	//EMAIL CHECK

	if (config.removeXumer && line.Contains("xrum"))
		return $"#Xrumer#";

	if (config.removeEqualLoginPass &&
		mailPass[1] == loginDomain[0] ||
		mailPass[1] == loginDomain[1] ||
		mailPass[1] == mailPass[0] ||
		mailPass[1] == $"{loginDomain[0]}@{loginDomain[1].Split('.')[0]}")
		return $"#PassIsLogin#";

	if (config.removeXXXX &&
		gmails.Contains(loginDomain[1]) && loginDomain[0].Contains("xxxx"))
		return $"#GoogleXXXX#";

	if (config.removeTempMail && tempDomains.Contains(loginDomain[1]))
		return $"#TempMail#";

	//DOMAIN FIX

	if (config.checkDNS && badDNS.Contains(loginDomain[1]))
		return $"#BadDNS#";

	//ALL LINE FIX

	if (htmlencodeRegex().Matches(line).Count > 0)
		line = WebUtility.HtmlDecode(line);

	//LOGIN FIX

	if (config.fixPlus && loginDomain[0].Contains('+'))
		loginDomain[0] = loginDomain[0].Split('+')[0];

	if (config.fixDotsGmail && gmails.Contains(loginDomain[1]) && loginDomain[0].Contains('.'))
		loginDomain[0] = loginDomain[0].Replace(".", "");

	if (config.fixDotsYandex && yandexs.Contains(loginDomain[1]) && loginDomain[0].Contains('.'))
		loginDomain[0] = loginDomain[0].Replace(".", "-");

	//PASS FIX

	if (mailPass[1].Contains("{slash}") || mailPass[1].Contains("{eq}"))
		mailPass[1] = mailPass[1].Replace("{slash}", "/").Replace("{eq}", "=");

	if (mailPass[1].Contains("$HEX["))
		mailPass[1] = HexFix(mailPass[1]);
	
	string result = $"{loginDomain[0]}@{loginDomain[1]}:{mailPass[1]}";
	if (mailPass.Length > 2)
		foreach (string piece in mailPass.Skip(2))
			result += $":{piece}";

	return result;
}

async Task MainWork(string path) {
	string? line, result, fileName = Path.GetFileNameWithoutExtension(path);
	int i = 0, shitCount = 0, goodCount = 0;
	float lines = GetLinesCount(path);
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
	WriteColorized("\n[0] ", ConsoleColor.Red);
	Console.WriteLine($"Working with file \"{fileName}\"");
	WriteColorized("[1] ", ConsoleColor.DarkYellow);
	Console.WriteLine("Fixing domains and writing to temp file.");
	Stopwatch stopWatch = new();
	stopWatch.Start();
	StringBuilder tempGood = new(), tempBad = new();
	using (FileStream reader = new(path, FileMode.Open)) {
        using StreamReader streamReader = new(reader);
        while (!streamReader.EndOfStream) {
            line = streamReader.ReadLine();
            if (i % (lines / 100) == 0)
                Console.Title = $"[1] {i / (lines / 100)}% | Fixing domains.";
            try {
                if (loginpassRegex().IsMatch(line)) {
					char splitter = ':';
					if (line.Split(';').Length >= 2)
						splitter = ';';
                    string templine = $"{line.Split(splitter)[0].Split('@')[0]}@{DomainFix(line.Split(splitter)[0].Split('@')[1])}";
                    if (line.Split(splitter).Length >= 2)
                        foreach (string piece in line.Split(splitter).Skip(1))
                            templine += $":{piece}";
                    tempGood.AppendLine(templine);
                }
                else
                    tempBad.AppendLine($"#BadSyntax# {line}");
                if (tempGood.Length >= 20480)
                    using (FileStream stream = new(allFiles[4], FileMode.Append))
                    using (StreamWriter writer = new(stream)) {
                        writer.Write(tempGood.ToString());
                        tempGood.Clear();
                    }
                if (tempBad.Length >= 20480)
                    using (FileStream stream = new(allFiles[0], FileMode.Append))
                    using (StreamWriter writer = new(stream)) {
                        writer.Write(tempBad.ToString());
                        tempBad.Clear();
                    }
            }
            catch { }
            i++;
        }
		streamReader.Close();
        if (tempGood.Length > 0)
            await File.AppendAllTextAsync(allFiles[4], tempGood.ToString());
        if (tempBad.Length > 0)
            await File.AppendAllTextAsync(allFiles[0], tempBad.ToString());
    }
    tempGood.Clear();
    tempBad.Clear();
    if (!File.Exists(allFiles[4])) {
		Console.Title = "Idle.";
		Console.WriteLine($"No good mail:pass(:other) lines found!");
		return;
	}
	Console.Title = $"[1] {i / (lines / 100)}% | Fixing domains.";
	i = 0;
	lines = GetLinesCount(allFiles[4]);
	WriteColorized("[1] ", ConsoleColor.Green);
	Console.WriteLine("Done!");
	WriteColorized("[2.1] ", ConsoleColor.DarkYellow);
	Console.WriteLine("Checking DNS addresses.");
	HashSet<string> badDNS = await DNSCheck(allFiles[4], lines);
	WriteColorized("[3] ", ConsoleColor.DarkYellow);
	Console.WriteLine("Cleaning the base.");
	Console.Title = "0% | Cleaning the base.";
    using StreamReader reader1 = new(allFiles[4]);
	while ((line = reader1.ReadLine()) != null) {
		i++;
		if (i % (lines / 100) == 0)
			Console.Title = $"[3] {i / (lines / 100)}% | Cleaning the base.";
		try {
            char splitter = ':';
            if (line.Split(';').Length >= 2)
                splitter = ';';
            result = ProcessLine(line, badDNS, splitter);
			if (Regex.IsMatch(result, @"^#\w+#$")) {
				tempBad.AppendLine($"{result} {line}");
				shitCount++;
			}
			else {
				tempGood.AppendLine($"{result}");
				goodCount++;
			}
			if (tempBad.Length >= 20480) {
				File.AppendAllLines(allFiles[0], tempBad.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
				tempBad.Clear();
			}
			if (tempGood.Length >= 20480) {
				File.AppendAllLines(allFiles[1], tempGood.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
				tempGood.Clear();
			}
		}
		catch { }
	}
	reader1.Close();
	if (tempBad.Length > 0) {
		File.AppendAllLines(allFiles[0], tempBad.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
		tempBad.Clear();
	}
	if (tempGood.Length > 0) {
		File.AppendAllLines(allFiles[1], tempGood.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
		tempGood.Clear();
	}
	WriteColorized("[3] ", ConsoleColor.Green);
	Console.WriteLine($"Done! There is {shitCount} bad lines and {goodCount} good lines.");
	WriteColorized("[4] ", ConsoleColor.DarkYellow);
	Console.WriteLine("Making result files.");
	Console.Title = "[4] Making result files.";
	File.Delete(allFiles[4]);
	await TempToTxtAsync(allFiles[1]);
	await TempToTxtAsync(allFiles[0]);
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
		else
			break;
	}
	line = "Done! Elapsed";
	if (hours > 0) line += $" {hours}h";
	if (minutes > 0) line += $" {minutes}m";
	if (seconds > 0) line += $" {Math.Round(seconds, 2)}s";
	WriteColorized("[4] ", ConsoleColor.Green);
	Console.WriteLine($"{line}.\r\n");
	Console.Title = "Idle.";
}

/*int upd = AddContextMenuItem();
if (upd == 1)
	Console.WriteLine("Successfully updated app in context menu!");
if (upd == 2)
	Console.WriteLine("Successfully installed app to context menu!");

string fileArgument = "";
if (args.Length > 0) {
	fileArgument = args[0];
	Console.WriteLine(fileArgument);
}*/
while (true) {
	Console.Title = "Idle.";
	Console.WriteLine("Drop a file here: ");
	string? path = Console.ReadLine().Replace("\"", "");
	Console.Clear();
	if (pathRegex().IsMatch(path) && File.Exists(path))
		MainWork(path).Wait();
	else {
		Console.WriteLine("Drop valid .txt file that exists.");
		continue;
	}
	Console.Write("Do you want to exit? ( ");
	WriteColorized("y", ConsoleColor.Green);
	Console.Write(" / ");
	WriteColorized("n", ConsoleColor.Red);
	Console.WriteLine(" ):");
	path = Console.ReadLine();
	if (path == "y" || path == "Y") break;
	else Console.Clear();
}

partial class Program {
	[GeneratedRegex(@"^[A-Za-z]:(?:\\[^\\\/:*?""<>\|]+)*\\[^\\\/:*?""<>\|]+\.txt$")]
	private static partial Regex pathRegex();

	[GeneratedRegex(@"^[A-Za-z\d][\w.+-]*@([A-Za-z\d][A-Za-z\d-]*\.)+[A-Za-z][A-Za-z\d]{1,10}:.*?$")]
	private static partial Regex loginpassRegex();

	[GeneratedRegex(@"&[A-Za-z\d]{2,6};")]
	private static partial Regex htmlencodeRegex();

	[GeneratedRegex(@"@([\w-]+(?:\.[\w-]+)+):")]
	private static partial Regex domainRegex();
}

class Config {
	public bool checkDNS = true;
	public bool removeXXXX = true;
	public bool removeEmptyPass = true;
	public bool removeXumer = true;
	public bool removeEqualLoginPass = true;
	public bool removeTempMail = true;
	public bool fixDotsGmail = true;
	public bool fixDotsYandex = true;
	public bool fixPlus = true;
}