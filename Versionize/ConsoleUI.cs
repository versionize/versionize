using System;
using System.Drawing;
using Colorful;
using Console = Colorful.Console;

namespace Versionize
{
    public static class ConsoleUI
    {
        public static void Exit(string message, int code)
        {
            Console.WriteLine(message, Color.Red);
            Environment.Exit(code);
        }

        public static void Information(string message)
        {
            Console.WriteLine(message, Color.LightGray);
        }

        public static void Step(string message)
        {
            string stepMessage = "{0} {1}";
            var messageFormatters = new Formatter[]
            {
                new Formatter("√", Color.Green),
                new Formatter(message, Color.LightGray),
            };

            Console.WriteLineFormatted(stepMessage, Color.White, messageFormatters);
        }

    }
}