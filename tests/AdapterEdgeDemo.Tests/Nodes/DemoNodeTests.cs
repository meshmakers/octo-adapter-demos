using FakeItEasy;
using Meshmakers.Octo.Communication.EdgeAdapter.Demo.Nodes;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Xunit;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Tests.Nodes;

public class DemoNodeTests
{
    private readonly IDataContext _dataContext;
    private readonly INodeContext _nodeContext;
    private readonly NodeDelegate _next;
    private readonly DemoNode _sut;

    public DemoNodeTests()
    {
        _dataContext = A.Fake<IDataContext>();
        _nodeContext = A.Fake<INodeContext>();
        _next = A.Fake<NodeDelegate>();
        _sut = new DemoNode(_next);
    }

    [Fact]
    public async Task ProcessObjectAsync_SetsValueByPath_WithConfiguredMessage()
    {
        // Arrange
        var config = new DemoNodeConfiguration { MyMessage = "Test Message" };
        A.CallTo(() => _nodeContext.GetNodeConfiguration<DemoNodeConfiguration>()).Returns(config);

        // Act
        await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

        // Assert
        A.CallTo(() => _dataContext.Set(
            config.TargetPath,
            "Test Message",
            config.DocumentMode,
            config.TargetValueKind,
            config.TargetValueWriteMode)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_CallsNextDelegate()
    {
        // Arrange
        var config = new DemoNodeConfiguration { MyMessage = "Hello" };
        A.CallTo(() => _nodeContext.GetNodeConfiguration<DemoNodeConfiguration>()).Returns(config);

        // Act
        await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

        // Assert
        A.CallTo(() => _next(_dataContext, _nodeContext)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_LogsInfoMessage()
    {
        // Arrange
        var config = new DemoNodeConfiguration { MyMessage = "Log Test" };
        A.CallTo(() => _nodeContext.GetNodeConfiguration<DemoNodeConfiguration>()).Returns(config);

        // Act
        await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

        // Assert
        A.CallTo(() => _nodeContext.Info(
            A<string>.That.Contains("Log Test"),
            A<object[]>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessObjectAsync_UsesDefaultMessage_WhenNotConfigured()
    {
        // Arrange
        var config = new DemoNodeConfiguration { MyMessage = "Hello, World!" };
        A.CallTo(() => _nodeContext.GetNodeConfiguration<DemoNodeConfiguration>()).Returns(config);

        // Act
        await _sut.ProcessObjectAsync(_dataContext, _nodeContext);

        // Assert
        A.CallTo(() => _dataContext.Set(
            config.TargetPath,
            "Hello, World!",
            config.DocumentMode,
            config.TargetValueKind,
            config.TargetValueWriteMode)).MustHaveHappenedOnceExactly();
    }
}
