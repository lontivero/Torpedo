using System;
using System.Collections.Generic;

namespace Torpedo
{
    class Logger
    {
        private static object _sync = new object();

        private static Dictionary<Type, Logger> _loggers = new Dictionary<Type, Logger>();

        public static Logger GetLogger<T>()
        {
            var typeOfT = typeof(T);
            if(_loggers.TryGetValue(typeOfT, out var logger))
            {
                return logger;
            }
            logger = new Logger(typeOfT.Name);
            _loggers.Add(typeOfT, logger);
            return logger;
        }

        private string Name { get; }

        private Logger(string name)
        {
            Name = name;
        }

        public void Info(string text)
        {
            Console.WriteLine($"{Name}: INFO  - {text}");
        }

        public void Debug(string text)
        {
            Console.WriteLine($"{Name}: DEBUG - {text}");
        }

        public void Error(string text)
        {
            lock(_sync)
            {
                var currentColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{Name}: ERROR - {text}");
                }
                finally
                {
                    Console.ForegroundColor = currentColor; 
                }
            }
        }
    }
}