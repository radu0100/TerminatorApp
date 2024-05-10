using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public class TerminatorConfig
{
    public string ProcessName { get; set; } = string.Empty;
    public int MaxRuntimeMs { get; set; }
    public int CheckIntervalMs { get; set; }
}

public class Terminator
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    private const int VK_Q = 0x51;

    public static void Main(string[] args)
    {
        var config = ReadAndParseConfiguration();
        if (config == null)
        {
            Console.WriteLine("Exiting program due to configuration errors.");
            return;
        }

        Process? monitoredProcess = StartProcess(config.ProcessName);
        if (monitoredProcess == null)
        {
            Console.WriteLine("Could not start the process. Please check the application name and try again.");
            Environment.Exit(1);
        }

        MonitorProcessAndKeyPress(config, monitoredProcess);
    }

    private static TerminatorConfig? ReadAndParseConfiguration()
    {
        while (true)
        {
            Console.WriteLine("Please enter the process to start, max runtime in minutes, and check interval in minutes (e.g., notepad 5 1):");
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("No input provided, please try again.");
                continue;
            }

            try
            {
                return ParseInput(input);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                continue;
            }
        }
    }

    public static TerminatorConfig ParseInput(string input)
    {
        string[] inputs = input.Split();
        if (inputs.Length != 3)
        {
            throw new ArgumentException("Invalid input format. Please use the format: [ApplicationName] [MaxRuntimeInMinutes] [CheckIntervalInMinutes]");
        }

        if (!int.TryParse(inputs[1], out int maxRuntime) || !int.TryParse(inputs[2], out int checkInterval))
        {
            throw new ArgumentException("Invalid numbers for runtime or interval. Please enter valid integers.");
        }

        return new TerminatorConfig
        {
            ProcessName = inputs[0],
            MaxRuntimeMs = maxRuntime * 60 * 1000,
            CheckIntervalMs = checkInterval * 60 * 1000
        };
    }

    private static Process? StartProcess(string processName)
    {
        try
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    UseShellExecute = false
                }
            };
            process.Start();
            return process;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting process: " + ex.Message);
            return null;
        }
    }

    private static void MonitorProcessAndKeyPress(TerminatorConfig config, Process process)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var keyMonitorThread = new Thread(() => MonitorKeyPress(config.MaxRuntimeMs, process));
        keyMonitorThread.Start();

        int lastReportedTime = 0;

        try
        {
            while (stopwatch.ElapsedMilliseconds < config.MaxRuntimeMs)
            {
                if (process.HasExited)
                {
                    Console.WriteLine("Process was closed by the user.");
                    break;
                }
                int elapsedMinutes = (int)(stopwatch.ElapsedMilliseconds / 60000);
                if (elapsedMinutes > lastReportedTime)
                {
                    lastReportedTime = elapsedMinutes;
                    Console.WriteLine($"Process has been running for {lastReportedTime} minutes.");
                }

                Thread.Sleep(10);
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                Console.WriteLine("Process was terminated as it exceeded the maximum runtime.");
            }
            stopwatch.Stop();
            Console.WriteLine("Monitoring stopped. Exiting program.");
            Environment.Exit(0);
        }
    }

    private static void MonitorKeyPress(int maxRuntimeMs, Process process)
    {
        var stopwatch = new Stopwatch();
        bool isKeyPressed = false;

        while (true)
        {
            if ((GetAsyncKeyState(VK_Q) & 0x8000) != 0)
            {
                if (!isKeyPressed)
                {
                    isKeyPressed = true;
                    stopwatch.Start();
                }
                else if (stopwatch.ElapsedMilliseconds >= 3000)
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        Console.WriteLine("Quitting application due to long press on 'Q'.");
                    }
                    Environment.Exit(0);
                }
            }
            else if (isKeyPressed)
            {
                isKeyPressed = false;
                stopwatch.Reset();
            }
            Thread.Sleep(100);
        }
    }
}
