using System;
using System.IO;

namespace E.Standard.Configuration;

/// <summary>
/// Loads a simple "KEY=VALUE" per-line env file (comparable to a Docker/Compose "--env-file")
/// from the "_config" directory into real process environment variables - e.g.
/// "_config/logging.env".
///
/// This must run before the host builder is created: its environment-variables configuration
/// source snapshots <see cref="Environment.GetEnvironmentVariables"/> once, at that point, so a
/// process environment variable set here is only picked up by
/// <c>Microsoft.Extensions.Configuration</c> if it exists beforehand.
///
/// Typical use case: Kubernetes deployments that only mount the "_config" directory (e.g. via a
/// ConfigMap volume) and have no easy way to set pod-level environment variables can still set
/// env-var-style settings (e.g. the standard OpenTelemetry <c>OTEL_*</c> variables) this way,
/// without touching the deployment/pod spec.
/// </summary>
static public class EnvFileLoader
{
    static public void LoadConfigDirectoryEnvFile(string fileName)
    {
        var fi = new FileInfo(ConfigDirectory.ResolveFilePath(fileName));

        if (!fi.Exists)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(fi.FullName))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line.Substring(0, separatorIndex).Trim();
            var value = line.Substring(separatorIndex + 1).Trim().Trim('"');

            // An environment variable that is already set (e.g. by the container/orchestrator
            // itself) always wins - the file is a fallback/default, not a forced override.
            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
