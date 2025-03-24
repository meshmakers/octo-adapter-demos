using System.Net;
using System.Net.Sockets;
using System.Text;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Communication.MeshAdapter.Demo.Nodes;

/// <summary>
/// Configuration for DemoTriggerNode - a trigger node starts the execution of the transformation pipeline
/// </summary>
[NodeName("DemoTrigger", 1)]
// ReSharper disable once ClassNeverInstantiated.Global
public record DemoTriggerNodeConfiguration : TriggerNodeConfiguration
{
    /// <summary>
    /// Port the sample TCP listener listens on
    /// </summary>
    public required ushort Port { get; set; } = 8000;
}

/// <summary>
/// Implements 
/// </summary>
[NodeConfiguration(typeof(DemoTriggerNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class DemoTriggerNode( /* you can inject services here */)
    : ITriggerPipelineNode
{
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cts;


    public Task StartAsync(ITriggerContext context)
    {
        var c = context.NodeContext.GetNodeConfiguration<DemoTriggerNodeConfiguration>();

        _cts = new CancellationTokenSource();
        _tcpListener = new TcpListener(IPAddress.Any, c.Port);
        _tcpListener.Start();

        context.NodeContext.Info("TCP listener started and waiting for connections...");

        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    _ = ProcessClientAsync(tcpClient, context, _cts.Token); // Execute in a separate task
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped
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
            await using var networkStream = client.GetStream();
            var buffer = new byte[1024];
            int bytesRead;
            var messageBuilder = new StringBuilder();
            while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                Console.WriteLine("Client connected.");

                // Nachricht vom Client lesen
             //   string receivedMessage = reader.ReadLine();
              //  Console.WriteLine($"Received: {receivedMessage}");

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (networkStream.DataAvailable) continue;

                var message = messageBuilder.ToString();
                context.NodeContext.Info($"Received message: {message}");

                try
                {
                    var input = JToken.Parse(message);
                    var output = await context.ExecuteAsync(
                        new ExecutePipelineOptions(DateTime.UtcNow) // adapt as needed
                        {
                            ExternalReceivedDateTime = DateTime.UtcNow // adapt as needed
                        },
                        input);

                    var outputString = JsonConvert.SerializeObject(output);
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(outputString + Environment.NewLine);
                    await networkStream.WriteAsync(utf8Bytes, cancellationToken);
                    await networkStream.FlushAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    context.NodeContext.Error($"Error processing message: {ex.Message}");
                    throw DemoPipelineExecutionException.ProcessFailed(ex);
                }

                messageBuilder.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler bei der Client-Verarbeitung: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }
}