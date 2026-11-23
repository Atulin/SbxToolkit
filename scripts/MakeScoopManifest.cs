#!/usr/bin/env dotnet

#:package Microsoft.Extensions.Configuration@11.0.0
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@11.0.0

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
	.AddEnvironmentVariables()
	.Build();

var version = config.Require("VERSION");
var aotHash = config.Require("AOT_HASH");
var fdHash = config.Require("FD_HASH");

const string repo = "https://github.com/Atulin/SbxToolkit";
var baseUrl = $"https://github.com/Atulin/SbxToolkit/releases/download/v{version}";

Directory.CreateDirectory("bucket");

var manifests = new[] { true, false }
	.Select(isAot => new Manifest
	{
		Name = isAot ? "sbxtoolkit" : "sbxtoolkit-fd",
		Version = version,
		Description = $"Docker Sandboxes toolkit ({(isAot ? "AOT" : "Framework-dependent")})",
		Homepage = repo,
		License = "MIT",
		Url = Path.Join(baseUrl, isAot ? "sbxtoolkit-win-x64-aot.zip" : "sbxtoolkit-win-x64-fd.zip"),
		Hash = isAot ? aotHash : fdHash,
		Bin =
		[
			["SbxToolkit.exe", "sbxtoolkit"],
			["SbxToolkit.exe", "sbxt"]
		],
		Checkver = new() { ["github"] = repo },
		Autoupdate = new(Path.Join(baseUrl, isAot ? "sbxtoolkit-win-x64-aot.zip" : "sbxtoolkit-win-x64-fd.zip"))
	})
	.Select(m => new { Json = JsonSerializer.Serialize(m, ManifestContext.Default.Manifest), Path = Path.Join("bucket", $"{m.Name}.json") });

foreach (var manifest in manifests)
{
	await File.WriteAllTextAsync(manifest.Path, manifest.Json);
}

return;

record Autoupdate(string Url);

class Manifest
{
	public required string Name { get; init; }
	public required string Version { get; init; }
	public required string Description { get; init; }
	public required string Homepage { get; init; }
	public required string License { get; init; }
	public required string Url { get; init; }
	public required string Hash { get; init; }
	public required List<List<string>> Bin { get; init; }
	public required Dictionary<string, string> Checkver { get; init; }
	public required Autoupdate? Autoupdate { get; init; }
}

[JsonSerializable(typeof(Manifest))]
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	WriteIndented = true,
	IndentSize = 4
)]
internal sealed partial class ManifestContext : JsonSerializerContext;

internal static class Helpers
{
	extension(IConfigurationRoot root)
	{
		public string Require(string key) => root[key] ?? throw new InvalidOperationException($"Environment variable '{key}' is not set");
	}
}