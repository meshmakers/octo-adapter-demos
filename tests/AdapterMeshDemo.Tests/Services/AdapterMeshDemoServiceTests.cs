using FakeItEasy;
using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.MeshAdapter.Demo.Services;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Meshmakers.Octo.Communication.MeshAdapter.Demo.Tests.Services;

public class AdapterMeshDemoServiceTests
{
    private readonly IPipelineRegistryService _pipelineRegistryService;
    private readonly IEventHubControl _eventHubControl;
    private readonly AdapterMeshDemoService _sut;

    public AdapterMeshDemoServiceTests()
    {
        _pipelineRegistryService = A.Fake<IPipelineRegistryService>();
        _eventHubControl = A.Fake<IEventHubControl>();
        _sut = new AdapterMeshDemoService(
            NullLogger<AdapterMeshDemoService>.Instance,
            _pipelineRegistryService,
            _eventHubControl);
    }

    [Fact]
    public async Task StartupAsync_RegistersPipelines_AndStartsEventHub()
    {
        // Arrange
        var pipelines = new List<PipelineConfigurationDto>();
        var config = new AdapterConfigurationDto(new RtEntityId("System.Communication/MeshAdapter@6760711ec4ff02221e0b532e"), null, pipelines);
        var startup = new AdapterStartup { TenantId = "test-tenant", Configuration = config };
        var errorMessages = new List<DeploymentUpdateErrorMessageDto>();
        A.CallTo(() => _pipelineRegistryService.RegisterPipelinesAsync(
            "test-tenant", pipelines, errorMessages)).Returns(true);

        // Act
        var result = await _sut.StartupAsync(startup, errorMessages, CancellationToken.None);

        // Assert
        Assert.True(result);
        A.CallTo(() => _pipelineRegistryService.RegisterPipelinesAsync(
            "test-tenant", pipelines, errorMessages)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _eventHubControl.StartAsync(CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task StartupAsync_ReturnsFalse_WhenPipelineRegistrationFails()
    {
        // Arrange
        var pipelines = new List<PipelineConfigurationDto>();
        var config = new AdapterConfigurationDto(new RtEntityId("System.Communication/MeshAdapter@6760711ec4ff02221e0b532e"), null, pipelines);
        var startup = new AdapterStartup { TenantId = "test-tenant", Configuration = config };
        var errorMessages = new List<DeploymentUpdateErrorMessageDto>();
        A.CallTo(() => _pipelineRegistryService.RegisterPipelinesAsync(
            "test-tenant", pipelines, errorMessages)).Returns(false);

        // Act
        var result = await _sut.StartupAsync(startup, errorMessages, CancellationToken.None);

        // Assert
        Assert.False(result);
        A.CallTo(() => _eventHubControl.StartAsync(CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShutdownAsync_StopsEventHub_AndUnregistersAllPipelines()
    {
        // Arrange
        var shutdown = new AdapterShutdown { TenantId = "test-tenant" };

        // Act
        await _sut.ShutdownAsync(shutdown, CancellationToken.None);

        // Assert
        A.CallTo(() => _eventHubControl.StopAsync(CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _pipelineRegistryService.UnregisterAllPipelinesAsync("test-tenant")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task StartupAsync_ThrowsException_WhenEventHubFails()
    {
        // Arrange
        var pipelines = new List<PipelineConfigurationDto>();
        var config = new AdapterConfigurationDto(new RtEntityId("System.Communication/MeshAdapter@6760711ec4ff02221e0b532e"), null, pipelines);
        var startup = new AdapterStartup { TenantId = "test-tenant", Configuration = config };
        var errorMessages = new List<DeploymentUpdateErrorMessageDto>();
        A.CallTo(() => _pipelineRegistryService.RegisterPipelinesAsync(
            A<string>._, A<ICollection<PipelineConfigurationDto>>._, A<List<DeploymentUpdateErrorMessageDto>>._)).Returns(true);
        A.CallTo(() => _eventHubControl.StartAsync(A<CancellationToken>._))
            .ThrowsAsync(new InvalidOperationException("EventHub connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.StartupAsync(startup, errorMessages, CancellationToken.None));
    }

    [Fact]
    public async Task ShutdownAsync_ThrowsException_WhenEventHubStopFails()
    {
        // Arrange
        var shutdown = new AdapterShutdown { TenantId = "test-tenant" };
        A.CallTo(() => _eventHubControl.StopAsync(A<CancellationToken>._))
            .ThrowsAsync(new InvalidOperationException("EventHub stop failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ShutdownAsync(shutdown, CancellationToken.None));
    }
}
