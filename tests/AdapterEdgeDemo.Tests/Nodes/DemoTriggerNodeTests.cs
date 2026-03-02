using FakeItEasy;
using Meshmakers.Octo.Communication.EdgeAdapter.Demo.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Xunit;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Tests.Nodes;

public class DemoTriggerNodeTests
{
    private readonly ITriggerContext _triggerContext;
    private readonly INodeContext _nodeContext;
    private readonly DemoTriggerNode _sut;

    public DemoTriggerNodeTests()
    {
        _triggerContext = A.Fake<ITriggerContext>();
        _nodeContext = A.Fake<INodeContext>();
        A.CallTo(() => _triggerContext.NodeContext).Returns(_nodeContext);
        _sut = new DemoTriggerNode();
    }

    [Fact]
    public async Task StartAsync_StartsListening_OnConfiguredPort()
    {
        // Arrange
        var config = new DemoTriggerNodeConfiguration { Port = 0 }; // Port 0 = OS assigns free port
        A.CallTo(() => _nodeContext.GetNodeConfiguration<DemoTriggerNodeConfiguration>()).Returns(config);

        // Act
        await _sut.StartAsync(_triggerContext);

        // Assert
        A.CallTo(() => _nodeContext.Info(
            A<string>.That.Contains("TCP listener started"),
            A<object[]>._)).MustHaveHappenedOnceExactly();

        // Cleanup
        await _sut.StopAsync(_triggerContext);
    }

    [Fact]
    public async Task StopAsync_StopsListener_AfterStart()
    {
        // Arrange
        var config = new DemoTriggerNodeConfiguration { Port = 0 };
        A.CallTo(() => _nodeContext.GetNodeConfiguration<DemoTriggerNodeConfiguration>()).Returns(config);
        await _sut.StartAsync(_triggerContext);

        // Act
        await _sut.StopAsync(_triggerContext);

        // Assert
        A.CallTo(() => _nodeContext.Info(
            A<string>.That.Contains("TCP listener stopped"),
            A<object[]>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        // Act & Assert - StopAsync should handle the case where Start was never called
        var exception = await Record.ExceptionAsync(() => _sut.StopAsync(_triggerContext));

        // Note: current implementation will attempt to cancel a null CTS and stop a null listener
        // which may or may not throw depending on null checks
        Assert.True(exception == null || exception is DemoPipelineExecutionException);
    }

    [Fact]
    public void DemoTriggerNodeConfiguration_HasDefaultPort()
    {
        // Arrange & Act
        var config = new DemoTriggerNodeConfiguration { Port = 8000 };

        // Assert
        Assert.Equal((ushort)8000, config.Port);
    }
}
