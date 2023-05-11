using config;
using fileutils;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace lineutils {

	internal class LineUtils {

		public static string DomainFix(string line) {
			if (!Config.domains.Contains(line) && Config.fixDomainsDictionary.TryGetValue(line, out string value))
				return value;
			return line;
		}

		public static char GetSplitter(string line) {
			if (line.Contains(';'))
				return ';';
			if (line.Contains(':'))
				return ':';
			if (Config.mailRegex.IsMatch(line))
				return '@';
			return '\0';
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

			//ALL LINE FIX

			if (Config.htmlencodeRegex.Matches(line).Count > 0)
				line = WebUtility.HtmlDecode(line);

			string[] gmails = new string[2] { "googlemail.com", "gmail.com" };
			string[] yandexs = new string[5] { "ya.ru", "yandex.com", "yandex.ru", "yandex.by", "yandex.kz" };
			string[] mailPass = line.Split(splitter);
			if (!typeOfLine)
				mailPass = new string[2] { line, "" };

			//PASS CHECK

			if (Config.removeEmptyPass && typeOfLine && mailPass[1] == "")
				return "#EmptyPass#";

			string[] loginDomain = mailPass[0].ToLower().Split('@');
			mailPass[1] = mailPass[1].Replace("№", "");

			//EMAIL CHECK

			if (Config.removeXumer && line.Contains("xrum"))
				return "#Xrumer#";

			if (Config.removeEqualLoginPass && typeOfLine &&
				mailPass[1] == loginDomain[0] ||
				mailPass[1] == loginDomain[1] ||
				mailPass[1] == mailPass[0] ||
				mailPass[1] == $"{loginDomain[0]}@{loginDomain[1].Split('.')[0]}")
				return "#PassIsLogin#";

			if (Config.removeXXXX &&
				gmails.Contains(loginDomain[1]) && loginDomain[0].Contains("xxxx"))
				return "#GoogleXXXX#";

			if (Config.removeTempMail && FileUtils.tempDomains.Contains(loginDomain[1]))
				return "#TempMail#";

			//DOMAIN FIX

			if (Config.checkDNS && badDNS.Contains(loginDomain[1]))
				return "#BadDNS#";

			//LOGIN FIX

			if (Config.fixPlus && loginDomain[0].Contains('+'))
				loginDomain[0] = loginDomain[0].Split('+')[0];

			if (Config.fixDotsGmail && gmails.Contains(loginDomain[1]) && loginDomain[0].Contains('.'))
				loginDomain[0] = loginDomain[0].Replace(".", "");

			if (gmails.Contains(loginDomain[1]) && (loginDomain[0].Length < 6 || loginDomain[0].Length > 30))
				return "#FakeGmail#";

			if (Config.fixDotsYandex && yandexs.Contains(loginDomain[1]) && loginDomain[0].Contains('.'))
				loginDomain[0] = loginDomain[0].Replace('.', '-');

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

		public static string ZoneFix(string line) {
			if (!line.StartsWith('.'))
				line = $".{line}";
			if (!Config.zones.Contains(line))
				foreach (var zone in Config.fixZonesDictionary)
					if (Regex.IsMatch(line, zone.Key))
						return zone.Value;
			return line;
		}

		public static string FixLine(ref string line) {
			char splitter = GetSplitter(line);
			string templine = splitter == ';' ? line.Replace(';', ':') : line;
			if (!Config.fixDomains)
				return templine;
			if (!Config.loginpassRegex.IsMatch(line) && !Config.mailRegex.IsMatch(line) && !Config.loginpassPartialRegex.IsMatch(line))
				return $"#BadSyntax# {templine}";
			string[] splitLine = line.Split(splitter);
			splitLine[0] = splitLine[0].ToLower();
			string[] temp = splitLine[0].Split('@')[1].Split('.');
			string domain = temp[^2];
			string zone = temp[^1];
			templine = $"{splitLine[0].Split('@')[0]}@";
			if (temp.Length >= 3)
				for (int i = 0; i < temp.Length - 2; i++)
					templine += temp[i] + '.';
			string fixedDomain = DomainFix(domain);
			templine += fixedDomain;
			string fixedZone = ".com";
			if (fixedDomain != "gmail")
				fixedZone = ZoneFix(zone);
			templine += fixedZone;
			if (splitter != '@' && splitLine.Length >= 2)
				for (int i = 1; i <= splitLine.Length - 1; i++)
					templine += ':' + splitLine[i];
			return templine;
		}

		public static long GetMaxFileSize(string input) {
			input = input.Replace(" ", "");
			char lastChar = input[^1];
			if (char.IsLetter(lastChar)) {
				input = input[..^1];
				long size = long.Parse(input);
				return lastChar switch {
					'K' => size * 1024,
					'M' => size * 1024 * 1024,
					'G' => size * 1024 * 1024 * 1024,
					_ => throw new ArgumentException("Invalid size suffix"),
				};
			}
			else return long.Parse(input);
		}

		/*public static int StringCompare(string first, string second) {
            int min = Math.Min(first.Length, second.Length);
            int sum = 0;
			for (int i = 0; i < min; ++i)
				if (first[i] != second[i])
					sum++;
            //    sum += second[i] - first[i];
            if (first.Length > min)
                for (int i = min; i < first.Length; ++i)
                    sum++;
            //sum -= first[i];
            if (second.Length > min)
                for (int i = min; i < second.Length; ++i)
                    sum++;
            //sum += second[i];
            return Math.Abs(sum);
        }*/
	}
}