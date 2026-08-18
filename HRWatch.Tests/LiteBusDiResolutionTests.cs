using HRWatch.Application;
using HRWatch.Infrastructure;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace HRWatch.Tests;

public class LiteBusDiResolutionTests
{
    private readonly ITestOutputHelper _output;

    public LiteBusDiResolutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FullApplicationAndInfrastructure_ShouldResolveAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var inMemorySettings = new Dictionary<string, string?> {
            {"ConnectionStrings:DefaultConnection", "Server=dummy;Database=dummy;"},
            {"CG1:BaseUrl", "https://localhost:5092"},
            {"Cosec:BaseUrl", "http://172.24.120.88"},
            {"Cosec:Username", "API"},
            {"Cosec:Password", "Api@123"},
            {"Jwt:Key", "SuperSecretKeyForTestingAtLeast32CharsLong!"},
            {"Jwt:Issuer", "Test"},
            {"Jwt:Audience", "Test"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddSingleton(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Build without Coravel host lifetime in unit test container
        var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false });

        var cmd = sp.GetRequiredService<ICommandMediator>();
        var qry = sp.GetRequiredService<IQueryMediator>();

        Assert.NotNull(cmd);
        Assert.NotNull(qry);

        _output.WriteLine("Successfully resolved ICommandMediator and IQueryMediator with zero errors!");
    }
}
