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

string? tmpline;
HashSet<string> fixdomains = WriteToHash(files[1]);
HashSet<string> fixzones = WriteToHash(files[2]);
HashSet<string> tempdomains = WriteToHash(files[4]);

HashSet<string> WriteToHash(string filename) {
    HashSet<string> list = new(GetLinesCount(filename));
    using (StreamReader reader = new(filename))
        while ((tmpline = reader.ReadLine()) != null)
            list.Add(tmpline);
    return list;
}

bool SetPartConfig(string msg) {
    string temp;
    bool tempb;
    while (true) {
        Console.WriteLine(msg);
        temp = Console.ReadLine();
        if (Regex.IsMatch(temp, "^[yn]$")) {
            tempb = temp == "y";
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

async Task<HashSet<string>> DNSCheck(string path, int lines_c) {
    Regex domainCheck = domainRegex();
    HashSet<string> baddns = WriteToHash(files[0]);
    HashSet<string> gooddns = WriteToHash(files[3]);
    HashSet<string> domains = new();
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

string ZoneFix(string line) {
    string[] lineSplit = line.Split('.');
    string line1 = "." + string.Join(".", lineSplit, 1, lineSplit.Length - 1);
    foreach (string zone in fixzones) {
        string[] zoneSplit = zone.Split('=');
        if (Regex.IsMatch(line1, zoneSplit[0])) {
            line1 = line1.Replace(line1, zoneSplit[1]);
            break;
        }
    }
    return line1;
}

string DomainFix(string line) {
    int dotIndex = line.IndexOf('.');
    string subdomain = line[..dotIndex];
    foreach (string domain in fixdomains) {
        string[] parts = domain.Split('=');
        if (line.Contains(parts[0])) {
            line = line.Replace(subdomain + ".", parts[1]);
            break;
        }
    }
    return subdomain + ZoneFix(line);
}

string ProcessLine(string line, HashSet<string> baddns) {
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
    string[] allFiles = new string[5] { $"./Results/{filename}_shit.tmp",
        $"./Results/{filename}_good.tmp", $"./Results/{filename}_shit.txt",
        $"./Results/{filename}_shit.txt", $"./Results/{filename}.txt" };
    if (!Directory.Exists("./Results"))
        Directory.CreateDirectory("./Results");
    foreach (string tmpFile in allFiles)
        if (File.Exists(tmpFile))
            File.Delete(tmpFile);
    Console.Clear();
    Console.Title = $"0% | {i} / {lines} | Fixing domains and writing to temp file";
    Console.WriteLine($"[1] Fixing domains and writing to temp file.");

    using (FileStream reader = new(path, FileMode.Open)) {
        using (StreamReader streamReader = new(reader)) {
            StringBuilder temp = new();
            StringBuilder tempbad = new();
            while (!streamReader.EndOfStream) {
                line = streamReader.ReadLine();
                if ((float)i / lines * 1000000 % 2 == 0)
                    Console.Title = $"{Math.Round((float)i / lines * 100, 0)}% | {i} / {lines} | Fixing domains and writing to temp file";
                try {
                    if (loginpassRegex().IsMatch(line)) {
                        string templine = $"{line.Split(':')[0].Split('@')[0]}@{DomainFix(line.Split(':')[0].Split('@')[1])}";
                        if (line.Split(':').Length >= 2)
                            foreach (string piece in line.Split(':').Skip(1))
                                templine += $":{piece}";
                        temp.AppendLine(templine);
                    }
                    else
                        tempbad.AppendLine($"#BadSyntax# {line}");
                    if (temp.Length >= 20480)
                        using (FileStream stream = new(allFiles[4], FileMode.Append))
                        using (StreamWriter writer = new(stream)) {
                            writer.Write(temp.ToString());
                            temp.Clear();
                        }
                    if (tempbad.Length >= 20480)
                        using (FileStream stream = new(allFiles[0], FileMode.Append))
                        using (StreamWriter writer = new(stream)) {
                            writer.Write(tempbad.ToString());
                            tempbad.Clear();
                        }
                }
                catch { }
                i++;
            }
            if (temp.Length > 0)
                using (FileStream stream = new(allFiles[4], FileMode.Append))
                using (StreamWriter writer = new(stream))
                    writer.Write(temp.ToString());
            if (tempbad.Length > 0)
                using (FileStream stream = new(allFiles[0], FileMode.Append))
                using (StreamWriter writer = new(stream))
                    writer.Write(tempbad.ToString());
        }
    }

    if (!File.Exists(allFiles[4])) {
        Console.Title = $"Idle.";
        Console.WriteLine("No good mail:pass(:other) lines found!");
        return;
    }
    Console.Title = $"{Math.Round((float)i / lines * 100, 0)}% | {i} / {lines} | Fixing domains and writing to temp file";
    i = 0;
    lines = GetLinesCount(allFiles[4]);
    Console.WriteLine($"[2.1] Checking DNS addresses.");
    HashSet<string> baddns = await DNSCheck(allFiles[4], lines);
    Console.WriteLine($"[3] Cleaning the base.");
    Console.Title = $"0% | {i} / {lines} | {filename} | Good / Shit: {good_c} / {shit_c}";
    StringBuilder tempBad = new(), tempGood = new();
    using StreamReader reader1 = new(allFiles[4]);
    while ((line = reader1.ReadLine()) != null) {
        i++;
        if ((float)i / lines * 1000000 % 10 == 0)
            Console.Title = $"{Math.Round((float)i / lines * 100, 0)}% | {i} / {lines} | {filename} | Good / Shit: {good_c} / {shit_c}";
        try {
            result = ProcessLine(line, baddns);
            if (Regex.IsMatch(result, @"^#\w+#$")) {
                tempBad.AppendLine($"{result} {line}");
                shit_c++;
            }
            else {
                tempGood.AppendLine($"{result}");
                good_c++;
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
    Console.WriteLine($"[4] Making result files.");
    Console.Title = $"Working with {filename}. {i} / {lines} | Good: {good_c} | Shit: {shit_c}";
    File.Delete(allFiles[4]);
    good_c = await TempToTxtAsync(allFiles[1]);
    await TempToTxtAsync(allFiles[0]);
    shit_c = lines - good_c;
    Console.WriteLine("Done!");
    Console.Title = "Idle.";
}

Console.WriteLine("Created by @SilverBulletRU");
bool checkcfg = false;
while (true) {
    Console.Title = "Idle.";
    if (CheckConfig() && !checkcfg) {
        checkcfg = true;
        PromptConfig();
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