using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsdnArticleExtract.Utilities
{
    public static class ConsoleUtils
    {
        public static void PressAnyKeyToContinue(string prompt)
        {
            Console.Write($"[#] {prompt}");
            Console.ReadKey(true);
            Console.WriteLine();
        }

        public static void PressAnyKeyToContinue(bool noPrompt)
        {
            if (noPrompt)
                Console.ReadKey(true);
            else
                PressAnyKeyToContinue("Press any key to continue.");
        }

        public static void PressAnyKeyToContinue()
        {
            PressAnyKeyToContinue(false);
        }

        public static string Input(string prompt)
        {
            Console.Write($"[#] {prompt} ");
            string? input = Console.ReadLine();
            if (input == null)
                Environment.Exit(0);
            return input!;
        }

        public static string InputUtil(string prompt, Func<string, bool> validator)
        {
            Console.Write($"[#] {prompt} ");

            while (true)
            {
                string? input =
                    Console.ReadLine();

                if (input == null)
                    Environment.Exit(0);

                if (validator.Invoke(input))
                    return input;

                Console.WriteLine("[X] Invalid input!");
            }
        }

        public static int Select(string prompt, params string[] options)
        {
            Console.WriteLine($"[#] {prompt}");
            for (int i = 0; i < options.Length; i++)
                Console.WriteLine($"  {i + 1}. {options[i]}");

            while (true)
            {
                Console.Write("[#] Please select an option: ");

                string? input = 
                    Console.ReadLine();

                if (input == null)
                    Environment.Exit(0);

                if (int.TryParse(input, out int result))
                {
                    if (result >= 1 && result <= options.Length)
                        return result - 1;
                }
            }
        }

        public static TEnum Select<TEnum>(string prompt) where TEnum : struct, Enum
        {
            TEnum[] values = 
                Enum.GetValues<TEnum>();

            string[] options =
                values.Select(value => value.ToString())
                .ToArray();

            return values[Select(prompt, options)];
        }

        public static bool YesOrNo(string prompt, bool? defaultValue = true)
        {
            string promptTail;
            if (!defaultValue.HasValue)
                promptTail = "(y/n)";
            else if (defaultValue.Value)
                promptTail = "(Y/n)";
            else
                promptTail = "(y/N)";

            while (true)
            {
                Console.Write($"[?] {prompt} {promptTail}");

                var keyInfo =
                    Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.Enter && defaultValue.HasValue)
                {
                    Console.WriteLine();
                    return defaultValue.Value;
                }
                else if (keyInfo.Key == ConsoleKey.Y)
                {
                    Console.WriteLine();
                    return true;
                }
                else if (keyInfo.Key == ConsoleKey.N)
                {
                    Console.WriteLine();
                    return false;
                }
            }
        }


        public static void Log(string message) =>
            Console.WriteLine($"[#] {message}");

        public static void Warn(string warning) => 
            Console.WriteLine($"[!] {warning}");

        public static void Error(string error) => 
            Console.WriteLine($"[X] {error}");

        public static void Tip(string tip) =>
            Console.WriteLine($"[@] {tip}");
    }
}
