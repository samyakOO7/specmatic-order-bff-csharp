using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace specmatic_order_bff_csharp.test.contract;

public class ContractTests : IAsyncLifetime
{
    private IContainer _stubContainer, _testContainer;
    private Process _appProcess;
    private static readonly string Pwd = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName ?? string.Empty;
    private readonly string _projectPath = Directory.GetParent(Pwd)?.FullName ?? string.Empty;
    private const string ProjectName = "specmatic-order-bff-csharp/specmatic-order-bff-csharp.csproj";
    private const string TestContainerDirectory = "/usr/src/app";

    [Fact]
    public async Task ContractTestsAsync()
    {
        var readBytes = await _testContainer
            .ReadFileAsync(TestContainerDirectory+ "/build/reports/specmatic/html/index.html");
        var file = new FileInfo(Pwd + "/reports/specmaticReport.html");
        file.Directory?.Create();
        await File.WriteAllBytesAsync(file.FullName, readBytes);
    }

    public async Task InitializeAsync()
    {
        await TestcontainersSettings.ExposeHostPortsAsync(8080)
            .ConfigureAwait(false);

        StartOrderBffService();
        await StartDomainServiceStub();
        await RunContractTests();
    }

    private async Task RunContractTests()
    { 
        _testContainer = new ContainerBuilder()
            .WithImage("znsio/specmatic").WithCommand("test")
            .WithCommand("--port=8080")
            .WithCommand("--host=host.docker.internal")
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .WithPortBinding(8090)
            .WithExposedPort(8090)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Tests run:"))
            .WithBindMount(
                $"{Pwd}/specmatic.yaml",
                $"{TestContainerDirectory}/specmatic.yaml").Build();
         
        await _testContainer.StartAsync().ConfigureAwait(true);
    }

    private void StartOrderBffService()
    {
        _appProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project {_projectPath}/{ProjectName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _appProcess.Start();
    }

    private async Task StartDomainServiceStub()
    {
        _stubContainer = new ContainerBuilder()
            .WithImage("znsio/specmatic").WithCommand("stub")
            .WithCommand("--examples=examples")
            .WithPortBinding(9000)
            .WithExposedPort(9000)
            .WithReuse(true)
            .WithBindMount($"{Pwd}/examples/domain_service", $"{TestContainerDirectory}/examples")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9000))
            .WithBindMount(
                $"{Pwd}/specmatic.yaml",
                $"{TestContainerDirectory}/specmatic.yaml").Build();
        
        await _stubContainer.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _stubContainer.DisposeAsync();
        await _testContainer.DisposeAsync();
        if (!_appProcess.HasExited)
        {
            _appProcess.Kill();
            await _appProcess.WaitForExitAsync();
            _appProcess.Dispose();
        }
    }
}