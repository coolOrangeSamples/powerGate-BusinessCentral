using System;
using System.Diagnostics;

namespace BusinessCentralPlugin.Helper
{
    internal class Timer : IDisposable
    {
        public static readonly object ConsoleLock = new object();
        private Stopwatch _stopwatch;
        private readonly string _text;
        public Timer(string text = null)
        {
            if (Console.IsOutputRedirected)
                return;

            if (string.IsNullOrEmpty(text))
                text = new StackTrace().GetFrame(3).GetMethod().Name;

            _text = text;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public void Dispose()
        {
            if (Console.IsOutputRedirected)
                return;

            _stopwatch.Stop();
            lock (ConsoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{_text}: {_stopwatch.ElapsedMilliseconds} ms");
                Console.ResetColor();
            }
            _stopwatch = null;
        }
    }
}