// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AppService.Options.Webapp;

public class WebappCreateOptions : BaseAppServiceOptions
{
    [JsonPropertyName(AppServiceOptionDefinitions.LocationName)]
    public string? Location { get; set; }

    [JsonPropertyName(AppServiceOptionDefinitions.SkuName)]
    public string? Sku { get; set; }

    [JsonPropertyName(AppServiceOptionDefinitions.RuntimeName)]
    public string? Runtime { get; set; }

    [JsonPropertyName(AppServiceOptionDefinitions.RuntimeVersionName)]
    public string? RuntimeVersion { get; set; }

    [JsonPropertyName(AppServiceOptionDefinitions.OsTypeName)]
    public string? OsType { get; set; }

    [JsonPropertyName(AppServiceOptionDefinitions.PlanName)]
    public string? Plan { get; set; }
}
