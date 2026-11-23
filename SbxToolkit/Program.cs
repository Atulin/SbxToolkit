using System.Diagnostics;
using System.Text.Json;
using SbxToolkit.Extensions;
using SbxToolkit.Models;
using SbxToolkit.Widgets;
using Spectre.Console;

var listProcess = new ProcessStartInfo("sbx")
{
	ArgumentList = { "policy", "approval", "ls", "--json" },
	RedirectStandardOutput = true,
};

var process = await Process.EnsureStartAsync(listProcess);

var requests = JsonSerializer.Deserialize(process.StandardOutput.BaseStream, PolicyApprovalRequestContext.Default.PolicyApprovalRequestArray);

if (requests == null)
{
	throw new InvalidOperationException("Failed to deserialize policy approval requests");
}

var response = ReviewPrompt.Run(
	requests,
	display: r => r.Title,
	itemKey: r => r.Id,
	options: r => r.Options,
	optionLabel: o => o.Label,
	optionKey: o => o.Id,
	defaultValue: new("", "Ignore")
);

var tree = new Tree("Status");

AnsiConsole.Live(tree)
	.Start(ctx => {
		foreach (var (label, itemId, choice) in response)
		{
			var lbl = tree.AddNode(label);

			if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(choice))
			{
				lbl.AddNode("Ignored");
				ctx.Refresh();
				continue;
			}

			var processInfo = new ProcessStartInfo("sbx")
			{
				ArgumentList = { "policy", "approval", "respond", itemId, "--option", choice },
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			if (!Process.TryStart(processInfo, out var res, out var error))
			{
				lbl.AddNode($"[red]{error}[/]");
				ctx.Refresh();
				continue;
			}

			if (res.StandardError.ReadToEnd() is { Length: > 0 } err)
			{
				lbl.AddNode($"[red]{err.Trim()}[/]");
			}

			if (res.StandardOutput.ReadToEnd() is { Length: > 0 } std)
			{
				lbl.AddNode($"[green]{std.Trim()}[/]");
			}

			ctx.Refresh();
		}
	});