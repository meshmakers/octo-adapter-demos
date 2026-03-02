using Newtonsoft.Json;
using Xunit;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Tests;

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
    public void MessageDeserializationFailed_ReturnsExceptionWithJsonReaderException()
    {
        // Arrange
        var jsonEx = new JsonReaderException("bad json");

        // Act
        var result = DemoPipelineExecutionException.MessageDeserializationFailed(jsonEx);

        // Assert
        Assert.IsType<DemoPipelineExecutionException>(result);
        Assert.Equal("Message deserialization failed", result.Message);
        Assert.Same(jsonEx, result.InnerException);
    }

    [Fact]
    public void DefaultConstructor_CreatesException()
    {
        // Act
        var ex = new DemoPipelineExecutionException();

        // Assert
        Assert.NotNull(ex);
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        // Act
        var ex = new DemoPipelineExecutionException("custom message");

        // Assert
        Assert.Equal("custom message", ex.Message);
    }

    [Fact]
    public void MessageAndInnerConstructor_SetsBoth()
    {
        // Arrange
        var inner = new Exception("inner");

        // Act
        var ex = new DemoPipelineExecutionException("outer", inner);

        // Assert
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
