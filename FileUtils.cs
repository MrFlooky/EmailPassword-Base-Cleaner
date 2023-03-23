using config;
using lineutils;

namespace fileutils {

    public class FileUtils {
        public static readonly string[] files = new string[5] { "BadDNS", "FixDomains", "FixZone", "GoodDNS", "TempMails" };
        public static readonly HashSet<string> tempDomains = WriteToHashSet(files[4]);

        public static string FixLines(string line, Dictionary<string, string> fixDomains, HashSet<string> domains, Dictionary<string, string> fixZones, HashSet<string> zones) {
            char splitter = LineUtils.GetSplitter(line);
            string templine = splitter == ';' ? line.Replace(';', ':') : line;
            if (Config.fixDomains) {
                string[] splitLine = line.Split(splitter);
                templine = $"{splitLine[0].Split('@')[0]}@";
                templine += $"{LineUtils.DomainFix(splitLine[0].Split('@')[1].Split('.')[0], fixDomains, domains)}";
                templine += $"{LineUtils.ZoneFix(splitLine[0].Split('@')[1].Split('.')[1], fixZones, zones)}";
                if (splitter != '@' && splitLine.Length >= 2)
                    templine += ":" + string.Join(":", splitLine.Skip(1));
            }
            return templine;
        }

        public static long GetLinesCount(string path) {
            long i = 0;
            foreach (var line in File.ReadLines(path))
                i += 1;
            return i;
        }

        public static async Task TempToTxtAsync(string file) {
            if (!File.Exists(file) || !file.EndsWith(".tmp")) return;
            var lines = new SortedSet<string>();
            using (var reader = new StreamReader(file)) {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                    lines.Add(line);
            }
            await File.WriteAllLinesAsync(file.Replace(".tmp", ".txt"), lines);
            File.Delete(file);
        }

        public static HashSet<string> WriteToHashSet(string fileName) =>
            new(File.ReadLines(fileName));

        public static Dictionary<string, string> WriteToDictionary(string fileName) {
            var result = new Dictionary<string, string>();
            foreach (var line in File.ReadLines(fileName)) {
                var parts = line.Split('=');
                if (parts.Length == 2)
                    result[parts[0]] = parts[1];
            }
            return result;
        }
    }
}