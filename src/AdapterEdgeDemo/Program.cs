using Meshmakers.Octo.Communication.EdgeAdapter.Demo.Nodes;
using Meshmakers.Octo.Communication.EdgeAdapter.Demo.Services;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.Services;

// AdapterBuilder is used for active adapters (connecting to external systems)
var adapterBuilder = new AdapterBuilder();

adapterBuilder.Run(args, (_, services) =>
{
    // Register data pipeline services
    services.AddDataPipeline()
        .RegisterNode<DemoNode>() // Sample to register a node
        .RegisterTriggerNode<DemoTriggerNode>() // Sample to register a trigger node
        // Register the ETL context to access meta data of the execution (e.g. tenant id, pipeline id, ...)
        .RegisterEtlContext<IEtlContext>();
    services.AddTransient<IPollingService, PollingService>();
    services.AddSingleton<IAdapterService, AdapterEdgeDemoService>();
});

