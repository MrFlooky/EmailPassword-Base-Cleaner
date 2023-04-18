using fileutils;
using lineutils;
using config;
using System.Text;
using System.Text.RegularExpressions;
using System;

namespace contextfunctions {
	internal class ContextFunctions {
		public static string input = "";

		public static void DeleteDuplicates() {
			Console.WriteLine("Deleting duplicates.");
			FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_noDup.txt", FileUtils.WriteToHashSet(input), false);
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void DeleteDuplicateEmails() {
			Console.WriteLine("Deleting duplicated mails.");
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			HashSet<string> uniqueEmails = new(), duplicateEmails = new(), nonDuplicateLines = new();
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				string email;
				if (splitter == '@')
					email = line;
				else if (splitter != '\0')
					email = line.Split(splitter)[0];
				else continue;
				if (!uniqueEmails.Add(email))
					duplicateEmails.Add(email);
			}
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				string email;
				if (splitter == '@')
					email = line;
				else if (splitter != '\0')
					email = line.Split(splitter)[0];
				else continue;
				if (!duplicateEmails.Contains(email))
					nonDuplicateLines.Add(line);
			}
			if (nonDuplicateLines.Count > 0) {
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_noDupMails.txt", nonDuplicateLines, false);
				Console.WriteLine("Done!");
			}
			else Console.WriteLine("There is all emails are duplicated ;(");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void DeleteDuplicatePasswords() {
			Console.WriteLine("Deleting duplicated passwords.");
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			HashSet<string> uniquePass = new(), duplicatePass = new(), nonDuplicateLines = new();
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				string pass;
				if (splitter != '\0' && splitter != '@')
					pass = line.Split(splitter)[1];
				else continue;
				if (!uniquePass.Add(pass))
					duplicatePass.Add(pass);
			}
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				string pass;
				if (splitter != '\0' && splitter != '@')
					pass = line.Split(splitter)[1];
				else continue;
				if (!duplicatePass.Contains(pass))
					nonDuplicateLines.Add(line);
			}
			if (nonDuplicateLines.Count > 0) {
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_noDupPass.txt", nonDuplicateLines, false);
				Console.WriteLine("Done!");
			}
			else Console.WriteLine("There is all passwords are duplicated ;(");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void MailPassToLoginPass() {
			Console.WriteLine("Converting mail:pass to login:pass.");
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			HashSet<string> loginpass = new();
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				if (splitter == '@' || splitter == '\0') continue;
				string[] mp = line.Split(splitter);
				if (Config.mailRegex.IsMatch(mp[0]))
					loginpass.Add($"{mp[0].Split('@')[0]}:{mp[1]}");
			}
			if (loginpass.Count > 0) {
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_LoginPass.txt", loginpass, false);
				Console.WriteLine("Done!");
			}
			else Console.WriteLine("There is no valid mail:pass lines ;(");
			Console.ReadLine();
			Environment.Exit(0);
		}
		
		public static void RandomizeLines() {
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			List<string> list = new(lines);
			Random random = new();
			int n = list.Count;
			while (n > 1) {
				n--;
				int k = random.Next(n + 1);
				(list[n], list[k]) = (list[k], list[n]);
			}
			FileUtils.WriteListToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_randomized.txt", list, false);
			Console.WriteLine("Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void GetPasswords() {
			Console.WriteLine("Getting all passwords.");
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			HashSet<string> passwords = new();
			foreach (string line in lines) {
				char splitter = LineUtils.GetSplitter(line);
				if (splitter == '@' || splitter == '\0') continue;
				string[] mp = line.Split(splitter);
				passwords.Add(mp[1]);
			}
			if (passwords.Count > 0) {
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_passwords.txt", passwords, false);
				Console.WriteLine("Done!");
			}
			else Console.WriteLine("There is no passwords ;(");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void GetMails() {
			Console.WriteLine("Getting all mails.");
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			HashSet<string> mails = new();
			foreach (string line in lines) {
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
			if (mails.Count > 0) {
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_mails.txt", mails, false);
				Console.WriteLine("Done!");
			}
			else Console.WriteLine("There is no mails ;(");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void GetLogins() {
			Console.WriteLine("Getting all logins.");
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			HashSet<string> logins = new();
			foreach (string line in lines) {
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
			if (logins.Count > 0) {
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_logins.txt", logins, false);
				Console.WriteLine("Done!");
			}
			else Console.WriteLine("There is no logins ;(");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void GetDomains() {
			Console.WriteLine("Getting all domains.");
			HashSet<string> lines = FileUtils.WriteToHashSet(input);
			HashSet<string> domains = new();
			foreach (string line in lines) {
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
			if (domains.Count > 0) {
				FileUtils.WriteHashsetToFile($"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_domains.txt", domains, false);
				Console.WriteLine("Done!");
			}
			else Console.WriteLine("There is no domains ;(");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void SplitByLines() {
			Console.WriteLine("Split base by lines.");
			double amount_input = FileUtils.GetLinesCount(input), amount;
			string amount_temp;
			while (true) {
				Console.WriteLine("What amount of lines must be in each file?");
				amount_temp = Console.ReadLine();
				if (Regex.IsMatch(amount_temp, @"^\d+$")) {
					amount = Convert.ToInt32(amount_temp);
					if (amount < amount_input)
						break;
					else Console.WriteLine("Bad amount / or amount larger than amount of lines in input file.");
				}
				else Console.WriteLine("Bad amount / or amount larger than amount of lines in input file.");
			}
			int fileCount = 1;
			string output = $"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_splitby_[{amount}]_[{fileCount}].txt";
			using (StreamReader reader = new(input)) {
				int lineCount = 0;
				StreamWriter writer = null;
				string line;
				while ((line = reader.ReadLine()) != null) {
					if (writer == null || lineCount >= amount) {
						writer?.Dispose();
						writer = new StreamWriter(output);
						fileCount++;
						lineCount = 0;
					}
					writer.WriteLine(line);
					lineCount++;
				}
				writer?.Dispose();
			}
			Console.WriteLine($"Done! There is new {fileCount -= 1} files.");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void SplitBySize() {
			Console.WriteLine("Split base by lines.");
			string amount_temp;
			FileInfo fileInfo = new(input);
			long fileSize = fileInfo.Length, amount;
			while (true) {
				Console.WriteLine("What size must be each file? For example, 512K, 2M");
				amount_temp = Console.ReadLine();
				if (Regex.IsMatch(amount_temp, @"^\d+[KM]$")) {
					amount = LineUtils.GetMaxFileSize(amount_temp);
					if (amount < fileSize)
						break;
					else Console.WriteLine("Bad size / or size larger than size of input file.");
				}
				else Console.WriteLine("Bad size / or size larger than size of input file.");
			}
			int fileCount = 1;
			string output = $"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_splitbysize_[{amount_temp}]_[{fileCount}].txt";
			using (StreamReader reader = new(input)) {
				fileSize = 0;
				StreamWriter writer = null;
				string line;
				while ((line = reader.ReadLine()) != null) {
					if (writer == null || fileSize >= amount) {
						writer?.Dispose();
						writer = new StreamWriter(output);
						fileCount++;
						fileSize = 0;
					}
					writer.WriteLine(line);
					fileSize += Encoding.UTF8.GetByteCount(line + Environment.NewLine);
				}
				writer?.Dispose();
			}
			Console.WriteLine($"Done! There is new {fileCount -= 1} files.");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void ReplaceSplitter() {
			Console.WriteLine("Replace splitter from ; to :.");
			string output = $"{Path.GetDirectoryName(input)}\\{Path.GetFileNameWithoutExtension(input)}_replacedSplitter.txt";
			using (StreamWriter writer = new(output))
				foreach (string line in File.ReadLines(input)) {
					string result = line;
					int index1 = result.IndexOf(';');
					int index2 = result.IndexOf(':');
					if (index1 >= 0 && (index1 < index2 || index2 < 0))
						result = result.Substring(0, index1) + ':' + result.Substring(index1 + 1);

					writer.WriteLine(result);
				}
			Console.WriteLine($"Done!");
			Console.ReadLine();
			Environment.Exit(0);
		}
	}
}