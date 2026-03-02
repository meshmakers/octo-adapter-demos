using System.Net.Sockets;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Meshmakers.Octo.Communication.MeshAdapter.Demo.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Meshmakers.Octo.Communication.AdapterDemos.IntegrationTests.Nodes;

/// <summary>
/// Integration tests for the Mesh DemoTriggerNode using actual TCP connections.
/// </summary>
public class MeshDemoTriggerNodeIntegrationTests : IAsyncLifetime
{
    private readonly ITriggerContext _triggerContext;
    private readonly INodeContext _nodeContext;
    private readonly DemoTriggerNode _sut;
    private int _actualPort;

    public MeshDemoTriggerNodeIntegrationTests()
    {
        _triggerContext = A.Fake<ITriggerContext>();
        _nodeContext = A.Fake<INodeContext>();
        A.CallTo(() => _triggerContext.NodeContext).Returns(_nodeContext);
        _sut = new DemoTriggerNode();
    }

    public async Task InitializeAsync()
    {
        _actualPort = GetFreePort();
        var config = new DemoTriggerNodeConfiguration { Port = (ushort)_actualPort };
        A.CallTo(() => _nodeContext.GetNodeConfiguration<DemoTriggerNodeConfiguration>()).Returns(config);

        A.CallTo(() => _triggerContext.ExecuteAsync(
            A<ExecutePipelineOptions>._,
            A<object?>._))
            .ReturnsLazily((ExecutePipelineOptions _, object? input) => Task.FromResult(input));

        await _sut.StartAsync(_triggerContext);
        await Task.Delay(200);
    }

    public async Task DisposeAsync()
    {
        await _sut.StopAsync(_triggerContext);
    }

    [Fact]
    public async Task TriggerNode_AcceptsTcpConnection_AndProcessesJsonMessage()
    {
        // Arrange & Act
        var response = await SendTcpMessageAsync("{\"device\": \"sensor-01\", \"temp\": 23.5}");

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        var responseJson = JToken.Parse(response!);
        responseJson["device"]!.Value<string>().Should().Be("sensor-01");
        responseJson["temp"]!.Value<double>().Should().Be(23.5);
    }

    [Fact]
    public async Task TriggerNode_SetsExecutePipelineOptions_WithTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        await SendTcpMessageAsync("{\"data\": true}");
        await Task.Delay(100);

        var after = DateTime.UtcNow;

        // Assert
        A.CallTo(() => _triggerContext.ExecuteAsync(
            A<ExecutePipelineOptions>.That.Matches(o =>
                o.TransactionStartedDateTime >= before &&
                o.TransactionStartedDateTime <= after &&
                o.ExternalReceivedDateTime != null),
            A<object?>._))
            .MustHaveHappened();
    }

    private async Task<string?> SendTcpMessageAsync(string message)
    {
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", _actualPort);

        await using var stream = client.GetStream();
        stream.ReadTimeout = 5000;

        var messageBytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(messageBytes);

        client.Client.Shutdown(SocketShutdown.Send);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[4096];
        var responseBuilder = new StringBuilder();

        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cts.Token)) > 0)
            {
                responseBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout
        }

        var response = responseBuilder.ToString().Trim();
        return string.IsNullOrEmpty(response) ? null : response;
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
