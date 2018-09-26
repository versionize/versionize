using System;

namespace Versionize
{

    public static class ConsoleUI
    {
        public static void Exit(string message, int code)
        {
            Console.WriteLine(message);
            Environment.Exit(code);
        }
    }
}