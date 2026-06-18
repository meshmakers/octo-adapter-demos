using System.Text.Json;
using Xunit;

namespace Meshmakers.Octo.Communication.MeshAdapter.Demo.Tests;

public class DemoPipelineExecutionExceptionTests
{
    [Fact]
    public void StopFailed_ReturnsExceptionWithInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("inner error");

        // Act
        var result = DemoPipelineExecutionException.StopFailed(inner);

        // Assert
        Assert.IsType<DemoPipelineExecutionException>(result);
        Assert.Equal("Stop failed", result.Message);
        Assert.Same(inner, result.InnerException);
    }

    [Fact]
    public void PipelineExecutionFailed_ReturnsExceptionWithInnerException()
    {
        // Arrange
        var inner = new TimeoutException("timeout");

        // Act
        var result = DemoPipelineExecutionException.PipelineExecutionFailed(inner);

        // Assert
        Assert.IsType<DemoPipelineExecutionException>(result);
        Assert.Equal("Execution of pipeline failed", result.Message);
        Assert.Same(inner, result.InnerException);
    }

    [Fact]
    public void MessageDeserializationFailed_ReturnsExceptionWithJsonException()
    {
        // Arrange
        var jsonEx = new JsonException("bad json");

        // Act
        var result = DemoPipelineExecutionException.MessageDeserializationFailed(jsonEx);

        // Assert
        Assert.IsType<DemoPipelineExecutionException>(result);
        Assert.Equal("Message deserialization failed", result.Message);
        Assert.Same(jsonEx, result.InnerException);
    }
}
