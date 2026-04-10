// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Tools.AppService.Models;
using Azure.Mcp.Tools.AppService.Options;
using Azure.Mcp.Tools.AppService.Options.Webapp;
using Azure.Mcp.Tools.AppService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Extensions;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AppService.Commands.Webapp;

public sealed class WebappCreateCommand(ILogger<WebappCreateCommand> logger)
    : BaseAppServiceCommand<WebappCreateOptions>(resourceGroupRequired: false, appRequired: false)
{
    private const string CommandTitle = "Create App Service Web App";
    private readonly ILogger<WebappCreateCommand> _logger = logger;

    public override string Id => "e3a7c1d5-9f2b-4e8a-b6d0-3c5f7a9e1b4d";

    public override string Name => "create";

    public override string Description =>
        """
        Create a new Azure App Service web app with an App Service Plan.
        Creates the App Service Plan if it does not already exist, then creates the web app on that plan.
        Supports dotnet, node, python, and php runtime stacks on Linux or Windows.
        Most parameters are optional with smart defaults:
        - App name: auto-generated if not specified (e.g., webapp-a1b2c3d4)
        - Resource group: defaults to '{app-name}-rg'
        - Location: defaults to canadacentral
        - SKU: defaults to P0V3
        - Runtime version: defaults to the latest version for the selected runtime
        - OS type: defaults to linux for node, python, php; required for dotnet
        - Plan name: defaults to '{app-name}-plan'
        Only --runtime is required (dotnet, node, python, or php).
        Uses the default Azure subscription from 'az login' if --subscription is not specified.
        """;

    public override string Title => CommandTitle;

    public override ToolMetadata Metadata => new()
    {
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        ReadOnly = false,
        LocalRequired = false,
        Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AppServiceOptionDefinitions.Location);
        command.Options.Add(AppServiceOptionDefinitions.Runtime.AsRequired());
        command.Options.Add(AppServiceOptionDefinitions.Sku);
        command.Options.Add(AppServiceOptionDefinitions.RuntimeVersion);
        command.Options.Add(AppServiceOptionDefinitions.OsType);
        command.Options.Add(AppServiceOptionDefinitions.Plan);

        command.Validators.Add(commandResult =>
        {
            var runtime = commandResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.Runtime.Name);
            var osType = commandResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.OsType.Name);

            if (string.Equals(runtime, "dotnet", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(osType))
            {
                commandResult.AddError("The --os-type option is required for the dotnet runtime. Specify 'linux' or 'windows'.");
            }

            if (!string.IsNullOrEmpty(osType) &&
                !string.Equals(osType, "linux", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(osType, "windows", StringComparison.OrdinalIgnoreCase))
            {
                commandResult.AddError($"Invalid --os-type value '{osType}'. Accepted values: linux, windows.");
            }

            if (!string.IsNullOrEmpty(runtime) &&
                string.Equals(runtime, "python", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(osType, "windows", StringComparison.OrdinalIgnoreCase))
            {
                commandResult.AddError("The python runtime is not supported on Windows. Use Linux instead.");
            }
        });
    }

    protected override WebappCreateOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Location = parseResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.Location.Name);
        options.Runtime = parseResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.Runtime.Name);
        options.Sku = parseResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.Sku.Name);
        options.RuntimeVersion = parseResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.RuntimeVersion.Name);
        options.OsType = parseResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.OsType.Name);
        options.Plan = parseResult.GetValueOrDefault<string>(AppServiceOptionDefinitions.Plan.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid)
        {
            return context.Response;
        }

        var options = BindOptions(parseResult);

        // Apply defaults for optional parameters
        options.AppName ??= $"webapp-{Guid.NewGuid().ToString("N")[..8]}";
        options.ResourceGroup ??= $"{options.AppName}-rg";
        options.Location ??= "canadacentral";

        try
        {
            context.Activity?.AddTag("subscription", options.Subscription);

            var appServiceService = context.GetService<IAppServiceService>();
            var result = await appServiceService.CreateWebAppAsync(
                options.AppName!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Location!,
                options.Runtime!,
                options.Sku,
                options.RuntimeVersion,
                options.OsType,
                options.Plan,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(new(result), AppServiceJsonContext.Default.WebappCreateCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating Web App. AppName: {AppName}, ResourceGroup: {ResourceGroup}, Location: {Location}, Subscription: {Subscription}",
                options.AppName, options.ResourceGroup, options.Location, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override HttpStatusCode GetStatusCode(Exception ex) => ex switch
    {
        RequestFailedException reqEx => (HttpStatusCode)reqEx.Status,
        Identity.AuthenticationFailedException => HttpStatusCode.Unauthorized,
        ArgumentException => HttpStatusCode.BadRequest,
        _ => base.GetStatusCode(ex)
    };

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        RequestFailedException reqEx when reqEx.Status == 404 =>
            "Resource not found. Verify the resource group exists and you have access.",
        RequestFailedException reqEx when reqEx.Status == 403 =>
            $"Authorization failed. Verify you have permissions to create App Service resources. Details: {reqEx.Message}",
        RequestFailedException reqEx when reqEx.Status == 409 =>
            $"A web app with the specified name already exists. Details: {reqEx.Message}",
        RequestFailedException reqEx when reqEx.Message.Contains("quota", StringComparison.OrdinalIgnoreCase) =>
            $"Quota exceeded. You may need to request a quota increase. Details: {reqEx.Message}",
        RequestFailedException reqEx => reqEx.Message,
        Identity.AuthenticationFailedException =>
            "Authentication failed. Please run 'az login' to sign in.",
        ArgumentException argEx => argEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    public record WebappCreateCommandResult(Models.WebappCreateResult Webapp);
}
