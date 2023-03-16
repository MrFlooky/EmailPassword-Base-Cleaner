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
    }
}