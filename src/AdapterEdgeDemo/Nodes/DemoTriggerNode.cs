using System.Net;
using System.Net.Sockets;
using System.Text;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Communication.EdgeAdapter.Demo.Nodes;

/// <summary>
/// Configuration for DemoTriggerNode - a trigger node starts the execution of the transformation pipeline
/// </summary>
[NodeName("DemoTrigger", 1)]
// ReSharper disable once ClassNeverInstantiated.Global
public record DemoTriggerNodeConfiguration : TriggerNodeConfiguration
{
    /// <summary>
    /// Port the sample TCP listener listens to
    /// </summary>
    public required ushort Port { get; set; } = 8000;
}

/// <summary>
/// Implements a trigger node that listens to a TCP port and processes incoming messages
/// </summary>
[NodeConfiguration(typeof(DemoTriggerNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class DemoTriggerNode( /* you can inject services here */) : ITriggerPipelineNode
{
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cts;


    public Task StartAsync(ITriggerContext context)
    {
        var c = context.NodeContext.GetNodeConfiguration<DemoTriggerNodeConfiguration>();

        // Listen to TCP port for incoming messages at all interfaces
        _cts = new CancellationTokenSource();
        _tcpListener = new TcpListener(IPAddress.Any, c.Port);
        _tcpListener.Start();

        context.NodeContext.Info("TCP listener started and waiting for connections...");

        Task.Run(async () =>
        {
            // Wait for incoming connections
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    _ = ProcessClientAsync(tcpClient, context, _cts.Token); // Execute in a separate task
                }
                catch (ObjectDisposedException)
                {
                    // The Listener was stopped
                    break;
                }
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(ITriggerContext context)
    {
        try
        {
            _cts?.Cancel();
            _tcpListener?.Stop();
            context.NodeContext.Info("TCP listener stopped.");
        }
        catch (Exception ex)
        {
            context.NodeContext.Error($"Error stopping: {ex.Message}");
            throw DemoPipelineExecutionException.StopFailed(ex);
        }

        return Task.CompletedTask;
    }

    private async Task ProcessClientAsync(TcpClient client, ITriggerContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Read incoming messages from the client
            await using var networkStream = client.GetStream();
            var buffer = new byte[1024];
            int bytesRead;
            var messageBuilder = new StringBuilder();
            while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                context.NodeContext.Info("Client connected.");

                // Process the incoming message
                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (networkStream.DataAvailable)
                {
                    continue;
                }

                var message = messageBuilder.ToString();
                context.NodeContext.Info($"Received message: {message}");

                // Parse the incoming message as JSON and start the pipeline execution with the message as input
                var input = JToken.Parse(message);
                var output = await context.ExecuteAsync(
                    new ExecutePipelineOptions(DateTime.UtcNow)
                    {
                        ExternalReceivedDateTime = DateTime.UtcNow
                    },
                    input);

                // Serialize the output as JSON and send it back to the client
                var outputString = JsonConvert.SerializeObject(output);
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(outputString + Environment.NewLine);
                await networkStream.WriteAsync(utf8Bytes, cancellationToken);
                await networkStream.FlushAsync(cancellationToken);

                messageBuilder.Clear();
            }
        }
        catch (JsonReaderException ex)
        {
            context.NodeContext.Error($"Error parsing input: {ex.Message}");
            throw DemoPipelineExecutionException.MessageDeserializationFailed(ex);
        }
        catch (Exception ex)
        {
            context.NodeContext.Error($"Error processing message: {ex.Message}");
            throw DemoPipelineExecutionException.PipelineExecutionFailed(ex);
        }
        finally
        {
            client.Close();
        }
    }
}