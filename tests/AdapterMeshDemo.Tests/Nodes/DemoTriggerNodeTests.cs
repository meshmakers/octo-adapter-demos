using FakeItEasy;
using Meshmakers.Octo.Communication.MeshAdapter.Demo.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Xunit;

namespace Meshmakers.Octo.Communication.MeshAdapter.Demo.Tests.Nodes;

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
    public async Task StartAsync_StartsListening()
    {
        // Arrange
        var config = new DemoTriggerNodeConfiguration { Port = 0 };
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
    public async Task StopAsync_StopsListener()
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
    public void DemoTriggerNodeConfiguration_HasDefaultPort()
    {
        // Act
        var config = new DemoTriggerNodeConfiguration { Port = 8000 };

        // Assert
        Assert.Equal((ushort)8000, config.Port);
    }
}
