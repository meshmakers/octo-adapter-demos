using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Communication.Plugs.Demo.Nodes;

/// <summary>
/// Configuration for DemoNode
/// </summary>
[NodeName("Demo", 1)]
public record DemoNodeConfiguration : SourceTargetPathNodeConfiguration // Alternatives  are: TargetPathNodeConfiguration (defines TargetPath and options), PathNodeConfiguration (defines Path), SourceTargetPathNodeConfiguration (defines Path + TargetPath and options)
{
    /// <summary>
    /// Message that is written to output for demonstration
    /// </summary>
    public required string MyMessage { get; set; }= "Hello, World!";
}

/// <summary>
/// Implements a simple demonstration node
/// </summary>
/// <param name="next">Delegate to the next node</param>
[NodeConfiguration(typeof(DemoNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class DemoNode(NodeDelegate next /* IConnectionManagerService connectionManagerService // you can inject services here */)
    : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        // Get configuration
        var c = dataContext.NodeContext.GetNodeConfiguration<DemoNodeConfiguration>();

        // set value
        dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, c.MyMessage);

        // Continue with next node in pipeline
        await next(dataContext);
    }
}