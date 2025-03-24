using Meshmakers.Octo.Sdk.Common.Services;

namespace Meshmakers.Octo.Communication.MeshAdapter.Demo;

public class DemoPipelineExecutionException : PipelineExecutionException
{
    public DemoPipelineExecutionException()
    {
    }

    public DemoPipelineExecutionException(string message) : base(message)
    {
    }

    public DemoPipelineExecutionException(string message, Exception inner) : base(message, inner)
    {
    }

    public static Exception StopFailed(Exception exception)
    {
        return new DemoPipelineExecutionException("Stop failed", exception);
    }

    public static Exception ProcessFailed(Exception exception)
    {
        return new DemoPipelineExecutionException("Process failed", exception);
    }
}

