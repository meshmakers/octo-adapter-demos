using Meshmakers.Octo.Common.DistributionEventHub.Services;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Meshmakers.Octo.Communication.MeshAdapter.Demo.Services;

internal class AdapterMeshDemoService(
    ILogger<AdapterMeshDemoService> logger,
    IPipelineRegistryService pipelineRegistryService,
    IEventHubControl eventHubControl) : IAdapterService
{
    public async Task<bool> StartupAsync(AdapterStartup adapterStartup, List<DeploymentUpdateErrorMessageDto> errorMessages,
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Startup of mesh adapter");
        try
        {
            var success = await pipelineRegistryService.RegisterPipelinesAsync(adapterStartup.TenantId,
                adapterStartup.Configuration.Pipelines, errorMessages);
            await pipelineRegistryService.StartTriggerPipelineNodesAsync(adapterStartup.TenantId);

            await eventHubControl.StartAsync(stoppingToken);

            var conf = adapterStartup.Configuration;
            conf.Pipelines.Where(p =>
            {
                logger.LogInformation("Pipeline {NodeConfiguration} registered", p.NodeConfiguration);
                return false;
            });

            var adapterConf = conf.AdapterConfiguration;
            logger.LogInformation("Adapter configuration: {AdapterConfiguration}", adapterConf);
            
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
            logger.LogInformation("Shutdown of mesh adapter");
            await pipelineRegistryService.StopTriggerPipelineNodesAsync(adapterShutdown.TenantId);
            
            pipelineRegistryService.UnregisterAllPipelines(adapterShutdown.TenantId);
            await eventHubControl.StopAsync(stoppingToken);
            logger.LogInformation("Mesh Adapter service stopped");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while shutdown");
            throw;
        }
    }
}