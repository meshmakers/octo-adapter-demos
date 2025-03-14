using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using NLog;

namespace Meshmakers.Octo.Communication.Plugs.Demo.Services;

/// <summary>
///  This is the main service for the plug. It is responsible for starting and stopping the plug.
///  Shutdown is called when the plug is stopped or new configuration is received.
///  Startup is called when the plug is started or new configuration is received.
/// </summary>
/// <param name="pipelineRegistryService">Service for registering and starting pipelines</param>
/// <param name="eventHubControl">Event hub control service</param>
public class DemoPlugService(
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
            if (adapterStartup.Configuration.AdapterConfiguration == null)
            {
                throw new Exception("No configuration received");
            }

            // Register pipelines
            var success = await pipelineRegistryService.RegisterPipelinesAsync(adapterStartup.TenantId,
                adapterStartup.Configuration.Pipelines, errorMessages);
            
            // Start triggers
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