using System;
using System.IO;

namespace Av.Agent.StartupInspection;

public sealed class DefaultExecutableResolver : IExecutableResolver
{
    public (string? path, string? arguments) Resolve(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return (null, null);
        }

        var trimmed = command.Trim();
        if (trimmed.StartsWith('"'))
        {
            var closing = trimmed.IndexOf('"', 1);
            if (closing > 1)
            {
                var quotedPath = trimmed[1..closing];
                var args = trimmed[(closing + 1)..].Trim();
                return (ExpandPath(quotedPath), string.IsNullOrWhiteSpace(args) ? null : args);
            }
        }

        var firstSpace = trimmed.IndexOf(' ');
        if (firstSpace < 0)
        {
            return (ExpandPath(trimmed), null);
        }

        var path = trimmed[..firstSpace];
        var arguments = trimmed[(firstSpace + 1)..].Trim();
        return (ExpandPath(path), string.IsNullOrWhiteSpace(arguments) ? null : arguments);
    }

    private static string ExpandPath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
        return Path.GetFullPath(expanded);
    }
}
