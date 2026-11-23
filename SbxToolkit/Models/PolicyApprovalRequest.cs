using System.Text.Json.Serialization;

namespace SbxToolkit.Models;

public sealed class PolicyApprovalRequest
{
	public required DateTimeOffset CreatedAt { get; set; }
	public required string Detail { get; init; }
	public required string Id { get; init; }
	public required string SandboxName { get; init; }
	public required string Title { get; init; }
	public required Option[] Options { get; init; }

	public sealed record Option(string Id, string Label);
}

[JsonSerializable(typeof(PolicyApprovalRequest[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public sealed partial class PolicyApprovalRequestContext : JsonSerializerContext;