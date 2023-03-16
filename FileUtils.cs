using config;
using lineutils;
using System.Text;

namespace fileutils {

    public class FileUtils {
        public static readonly string[] files = new string[5] { "BadDNS", "FixDomains", "FixZone", "GoodDNS", "TempMails" };
        public static readonly HashSet<string> tempDomains = WriteToHashSet(files[4]);

        public static string FixLines(string line) {
            char splitter = LineUtils.GetSplitter(line);
            string templine = splitter == ';' ? line.Replace(';', ':') : line;
            if (Config.fixDomains) {
                templine = $"{line.Split(splitter)[0].Split('@')[0]}@{LineUtils.DomainFix(line.Split(splitter)[0].Split('@')[1])}";
                if (splitter != '@' && line.Split(splitter).Length >= 2)
                    foreach (string piece in line.Split(splitter).Skip(1))
                        templine += $":{piece}";
            }
            return templine;
        }

        public static long GetLinesCount(string path) {
            long i = 0;
            using (StreamReader sr = new(path))
                while (sr.ReadLine() != null)
                    i += 1;
            return i;
        }

        public static async Task TempToTxtAsync(string file) {
            if (!File.Exists(file) || !file.EndsWith(".tmp")) return;
            using (StreamReader reader = new(file)) {
                Dictionary<string, bool> tempList = new();
                string line;
                while ((line = await reader.ReadLineAsync()) is not null)
                    if (!tempList.ContainsKey(line))
                        tempList[line] = true;
                StringBuilder stringBuilder = new();
                foreach (string key in tempList.Keys.OrderBy(x => x))
                    stringBuilder.AppendLine(key);
                using StreamWriter writer = new(file.Replace(".tmp", ".txt"));
                await writer.WriteAsync(stringBuilder.ToString());
                tempList = null;
            }
            File.Delete(file);
        }

        public static HashSet<string> WriteToHashSet(string fileName) {
            string? tmpLine;
            var list = new HashSet<string>();
            using (StreamReader reader = new(fileName))
                while ((tmpLine = reader.ReadLine()) != null)
                    list.Add(tmpLine);
            return list;
        }
    }
}