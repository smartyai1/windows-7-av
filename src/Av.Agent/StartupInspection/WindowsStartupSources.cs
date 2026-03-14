using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Av.Agent.StartupInspection;

public sealed class WindowsStartupSources : IStartupEntriesSource
{
    private static readonly string[] RunKeyPaths =
    [
        @"Software\Microsoft\Windows\CurrentVersion\Run",
        @"Software\Microsoft\Windows\CurrentVersion\RunOnce"
    ];

    public Task<IReadOnlyList<StartupEntry>> EnumerateAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<StartupEntry>();
        entries.AddRange(ReadRunEntries(Registry.CurrentUser, "HKCU"));
        entries.AddRange(ReadRunEntries(Registry.LocalMachine, "HKLM"));
        entries.AddRange(ReadStartupFolders());
        entries.AddRange(ReadScheduledTasks());
        entries.AddRange(ReadServices());
        return Task.FromResult<IReadOnlyList<StartupEntry>>(entries);
    }

    public Task<bool> DisableAsync(string entryId, CancellationToken cancellationToken = default)
    {
        if (entryId.StartsWith("reg:", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(DisableRegistryEntry(entryId));
        }

        if (entryId.StartsWith("startup:", StringComparison.OrdinalIgnoreCase))
        {
            var file = entryId["startup:".Length..];
            if (File.Exists(file))
            {
                File.Move(file, file + ".disabled", overwrite: true);
                return Task.FromResult(true);
            }
        }

        if (entryId.StartsWith("task:", StringComparison.OrdinalIgnoreCase))
        {
            var taskName = entryId["task:".Length..];
            Execute("schtasks", $"/Change /TN \"{taskName}\" /Disable");
            return Task.FromResult(true);
        }

        if (entryId.StartsWith("svc:", StringComparison.OrdinalIgnoreCase))
        {
            var serviceName = entryId["svc:".Length..];
            Execute("sc", $"config \"{serviceName}\" start= disabled");
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private static IEnumerable<StartupEntry> ReadRunEntries(RegistryKey root, string hive)
    {
        foreach (var keyPath in RunKeyPaths)
        {
            using var key = root.OpenSubKey(keyPath, writable: false);
            if (key is null)
            {
                continue;
            }

            var type = keyPath.EndsWith("RunOnce", StringComparison.OrdinalIgnoreCase)
                ? StartupEntryType.RunOnce
                : StartupEntryType.Run;

            foreach (var name in key.GetValueNames())
            {
                var command = key.GetValue(name)?.ToString() ?? string.Empty;
                yield return new StartupEntry(
                    $"reg:{hive}\\{keyPath}::{name}",
                    type,
                    name,
                    $"{hive}\\{keyPath}",
                    command,
                    null,
                    null);
            }
        }
    }

    private static IEnumerable<StartupEntry> ReadStartupFolders()
    {
        var startupDirs = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
        }.Where(static p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p));

        foreach (var dir in startupDirs)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
            {
                yield return new StartupEntry(
                    $"startup:{file}",
                    StartupEntryType.StartupFolder,
                    Path.GetFileName(file),
                    dir,
                    file,
                    null,
                    null);
            }
        }
    }

    private static IEnumerable<StartupEntry> ReadScheduledTasks()
    {
        var output = Execute("schtasks", "/Query /FO CSV /V");
        using var reader = new StringReader(output);
        _ = reader.ReadLine();
        while (reader.ReadLine() is { } line)
        {
            var cells = ParseCsvLine(line);
            if (cells.Count < 9)
            {
                continue;
            }

            var taskName = cells[0];
            var taskToRun = cells[8];
            yield return new StartupEntry(
                $"task:{taskName}",
                StartupEntryType.ScheduledTask,
                taskName,
                "Task Scheduler",
                taskToRun,
                null,
                null);
        }
    }

    private static IEnumerable<StartupEntry> ReadServices()
    {
        foreach (var service in ServiceController.GetServices())
        {
            yield return new StartupEntry(
                $"svc:{service.ServiceName}",
                StartupEntryType.Service,
                service.DisplayName,
                "Service Control Manager",
                service.ServiceName,
                null,
                null);
        }
    }

    private static bool DisableRegistryEntry(string entryId)
    {
        var body = entryId["reg:".Length..];
        var split = body.Split("::", StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2)
        {
            return false;
        }

        var path = split[0];
        var valueName = split[1];
        var hive = path.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase) ? Registry.CurrentUser : Registry.LocalMachine;
        var subPath = path[5..];
        using var key = hive.OpenSubKey(subPath, writable: true);
        if (key is null)
        {
            return false;
        }

        key.DeleteValue(valueName, throwOnMissingValue: false);
        return true;
    }

    private static string Execute(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
        {
            return string.Empty;
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current);
                current = string.Empty;
                continue;
            }

            current += ch;
        }

        result.Add(current);
        return result;
    }
}
