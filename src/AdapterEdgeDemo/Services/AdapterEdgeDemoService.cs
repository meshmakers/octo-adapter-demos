using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Services;

/// <summary>
///  This is the main service for the plug. It is responsible for starting and stopping the plug.
///  Shutdown is called when the plug is stopped or new configuration is received.
///  Startup is called when the plug starts or receives a new configuration.
/// </summary>
/// <param name="pipelineRegistryService">Service for registering and starting pipelines</param>
/// <param name="eventHubControl">Event hub control service</param>
public class AdapterEdgeDemoService(
    ILogger<AdapterEdgeDemoService> logger,
    IPipelineRegistryService pipelineRegistryService,
    IEventHubControl eventHubControl)
    : IAdapterService
{
    public async Task<bool> StartupAsync(AdapterStartup adapterStartup, List<DeploymentUpdateErrorMessageDto> errorMessages, CancellationToken stoppingToken)
    {
        logger.LogInformation("Startup");

        try
        {
            // adapterStartup contains configuration:
            // Adapter configuration (optional) and a list of pipelines to be executed by this adapter.
            // Pipelines are a sequence of nodes that process data.
            // The pipeline is registered with the pipeline registry service.

            // Register pipelines
            var success = await pipelineRegistryService.RegisterPipelinesAsync(adapterStartup.TenantId,
                adapterStartup.Configuration.Pipelines, errorMessages);

            // If success is false, at least one pipeline failed to register, and the errorMessages list contains the error messages.
            // The adapter should start to execute the rest of the pipelines.

            // Start triggers. Triggers are special nodes that start the pipeline execution based on some event.
            await pipelineRegistryService.StartTriggerPipelineNodesAsync(adapterStartup.TenantId);
            
            // Start connection to rabbitmq event hub
            await eventHubControl.StartAsync(stoppingToken);

            return success;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while startup");
            throw;
        }
    }
    
    public async Task ShutdownAsync(AdapterShutdown adapterShutdown, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Shutdown");

            // Stop triggers
            await pipelineRegistryService.StopTriggerPipelineNodesAsync(adapterShutdown.TenantId);

            // Unregister pipelines
            pipelineRegistryService.UnregisterAllPipelines(adapterShutdown.TenantId);
            
            // Stop connection rabbitmq event hub
            await eventHubControl.StopAsync(stoppingToken);

            logger.LogInformation("Shutdown complete");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while shutdown");
            throw;
        }
    }
}