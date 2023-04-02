namespace consoleutils {

    internal class ConsoleUtils {

        public static void WriteColorized(string text, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteColorizedYN() {
            Console.Write(" ( ");
            WriteColorized("y", ConsoleColor.Red);
            Console.Write(" / ");
            WriteColorized("n", ConsoleColor.Green);
            Console.WriteLine(" ):");
        }

        public static bool WorkWithArgs(string[] args, ref string input, ref string output) {
            if (args.Length != 4 || !args.Contains("-input") || !args.Contains("-output")) {
                Console.WriteLine("Wrong arguments!\nUse \"-input file.txt -output file.txt\" for correct work of the app.\n");
                Environment.Exit(0);
            }
            input = args[1];
            output = args[3];
            if (args[2] == "-input") {
                input = args[3];
                output = args[1];
            }
            if (!File.Exists(input)) {
                Console.WriteLine("Input file not found!");
                Environment.Exit(0);
            }
            return true;
        }
    }
}