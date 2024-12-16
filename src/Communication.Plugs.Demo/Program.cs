using Meshmakers.Octo.Communication.Plugs.Demo.Nodes;
using Meshmakers.Octo.Communication.Plugs.Demo.Services;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.Services;

var adapterBuilder = new AdapterBuilder();

adapterBuilder.Run(args, (_, services) =>
{
    services.AddDataPipeline()
        .RegisterNode<DemoNode>() // Sample to register a node
        .RegisterTriggerNode<DemoTriggerNode>() // Sample to register a trigger node
        .RegisterEtlContext<IEtlContext>();
    services.AddTransient<IPollingService, PollingService>();
    services.AddSingleton<IAdapterService, DemoPlugService>();
});

