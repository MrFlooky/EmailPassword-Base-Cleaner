using fileutils;
using lineutils;
using config;
using System.Text;
using System.Text.RegularExpressions;

namespace contextfunctions {
	internal class ContextFunctions {
		public static string input = "";

		private static void SimpleEnd(double i, string msg) {
			if (i == 0) Console.WriteLine(msg);
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void CleanDups() {
			Console.WriteLine("Deleting duplicates.");
			FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_noDup.txt", FileUtils.WriteToHashSet(input), false);
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void CleanSameMails() {
			Console.WriteLine("Deleting duplicated logins.");
			double i = 0;
			using (StreamWriter sw = new($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_noDupMails.txt", false, Encoding.UTF8, 2072576)) {
				HashSet<string> uniqueEmails = new();
				foreach (string line in File.ReadLines(input)) {
					string login;
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '\0') continue;
					if (splitter == '@') login = line;
					else {
						string mbLogin = line.Split(splitter)[0];
						if (Config.mailRegex.IsMatch(mbLogin) || Config.loginRegex.IsMatch(mbLogin))
							login = mbLogin;
						else continue;
					}
					if (uniqueEmails.Add(login)) {
						sw.WriteLine(line);
						i++;
					}
				}
				uniqueEmails = null;
				sw.Close();
			}
			SimpleEnd(i, "There is all emails are duplicated ;(");
		}

		public static void CleanSamePass() {
			Console.WriteLine("Deleting duplicated passwords.");
			double i = 0;
			using (StreamWriter sw = new($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_noDupPass.txt", false, Encoding.UTF8, 2072576)) {
				HashSet<string> uniquePass = new();
				foreach (string line in File.ReadLines(input)) {
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '\0' || splitter == '@') continue;
					string pass = line.Split(splitter)[1];
					if (uniquePass.Add(pass)) {
						sw.WriteLine(line);
						i++;
					}
				}
				uniquePass = null;
				sw.Close();
			}
			SimpleEnd(i, "There is all passwords are duplicated ;(");
		}

		public static void MPtoLP() {
			Console.WriteLine("Converting mail:pass to login:pass.");
			double i = 0;
			using (StreamWriter sw = new($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_LoginPass.txt", false, Encoding.UTF8, 2072576)) {
				HashSet<string> uniqueLines = new();
				foreach (string line in File.ReadLines(input)) {
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '@' || splitter == '\0') continue;
					string[] mp = line.Split(splitter);
					if (Config.mailRegex.IsMatch(mp[0])) {
						string outputLine = $"{mp[0].Split('@')[0]}:{mp[1]}";
						if (uniqueLines.Add(outputLine)) {
							sw.WriteLine(outputLine);
							i++;
						}
					}
				}
				uniqueLines = null;
				sw.Close();
			}
			SimpleEnd(i, "There is no valid mail:pass lines ;(");
		}

		public static void Randomize() {
			Console.WriteLine("Randomizing lines.");
			List<string> list = new(File.ReadLines(input));
			Random random = new();
			int n = list.Count;
			while (n > 1) {
				n--;
				int k = random.Next(n + 1);
				(list[n], list[k]) = (list[k], list[n]);
			}
			using (StreamWriter sw = new($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_randomized.txt", false)) {
				StringBuilder sb = new();
				foreach (string line in list)
					sb.AppendLine(line);
				sw.Write(sb.ToString());
				sb.Clear();
				sw.Close();
			}
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void GetPasswords() {
			Console.WriteLine("Getting all passwords.");
			double i = 0;
			using (StreamWriter sw = new($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_passwords.txt", false, Encoding.UTF8, 2072576)) {
				using StreamReader reader = new(input);
				HashSet<string> passwords = new();
				string line;
				while ((line = reader.ReadLine()) != null) {
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '@' || splitter == '\0') continue;
					string[] mp = line.Split(splitter);
					if (passwords.Add(mp[1])) {
						sw.WriteLine(mp[1]);
						i++;
					}
				}
				passwords = null;
				reader.Close();
				sw.Close();
			}
			SimpleEnd(i, "There is no passwords ;(");
		}

		public static void GetMails() {
			Console.WriteLine("Getting all mails.");
			HashSet<string> mails = new();
			using (StreamReader reader = new(input)) {
				string line;
				while ((line = reader.ReadLine()) != null) {
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '\0') continue;
					if (splitter == '@' && Config.mailRegex.IsMatch(line)) {
						mails.Add(line);
						continue;
					}
					string[] mp = line.Split(splitter);
					if (Config.mailRegex.IsMatch(mp[0]))
						mails.Add(mp[0]);
				}
				reader.Close();
			}
			if (mails.Count > 0) 
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_mails.txt", mails, false);
			else Console.WriteLine("There is no mails ;(");
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void GetLogins() {
			Console.WriteLine("Getting all logins.");
			HashSet<string> logins = new();
			using (StreamReader reader = new(input)) {
				string line;
				while ((line = reader.ReadLine()) != null) {
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '\0') continue;
					if (splitter == '@' && Config.mailRegex.IsMatch(line)) {
						logins.Add(line.Split('@')[0]);
						continue;
					}
					string[] mp = line.Split(splitter);
					if (Config.mailRegex.IsMatch(mp[0]))
						logins.Add(mp[0].Split('@')[0]);
				}
				reader.Close();
			}
			if (logins.Count > 0) 
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_logins.txt", logins, false);
			else Console.WriteLine("There is no logins ;(");
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void GetDomains() {
			Console.WriteLine("Getting all domains.");
			HashSet<string> domains = new();
			using (StreamReader reader = new(input)) {
				string line;
				while ((line = reader.ReadLine()) != null) {
					char splitter = LineUtils.GetSplitter(line);
					if (splitter == '\0') continue;
					if (splitter == '@' && Config.mailRegex.IsMatch(line)) {
						domains.Add(line.Split('@')[1]);
						continue;
					}
					string[] mp = line.Split(splitter);
					if (Config.mailRegex.IsMatch(mp[0]))
						domains.Add(mp[0].Split('@')[1]);
				}
				reader.Close();
			}
			if (domains.Count > 0) 
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_domains.txt", domains, false);
			else Console.WriteLine("There is no domains ;(");
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void SplitLines() {
			Console.WriteLine("Split base by lines.");
			double amount_input = FileUtils.GetLinesCount(input), amount;
			while (true) {
				Console.WriteLine("What amount of lines must be in each file?");
				string amount_temp = Console.ReadLine();
				if (Regex.IsMatch(amount_temp, @"^\d+$")) {
					amount = Convert.ToInt32(amount_temp);
					if (amount < amount_input) break;
					else Console.WriteLine("Bad amount / or amount larger than amount of lines in input file.");
				}
				else Console.WriteLine("Bad amount / or amount larger than amount of lines in input file.");
			}
			int fileCount = 1;
			using (StreamReader reader = new(input)) {
				int lineCount = 0;
				StreamWriter writer = null;
				string line;
				while ((line = reader.ReadLine()) != null) {
					if (writer == null || lineCount >= amount) {
						writer?.Dispose();
						string output = $"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_splitby_[{amount}]_[{fileCount}].txt";
						writer = new StreamWriter(output);
						fileCount++;
						lineCount = 0;
					}
					writer.WriteLine(line);
					lineCount++;
				}
				writer?.Dispose();
				writer.Close();
				reader.Close();
			}
			Console.WriteLine($"Done! There is new {fileCount -= 1} files.");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void SplitSize() {
			Console.WriteLine("Split base by lines.");
			string amount_temp;
			FileInfo fileInfo = new(input);
			long fileSize = fileInfo.Length, amount;
			while (true) {
				Console.WriteLine("What size must be each file? For example, 512K, 2M");
				amount_temp = Console.ReadLine();
				if (Regex.IsMatch(amount_temp, @"^\d+[KM]$")) {
					amount = LineUtils.GetMaxFileSize(amount_temp);
					if (amount < fileSize) break;
					else Console.WriteLine("Bad size / or size larger than size of input file.");
				}
				else Console.WriteLine("Bad size / or size larger than size of input file.");
			}
			int fileCount = 1;
			using (StreamReader reader = new(input)) {
				fileSize = 0;
				StreamWriter writer = null;
				string line;
				while ((line = reader.ReadLine()) != null) {
					if (writer == null || fileSize >= amount) {
						writer?.Dispose();
						string output = $"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_splitbysize_[{amount_temp}]_[{fileCount}].txt";
						writer = new StreamWriter(output);
						fileCount++;
						fileSize = 0;
					}
					writer.WriteLine(line);
					fileSize += Encoding.UTF8.GetByteCount(line + Environment.NewLine);
				}
				writer?.Dispose();
				writer.Close();
				reader.Close();
			}
			Console.WriteLine($"Done! There is new {fileCount -= 1} files.");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void ReplaceDelimiter() {
			Console.WriteLine("Replace delimiter from ; to :.");
			using (StreamWriter writer = new($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_replacedSplitter.txt")) {
				foreach (string line in File.ReadLines(input)) {
					string result = line;
					int index1 = result.IndexOf(';');
					int index2 = result.IndexOf(':');
					if (index1 >= 0 && (index1 < index2 || index2 < 0))
						result = result.Substring(0, index1) + ':' + result.Substring(index1 + 1);
					writer.WriteLine(result);
				}
				writer?.Dispose();
				writer.Close();
			}
			Console.WriteLine($"Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}
	}
}