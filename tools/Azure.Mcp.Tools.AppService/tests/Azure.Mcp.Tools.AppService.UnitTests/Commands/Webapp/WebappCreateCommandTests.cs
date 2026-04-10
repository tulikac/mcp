// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Net;
using System.Text.Json;
using Azure.Mcp.Tools.AppService.Commands;
using Azure.Mcp.Tools.AppService.Commands.Webapp;
using Azure.Mcp.Tools.AppService.Models;
using Azure.Mcp.Tools.AppService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.AppService.UnitTests.Commands.Webapp;

[Trait("Command", "WebappCreate")]
public class WebappCreateCommandTests
{
    private readonly IAppServiceService _appServiceService;
    private readonly ILogger<WebappCreateCommand> _logger;
    private readonly WebappCreateCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public WebappCreateCommandTests()
    {
        _appServiceService = Substitute.For<IAppServiceService>();
        _logger = Substitute.For<ILogger<WebappCreateCommand>>();

        _command = new(_logger);

        var services = new ServiceCollection();
        services.AddSingleton(_appServiceService);
        _context = new(services.BuildServiceProvider());
        _commandDefinition = _command.GetCommand();
    }

    [Theory]
    [InlineData("dotnet", "linux", "10.0", "P0V3")]
    [InlineData("dotnet", "windows", "10.0", "S1")]
    [InlineData("node", null, "24-lts", null)]
    [InlineData("python", null, null, null)]
    [InlineData("php", "linux", "8.5", "B1")]
    public async Task ExecuteAsync_WithValidParameters_CallsServiceWithCorrectArguments(
        string runtime, string? osType, string? runtimeVersion, string? sku)
    {
        // Arrange
        var subscription = "sub123";
        var resourceGroup = "rg1";
        var appName = "test-app";
        var location = "eastus";

        var expectedResult = new WebappCreateResult(
            appName, "/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.Web/sites/test-app",
            location, "Running", "test-app.azurewebsites.net", "app,linux",
            $"{appName}-plan", $"{runtime}|{runtimeVersion ?? "latest"}", osType ?? "linux", "Succeeded");

        _appServiceService.CreateWebAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        List<string> unparsedArgs =
        [
            "--subscription", subscription,
            "--resource-group", resourceGroup,
            "--app", appName,
            "--location", location,
            "--runtime", runtime
        ];

        if (!string.IsNullOrEmpty(osType))
        {
            unparsedArgs.AddRange(["--os-type", osType]);
        }
        if (!string.IsNullOrEmpty(runtimeVersion))
        {
            unparsedArgs.AddRange(["--runtime-version", runtimeVersion]);
        }
        if (!string.IsNullOrEmpty(sku))
        {
            unparsedArgs.AddRange(["--sku", sku]);
        }

        var args = _commandDefinition.Parse(unparsedArgs);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        await _appServiceService.Received(1).CreateWebAppAsync(
            appName, resourceGroup, subscription, location, runtime,
            sku, runtimeVersion, osType,
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>());

        Assert.NotNull(response);
        Assert.NotNull(response.Results);
    }

    [Fact]
    public async Task ExecuteAsync_MissingRuntime_ReturnsErrorResponse()
    {
        // Arrange - runtime is the only truly required option
        var args = _commandDefinition.Parse([
            "--subscription", "sub123",
            "--resource-group", "rg1",
            "--app", "test-app",
            "--location", "eastus"
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);

        await _appServiceService.DidNotReceive().CreateWebAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithOnlyRuntime_UsesDefaults()
    {
        // Arrange - only runtime specified, everything else should default
        var expectedResult = new WebappCreateResult(
            "webapp-auto", "id", "canadacentral", "Running", "webapp-auto.azurewebsites.net",
            "app,linux", "webapp-auto-plan", "python|3.14", "linux", "Succeeded");

        _appServiceService.CreateWebAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var args = _commandDefinition.Parse(["--runtime", "python"]);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);

        // Verify that auto-generated app name, resource group, and canadacentral location were used
        await _appServiceService.Received(1).CreateWebAppAsync(
            Arg.Is<string>(s => s.StartsWith("webapp-")),          // auto-generated app name
            Arg.Is<string>(s => s.EndsWith("-rg")),                // auto-generated resource group
            Arg.Any<string>(),                                      // subscription
            "canadacentral",                                        // default location
            "python",                                               // runtime as specified
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DotnetWithoutOsType_ReturnsValidationError()
    {
        // Arrange
        var args = _commandDefinition.Parse([
            "--subscription", "sub123",
            "--resource-group", "rg1",
            "--app", "test-app",
            "--location", "eastus",
            "--runtime", "dotnet"
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);

        await _appServiceService.DidNotReceive().CreateWebAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PythonOnWindows_ReturnsValidationError()
    {
        // Arrange
        var args = _commandDefinition.Parse([
            "--subscription", "sub123",
            "--resource-group", "rg1",
            "--app", "test-app",
            "--location", "eastus",
            "--runtime", "python",
            "--os-type", "windows"
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceThrowsException_ReturnsErrorResponse()
    {
        // Arrange
        _appServiceService.CreateWebAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Service error"));

        var args = _commandDefinition.Parse([
            "--subscription", "sub123",
            "--resource-group", "rg1",
            "--app", "test-app",
            "--location", "eastus",
            "--runtime", "node"
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);

        await _appServiceService.Received(1).CreateWebAppAsync(
            "test-app", "rg1", "sub123", "eastus", "node",
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomPlanAndSku_PassesValuesToService()
    {
        // Arrange
        var expectedResult = new WebappCreateResult(
            "test-app", "/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.Web/sites/test-app",
            "westus", "Running", "test-app.azurewebsites.net", "app,linux",
            "my-custom-plan", "node|24-lts", "linux", "Succeeded");

        _appServiceService.CreateWebAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var args = _commandDefinition.Parse([
            "--subscription", "sub123",
            "--resource-group", "rg1",
            "--app", "test-app",
            "--location", "westus",
            "--runtime", "node",
            "--sku", "F1",
            "--plan", "my-custom-plan"
        ]);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        await _appServiceService.Received(1).CreateWebAppAsync(
            "test-app", "rg1", "sub123", "westus", "node",
            "F1", Arg.Any<string?>(), Arg.Any<string?>(), "my-custom-plan",
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>());

        Assert.NotNull(response);
        Assert.NotNull(response.Results);
    }
}
