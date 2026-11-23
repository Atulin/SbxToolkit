using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SbxToolkit.Extensions;

public static class ProcessExtensions
{
	extension(Process)
	{
		public static async Task<Process> EnsureStartAsync(ProcessStartInfo startInfo, CancellationToken ct = default)
		{
			var process = Process.Start(startInfo);
			
			if (process is null)
			{
				throw new InvalidOperationException("Process failed to start");
			}

			await process.WaitForExitAsync(ct);
			
			return process.ExitCode is 0 
				? process 
				: throw new InvalidOperationException($"Process exited with non-zero exit code: {process.ExitCode}");
		}

		public static bool TryStart(ProcessStartInfo info, [NotNullWhen(true)] out Process? process, [NotNullWhen(false)] out string? error)
		{
			process = null;
			error = null;
			
			try
			{
				var proc = Process.Start(info);

				if (proc is null)
				{
					error = "Process failed to start";
					return false;
				}

				if (!proc.WaitForExit(1000))
				{
					error = "Process timed out";
					return false;
				}
				
				if (proc.ExitCode is not 0)
				{
					error = $"Process exited with non-zero exit code: {proc.ExitCode}";
					return false;
				}

				process = proc;
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}
	}
}