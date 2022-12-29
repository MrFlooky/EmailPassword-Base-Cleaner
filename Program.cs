using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

string[] gmails = new string[2] { "googlemail.com", "gmail.com" };
string[] yandexs = new string[6] { "ya.ru", "yandex.com", "yandex.ru", "yandex.by", "yandex.kz", "yandex.ua" };
string? tmpline;
List<string> tempdomains = new();
using StreamReader reader = new("TempMails");
while ((tmpline = reader.ReadLine()) != null)
	tempdomains.Add(tmpline);
List<string> baddomains = new();
using StreamReader reader1 = new("BadDomain");
while ((tmpline = reader1.ReadLine()) != null)
	baddomains.Add(tmpline);
int linescount(string path) {
	int i = 0;
	using (StreamReader sr = new(path)) {
		while (sr.ReadLine() != null)
			i++;
	}
	return i;
}

async Task temptotxtAsync(string file) {
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
		clean();
		using StreamWriter writer = new(file.Replace(".tmp", ".txt"));
		foreach (string line in tempList)
			await writer.WriteLineAsync(line);
		writer.Close();
		tempList.Clear();
		clean();
	}
	File.Delete(file);
}

void clean() {
	GC.Collect();
	GC.WaitForPendingFinalizers();
}

string hexfix(string line) {
	line = line.Replace("$HEX[", "").Replace("]", "");
	return string.Join("", Enumerable.Range(0, line.Length)
		.Where(x => x % 2 == 0)
		.Select(x => (char)int.Parse(line.Substring(x, 2),
		System.Globalization.NumberStyles.HexNumber)));
}

string ProcessLine(string line) {
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

	if (logindomain[0].Contains("???"))
		return $"#Questions#";

	if (gmails.Contains(logindomain[1]) && logindomain[0].Contains("xxxx"))
		return $"#GoogleXXXX#";

	if (tempdomains.Contains(logindomain[1]))
		return $"#TempMail#";
	
	if (baddomains.Contains(logindomain[1]))
		return $"#BadDomain#";

	//ALL LINE FIX

	if (htmlencodeRegex().Matches(line).Count > 0)
		line = WebUtility.HtmlDecode(line);

	//EMAIL FIX

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
		mailpass[1] = hexfix(mailpass[1]);

	//if (base64Regex().IsMatch(mailpass[1]))
	//	mailpass[1] = Encoding.UTF8.GetString(Convert.FromBase64String(mailpass[1]));

	return $"{logindomain[0]}@{logindomain[1]}:{mailpass[1]}";
}

async Task work(string path) {
	string? line, result, filename = Path.GetFileNameWithoutExtension(path);
	int i = 0, shit_c = 0, good_c = 0, lines = linescount(path);
	if (File.Exists($"{filename}_shit.txt")) File.Delete($"{filename}_shit.txt");
	if (File.Exists($"{filename}_shit.tmp")) File.Delete($"{filename}_shit.tmp");
	if (File.Exists($"{filename}_good.txt")) File.Delete($"{filename}_good.txt");
	if (File.Exists($"{filename}_good.tmp")) File.Delete($"{filename}_good.tmp");
	Console.Clear();
	Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c}");
	using StreamReader reader = new(path);
	while ((line = reader.ReadLine()) != null) {
		if (i % 5000 == 0) {
			clean();
			Console.Clear();
			Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c}");
		}
		result = ProcessLine(line);
		if (shitRegex().IsMatch(result)) {
			File.AppendAllText($"{filename}_shit.tmp", $"{result} {line}\r\n");
			shit_c++;
		}
		else {
			File.AppendAllText($"{filename}_good.tmp", $"{result}\r\n");
			good_c++;
		}
		i++;
	}
	Console.Clear();
	Console.WriteLine($"{i} / {lines}\nGood: {good_c} | Shit: {shit_c}");
	await temptotxtAsync($"{filename}_shit.tmp");
	temptotxtAsync($"{filename}_good.tmp").Wait();
}

while (true) {
	Console.Clear();
	Console.WriteLine("Drop a file here: ");
	string? path = Console.ReadLine();
	Console.Clear();
	if (pathRegex().IsMatch(path) && File.Exists(path)) {
		work(path).Wait();
	}
	else {
		Console.WriteLine("Drop a text file.");
		continue;
	}
	Console.WriteLine("Do you want to exit? ( y / n ): ");
	path = Console.ReadLine();
	if (path == "y" || path == "Y")
		break;
}

partial class Program {
	[GeneratedRegex(@"^[A-Za-z]:(?:\\[^\\\/:*?""<>\|]+)*\\[^\\\/:*?""<>\|]+\.txt$")]
	private static partial Regex pathRegex();
	
	[GeneratedRegex(@"^[\w\.\+-]+@([A-z\d-]+\.)+[A-z\d]{2,11}:.*?$")]
	private static partial Regex loginpassRegex();

	[GeneratedRegex(@"^#\w+#$")]
	private static partial Regex shitRegex();

	[GeneratedRegex(@"^(?:[A-z\d+/]{4})*(?:[A-z\d+/]{2}==|[A-z\d+/]{3}=)?$")]
	private static partial Regex base64Regex();
	
	[GeneratedRegex(@"&[A-z\d]{2,6};")]
	private static partial Regex htmlencodeRegex();
}