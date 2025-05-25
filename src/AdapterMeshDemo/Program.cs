using Meshmakers.Octo.Communication.MeshAdapter.Demo.Nodes;
using Meshmakers.Octo.Communication.MeshAdapter.Demo.Services;
using Meshmakers.Octo.Runtime.Contracts.MongoDb.Configuration;
using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using Meshmakers.Octo.Sdk.Common.Web.Sockets;
using Meshmakers.Octo.Sdk.MeshAdapter.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// WebAdapterBuilder is a builder for creating Adapters acting as a Socket (Listener) or a Web API (Host)
var adapterBuilder = new WebAdapterBuilder();

await adapterBuilder.RunAsync(args, builder =>
{
    // Define the configuration for the adapter
    builder.Services.Configure<OctoSystemConfiguration>(options =>
        builder.Configuration.GetSection("System").Bind(options));

    builder.Services.Configure<MeshAdapterConfiguration>(options =>
        builder.Configuration.GetSection("Adapter").Bind(options));

    builder.Services.AddSingleton<IPollingService, PollingService>();
    
    // Add services to the container.

    // Add the adapter service to startup and shutdown the adapter
    builder.Services.AddSingleton<IAdapterService, AdapterMeshDemoService>();

    // Add mesh adapter nodes and services to the container
    builder.Services.AddOctoMeshAdapter()
        .RegisterNode<DemoNode>() // Sample to register a node
        .RegisterTriggerNode<DemoTriggerNode>(); // Sample to register a trigger node

}, app =>
{
    app.UseOctoMeshAdapter();
});