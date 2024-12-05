using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;

namespace ServiceWire.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // These can be helpful if you're just trying to work on a single benchmark
            //BenchmarkRunner.Run<ConnectionBenchmarks>();
            //BenchmarkRunner.Run<NamedPipesBenchmarks>();
            //BenchmarkRunner.Run<TcpBenchmarks>();


            var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
            // Run with command line args if provided
            if (args.Length > 0)
            {
                switcher.Run(args);
                return;
            }

            // Otherwise run them all and combine the results into a single report (command line, HTML)
            var summary = switcher.RunAllJoined();

            // Launching the html report in the browser, makes it nice and easy to see the results
            string htmlReportPath = System.IO.Path.Combine(summary.ResultsDirectoryPath, $"{summary.Title}-report.html");
            OpenUrl(htmlReportPath);
        }


        private static void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Detects to see if the user is running in a "desktop environment"/GUI, or if they are running in a terminal session.
                // Won't be able to launch a web browser without a GUI
                // https://en.wikipedia.org/wiki/Desktop_environment
                var currDesktopEnvironment = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
                if (String.IsNullOrEmpty(currDesktopEnvironment))
                {
                    return;
                }

                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}
