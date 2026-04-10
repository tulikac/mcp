// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.AppService.Options;

public static class AppServiceOptionDefinitions
{
    public const string AppName = "app";
    public const string DatabaseType = "database-type";
    public const string DatabaseServer = "database-server";
    public const string DatabaseName = "database";
    public const string ConnectionString = "connection-string";
    public const string AppSettingNameName = "setting-name";
    public const string AppSettingValueName = "setting-value";
    public const string AppSettingUpdateTypeName = "setting-update-type";
    public const string DeploymentIdName = "deployment-id";
    public const string DetectorNameName = "detector-name";
    public const string StartTimeName = "start-time";
    public const string EndTimeName = "end-time";
    public const string IntervalName = "interval";
    public const string LocationName = "location";
    public const string SkuName = "sku";
    public const string RuntimeName = "runtime";
    public const string RuntimeVersionName = "runtime-version";
    public const string OsTypeName = "os-type";
    public const string PlanName = "plan";

    public static readonly Option<string> AppServiceName = new($"--{AppName}")
    {
        Description = "The name of the Azure App Service (e.g., my-webapp).",
        Required = true
    };

    public static readonly Option<string> DatabaseTypeOption = new($"--{DatabaseType}")
    {
        Description = "The type of database (e.g., SqlServer, MySQL, PostgreSQL, CosmosDB).",
        Required = true
    };

    public static readonly Option<string> DatabaseServerOption = new($"--{DatabaseServer}")
    {
        Description = "The server name or endpoint for the database (e.g., myserver.database.windows.net).",
        Required = true
    };

    public static readonly Option<string> DatabaseNameOption = new($"--{DatabaseName}")
    {
        Description = "The name of the database to connect to (e.g., mydb).",
        Required = true
    };

    public static readonly Option<string> ConnectionStringOption = new($"--{ConnectionString}")
    {
        Description = "The connection string for the database. If not provided, a default will be generated.",
        Required = false
    };

    public static readonly Option<string> AppSettingName = new($"--{AppSettingNameName}")
    {
        Description = "The name of the application setting.",
        Required = true
    };

    public static readonly Option<string> AppSettingValue = new($"--{AppSettingValueName}")
    {
        Description = "The value of the application setting. Required for add and set update types.",
        Required = false
    };

    public static readonly Option<string> AppSettingUpdateType = new($"--{AppSettingUpdateTypeName}")
    {
        Description = "The type of update to perform on the application setting. Valid values are: add, set, delete.",
        Required = true
    };

    public static readonly Option<string> DeploymentIdOption = new($"--{DeploymentIdName}")
    {
        Description = "The ID of the deployment.",
        Required = false
    };

    public static readonly Option<string> DetectorName = new($"--{DetectorNameName}")
    {
        Description = "The name of the diagnostic detector to run (e.g., Availability, CpuAnalysis, MemoryAnalysis).",
        Required = true
    };

    public static readonly Option<string> StartTime = new($"--{StartTimeName}")
    {
        Description = "The start time in ISO format (e.g., 2023-01-01T00:00:00Z).",
        Required = false
    };

    public static readonly Option<string> EndTime = new($"--{EndTimeName}")
    {
        Description = "The end time in ISO format (e.g., 2023-01-01T00:00:00Z).",
        Required = false
    };

    public static readonly Option<string> Interval = new($"--{IntervalName}")
    {
        Description = "The time interval (e.g., PT1H for 1 hour, PT5M for 5 minutes).",
        Required = false
    };

    public static readonly Option<string> Location = new($"--{LocationName}")
    {
        Description = "The Azure region for the App Service (e.g., eastus, westeurope, canadacentral). Defaults to canadacentral if not specified.",
        Required = false
    };

    public static readonly Option<string> Sku = new($"--{SkuName}")
    {
        Description = "The pricing SKU for the App Service Plan (e.g., F1, B1, S1, P0V3). Defaults to P0V3 if not specified.",
        Required = false
    };

    public static readonly Option<string> Runtime = new($"--{RuntimeName}")
    {
        Description = "The application runtime stack. Accepted values: dotnet, node, python, php.",
        Required = false
    };

    public static readonly Option<string> RuntimeVersion = new($"--{RuntimeVersionName}")
    {
        Description = "The runtime version (e.g., 10.0 for dotnet, 24-lts for node). Defaults to the latest version for the selected runtime.",
        Required = false
    };

    public static readonly Option<string> OsType = new($"--{OsTypeName}")
    {
        Description = "The operating system type. Accepted values: linux, windows. Required for dotnet runtime; defaults to linux for node, python, and php.",
        Required = false
    };

    public static readonly Option<string> Plan = new($"--{PlanName}")
    {
        Description = "The name of the App Service Plan. If not specified, a plan named '{app-name}-plan' will be created or reused.",
        Required = false
    };
}
