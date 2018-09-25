using System;

namespace DeployMaster
{
    public class Cs
    {
        private static string _line = null;
        private static bool _isEnabled = true;

        public static void SetIsEnabled(bool value)
        {
            _isEnabled = value;
        }
        
        public static void AddPart(string part)
        {
            _line += part;
        }

        public static void AddPart(string part, int startPosition)
        {
            if (!_isEnabled)
            {
                return;
            }
            while (_line.Length < startPosition)
            {
                _line += " ";
            }
            _line += part;
        }

        public static void EndLine(ConsoleColor color = ConsoleColor.Gray)
        {
            Line(_line, color);
            _line = string.Empty;
        }

        public static void Line(string text, ConsoleColor color = ConsoleColor.White)
        {
            if (!_isEnabled)
            {
                return;
            }
            lock ("CS")
            {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }

        public static void Write(string text, ConsoleColor color = ConsoleColor.White)
        {
            lock ("CS")
            {
                Console.ForegroundColor = color;
                Console.Write(text);
                Console.ResetColor();
            }
        }

        public static void Line(CsPart part, ConsoleColor color = ConsoleColor.White)
        {
            Line(part.Text, color);
        }

        public static void LineSpliter(string chars5, ConsoleColor color = ConsoleColor.Blue)
        {
            if (!_isEnabled)
            {
                return;
            }
            var line = string.Empty;
            for (var i = 0; i < 30; i++)
            {
                line += chars5;
            }
            Line("\n" + line + "\n", color);
        }

    }
}
