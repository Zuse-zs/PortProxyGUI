using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace PortProxyGUI.Utils;

internal static class WslUtil
{
    public static string GetDefaultDistributionAddress()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = "-e sh -lc \"hostname -I\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null) return null;
        if (!process.WaitForExit(5000))
        {
            process.Kill();
            throw new TimeoutException("获取 WSL 地址超时。");
        }

        var output = process.StandardOutput.ReadToEnd();
        if (process.ExitCode != 0) return null;

        return output
            .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(IsUsableIPv4Address);
    }

    private static bool IsUsableIPv4Address(string value)
    {
        return IPAddress.TryParse(value, out var address)
            && address.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.IsLoopback(address);
    }
}
