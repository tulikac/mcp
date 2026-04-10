// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AppService.Models;

public sealed record WebappCreateResult(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("defaultHostName")] string? DefaultHostName,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("appServicePlanName")] string? AppServicePlanName,
    [property: JsonPropertyName("runtime")] string? Runtime,
    [property: JsonPropertyName("osType")] string? OsType,
    [property: JsonPropertyName("provisioningState")] string? ProvisioningState);
