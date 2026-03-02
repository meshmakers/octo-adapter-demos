using System.Net.Sockets;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Meshmakers.Octo.Communication.EdgeAdapter.Demo.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Meshmakers.Octo.Communication.AdapterDemos.IntegrationTests.Nodes;

/// <summary>
/// Integration tests for the Edge DemoTriggerNode using actual TCP connections.
/// </summary>
public class EdgeDemoTriggerNodeIntegrationTests : IAsyncLifetime
{
    private readonly ITriggerContext _triggerContext;
    private readonly INodeContext _nodeContext;
    private readonly DemoTriggerNode _sut;
    private int _actualPort;

    public EdgeDemoTriggerNodeIntegrationTests()
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

        // Configure ExecuteAsync to return the input as output (echo)
        A.CallTo(() => _triggerContext.ExecuteAsync(
            A<ExecutePipelineOptions>._,
            A<object?>._))
            .ReturnsLazily((ExecutePipelineOptions _, object? input) => Task.FromResult(input));

        await _sut.StartAsync(_triggerContext);
        // Give the listener time to start accepting connections
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
        var response = await SendTcpMessageAsync("{\"Key\": \"Value\"}");

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        var responseJson = JToken.Parse(response!);
        responseJson["Key"]!.Value<string>().Should().Be("Value");
    }

    [Fact]
    public async Task TriggerNode_CallsExecuteAsync()
    {
        // Arrange & Act
        await SendTcpMessageAsync("{\"sensor\": \"temp\"}");

        // Allow async processing to complete
        await Task.Delay(100);

        // Assert
        A.CallTo(() => _triggerContext.ExecuteAsync(
            A<ExecutePipelineOptions>._,
            A<object?>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task TriggerNode_HandlesMultipleSequentialConnections()
    {
        // Act
        var response1 = await SendTcpMessageAsync("{\"seq\": 1}");
        var response2 = await SendTcpMessageAsync("{\"seq\": 2}");

        // Assert
        response1.Should().NotBeNullOrWhiteSpace();
        response2.Should().NotBeNullOrWhiteSpace();

        var json1 = JToken.Parse(response1!);
        var json2 = JToken.Parse(response2!);
        json1["seq"]!.Value<int>().Should().Be(1);
        json2["seq"]!.Value<int>().Should().Be(2);
    }

    private async Task<string?> SendTcpMessageAsync(string message)
    {
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", _actualPort);

        await using var stream = client.GetStream();
        stream.ReadTimeout = 5000;

        // Send the message
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(messageBytes);

        // Signal end of send so the server knows we're done
        client.Client.Shutdown(SocketShutdown.Send);

        // Read response with timeout
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
            // Timeout - return what we have
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
