using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Nodes;

/// <summary>
/// Configuration for DemoNode
/// </summary>
[NodeName("Demo", 1)]
public record
    DemoNodeConfiguration : SourceTargetPathNodeConfiguration // Alternatives are:
                                                        // TargetPathNodeConfiguration (defines TargetPath and options),
                                                        // PathNodeConfiguration (defines Path),
                                                        // SourceTargetPathNodeConfiguration (defines Path + TargetPath and options)
{
    /// <summary>
    /// Message that is written to output for demonstration
    /// </summary>
    public required string MyMessage { get; set; } = "Hello, World!";
}

/// <summary>
/// Implements a simple demonstration node
/// </summary>
/// <param name="next">Delegate to the next node</param>
[NodeConfiguration(typeof(DemoNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class DemoNode(
    NodeDelegate next /* IConnectionManagerService connectionManagerService // you can inject services here */)
    : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        // Get configuration
        var c = nodeContext.GetNodeConfiguration<DemoNodeConfiguration>();

        nodeContext.Info("DemoNode: " + c.MyMessage);
        
        var pathData = dataContext.Current?.SelectToken(c.Path) as JValue;
        var input = pathData?.Value<string>();
        
        var output = c.MyMessage + input;

        
        // set value
        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            output);

        // Continue with next node in pipeline
        await next(dataContext, nodeContext);
    }
}