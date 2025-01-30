using System;

namespace Avalonia.Utilities;

internal sealed class DebuggerHelper
{
	public static void Launch(Action<string> log)
    {
        // According this https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.debugger.launch?view=net-6.0#remarks
        // documentation, on not windows platform Debugger.Launch() always return true without running a debugger.
        if (System.Diagnostics.Debugger.Launch())
        {
            // Set timeout at 1 minut.
            var time = new System.Diagnostics.Stopwatch();
            var timeout = TimeSpan.FromMinutes(1);
            time.Start();

            // wait for the debugger to be attached or timeout.
            while (!System.Diagnostics.Debugger.IsAttached && time.Elapsed < timeout)
            {
                log?.Invoke($"[PID:{System.Diagnostics.Process.GetCurrentProcess().Id}] Wating attach debugger. Elapsed {time.Elapsed}...");
                System.Threading.Thread.Sleep(100);
            }

            time.Stop();
            if (time.Elapsed >= timeout)
            {
                log?.Invoke("Wating attach debugger timeout.");
            }
        }
        else
        {
            log?.Invoke("Debugging cancelled.");
        }
    }
}
