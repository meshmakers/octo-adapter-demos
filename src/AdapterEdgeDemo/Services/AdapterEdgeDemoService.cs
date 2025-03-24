using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Services;

/// <summary>
///  This is the main service for the plug. It is responsible for starting and stopping the plug.
///  Shutdown is called when the plug is stopped or new configuration is received.
///  Startup is called when the plug is started or new configuration is received.
/// </summary>
/// <param name="pipelineRegistryService">Service for registering and starting pipelines</param>
/// <param name="eventHubControl">Event hub control service</param>
public class AdapterEdgeDemoService(
    IPipelineRegistryService pipelineRegistryService,
    IEventHubControl eventHubControl)
    : IAdapterService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<bool> StartupAsync(AdapterStartup adapterStartup, List<DeploymentUpdateErrorMessageDto> errorMessages, CancellationToken stoppingToken)
    {
        Logger.Info("Startup");

        try
        {
            // adapterStartup contains configuration:
            // Adapter configuration (optional) and a list of pipelines to be executed by this adapter.
            // Pipelines are a sequence of nodes that process data.
            // The pipeline is registered with the pipeline registry service.

            // Register pipelines
            var success = await pipelineRegistryService.RegisterPipelinesAsync(adapterStartup.TenantId,
                adapterStartup.Configuration.Pipelines, errorMessages);

            // if success is false, at least one pipeline failed to register, and the errorMessages list contains the error messages.
            // The adapter should start to execute the rest of the pipelines.

            // Start triggers. Triggers are special nodes that start the pipeline execution based on some event.
            await pipelineRegistryService.StartTriggerPipelineNodesAsync(adapterStartup.TenantId);
            
            // Start connection to rabbitmq event hub
            await eventHubControl.StartAsync(stoppingToken);

            return success;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while startup");
            throw;
        }
    }
    
    public async Task ShutdownAsync(AdapterShutdown adapterShutdown, CancellationToken stoppingToken)
    {
        try
        {
            Logger.Info("Shutdown");

            // Stop triggers
            await pipelineRegistryService.StopTriggerPipelineNodesAsync(adapterShutdown.TenantId);

            // Unregister pipelines
            pipelineRegistryService.UnregisterAllPipelines(adapterShutdown.TenantId);
            
            // Stop connection rabbitmq event hub
            await eventHubControl.StopAsync(stoppingToken);

            Logger.Info("Shutdown complete");
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error while shutdown");
            throw;
        }
    }
}