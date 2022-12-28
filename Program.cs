using System.Text.RegularExpressions;


while (true) {
	Console.WriteLine("Hi! Drop a file here: ");
	string? path = Console.ReadLine();
	if (pathRegex().IsMatch(path) && File.Exists(path)) {
		Console.WriteLine("File is a text file.");
	}
	else {
		Console.WriteLine("Drop a text file.");
	}
	Console.WriteLine("Do you want to exit? ( y / n ): ");
	path = Console.ReadLine();
	if (path == "y" || path == "Y")
		break;
	Console.Clear();
}

partial class Program {
	[GeneratedRegex(@"^[A-Za-z]:(?:\\[^\\\/:*?""<>\|]+)*\\[^\\\/:*?""<>\|]+\.txt$")]
	private static partial Regex pathRegex();
}