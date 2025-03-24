using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo;

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

    public static Exception PipelineExecutionFailed(Exception exception)
    {
        return new DemoPipelineExecutionException("Execution of pipeline failed", exception);
    }

    public static Exception MessageDeserializationFailed(JsonReaderException jsonReaderException)
    {
        return new DemoPipelineExecutionException("Message deserialization failed", jsonReaderException);
    }
}

