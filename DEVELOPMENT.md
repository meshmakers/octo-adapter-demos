# Developer Guide - OctoMesh Adapter Demos

## Architecture

### Edge Adapter vs. Mesh Adapter

| Aspect | Edge Adapter | Mesh Adapter |
|---|---|---|
| Deployment | Edge devices or cloud | Cloud only |
| Connection | Via DistributionEventHub (RabbitMQ) | Direct to OctoMesh repositories |
| Builder | `AdapterBuilder` | `WebAdapterBuilder` |
| SDK | `Microsoft.NET.Sdk.Worker` | `Microsoft.NET.Sdk` |
| Type | Active (Plug) | Passive (Socket) |
| Target Framework | .NET 10 / .NET Framework 4.8 | .NET 10+ |
| Bandwidth | Limited (through EventHub) | High (direct connection) |
| NuGet Package | `Meshmakers.Octo.Sdk.Common` | `Meshmakers.Octo.Sdk.MeshAdapter` + `Meshmakers.Octo.Sdk.Common.Web` |

### Component Overview

```
┌─────────────────────────────────────────────────┐
│                 OctoMesh Platform                │
│  ┌───────────────┐    ┌──────────────────────┐  │
│  │  EventHub      │    │  Repository (MongoDB) │  │
│  │  (RabbitMQ)    │    │                      │  │
│  └───────┬───────┘    └──────────┬───────────┘  │
│          │                       │               │
└──────────┼───────────────────────┼───────────────┘
           │                       │
    ┌──────┴──────┐       ┌───────┴───────┐
    │ Edge Adapter │       │ Mesh Adapter  │
    │ (Plug)       │       │ (Socket)      │
    │              │       │               │
    │ ┌──────────┐ │       │ ┌──────────┐  │
    │ │ Trigger  │ │       │ │ Trigger  │  │
    │ │ Nodes    │ │       │ │ Nodes    │  │
    │ ├──────────┤ │       │ ├──────────┤  │
    │ │ Transform│ │       │ │ Transform│  │
    │ │ Nodes    │ │       │ │ Nodes    │  │
    │ └──────────┘ │       │ └──────────┘  │
    └──────────────┘       └──────────────┘
```

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [OctoMesh CLI (octo-cli)](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/cli)
- A running OctoMesh instance (local or cloud)
- For local development: local NuGet packages in `../nuget/` (built via `invoke-buildall`)

## Build

### Configurations

| Configuration | Purpose | NuGet Source | OctoVersion |
|---|---|---|---|
| `DebugL` | Local development | `../nuget/` (local packages) | `999.0.0` |
| `Debug` | Standard debug | nuget.org | `3.3.*` |
| `Release` | Production build | nuget.org or private server | `3.3.*` / `0.1.*` |

### Build Commands

```bash
# Local development (recommended for daily development)
dotnet build Octo.AdapterDemos.sln -c DebugL

# Standard debug build
dotnet build Octo.AdapterDemos.sln -c Debug

# Release build
dotnet build Octo.AdapterDemos.sln -c Release
```

Output binaries are located in `bin/<Configuration>/net10.0/`.

### Updating Local NuGet Packages

When working with local builds of dependent OctoMesh repositories:
```powershell
# From the meshmakers root directory
invoke-buildall -branch main -configuration DebugL -excludeFrontend $true
```

## Project Structure

```
octo-adapter-demos/
├── Directory.Build.props           # Shared build properties (framework, version, NuGet)
├── Octo.AdapterDemos.sln           # Solution file
├── src/
│   ├── AdapterEdgeDemo/
│   │   ├── Program.cs              # Entry point using AdapterBuilder
│   │   ├── AdapterEdgeDemo.csproj
│   │   ├── DemoPipelineExecutionException.cs
│   │   ├── Services/
│   │   │   └── AdapterEdgeDemoService.cs   # IAdapterService implementation
│   │   ├── Nodes/
│   │   │   ├── DemoNode.cs                 # IPipelineNode implementation
│   │   │   └── DemoTriggerNode.cs          # ITriggerPipelineNode implementation
│   │   ├── Dockerfile
│   │   ├── nlog.config
│   │   └── Properties/launchSettings.json
│   └── AdapterMeshDemo/
│       ├── Program.cs              # Entry point using WebAdapterBuilder
│       ├── AdapterMeshDemo.csproj
│       ├── DemoPipelineExecutionException.cs
│       ├── Services/
│       │   └── AdapterMeshDemoService.cs   # IAdapterService implementation
│       ├── Nodes/
│       │   ├── DemoNode.cs                 # IPipelineNode implementation
│       │   └── DemoTriggerNode.cs          # ITriggerPipelineNode implementation
│       ├── Dockerfile
│       ├── nlog.config
│       └── Properties/launchSettings.json
├── scripts/                        # PowerShell setup/test scripts
│   ├── om_login_local.ps1          # Login to local OctoMesh instance
│   ├── om_create_tenants.ps1       # Create test tenant
│   ├── om_delete_tenants.ps1       # Delete test tenant
│   ├── om_importrt_sample_general.ps1  # Import adapter runtime config
│   ├── om_send_tcp_message.ps1     # Send test TCP message
│   └── _general/
│       └── rt-adapters-demo.yaml   # Runtime entity definitions
└── devops-build/                   # Azure DevOps CI/CD templates
    ├── azure-pipelines.yml         # Main pipeline definition
    ├── build-and-push-docker-image.yml
    ├── handle-artifacts.yml
    ├── set-version.yml
    ├── update-build-number.yml
    └── download-ca.yml
```

## Developing Custom Nodes

### Transformation Node (IPipelineNode)

A transformation node processes data within a pipeline. It receives data, transforms it, and passes it to the next node.

**Steps:**
1. Create a configuration record inheriting from a base configuration class
2. Create the node class implementing `IPipelineNode`
3. Register the node in `Program.cs`

```csharp
// 1. Configuration - defines the YAML configuration schema for this node
[NodeName("MyNode", 1)]  // Name and version used in pipeline YAML
public record MyNodeConfiguration : TargetPathNodeConfiguration
{
    public required string MyParameter { get; set; } = "default";
}

// 2. Node implementation
[NodeConfiguration(typeof(MyNodeConfiguration))]
public class MyNode(NodeDelegate next) : IPipelineNode
{
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var config = nodeContext.GetNodeConfiguration<MyNodeConfiguration>();

        // Process data...
        dataContext.SetValueByPath(config.TargetPath, config.DocumentMode,
            config.TargetValueKind, config.TargetValueWriteMode, "result");

        // Pass to next node
        await next(dataContext, nodeContext);
    }
}

// 3. Register in Program.cs
services.AddDataPipeline()
    .RegisterNode<MyNode>();
```

**Available base configuration classes:**
- `TargetPathNodeConfiguration` - provides `TargetPath` + write options
- `PathNodeConfiguration` - provides `Path` (source path)
- `SourceTargetPathNodeConfiguration` - provides both `Path` + `TargetPath` + options

### Trigger Node (ITriggerPipelineNode)

A trigger node starts pipeline execution based on external events (e.g., incoming TCP messages, timers, file changes).

```csharp
[NodeName("MyTrigger", 1)]
public record MyTriggerNodeConfiguration : TriggerNodeConfiguration
{
    public required int Interval { get; set; } = 5000;
}

[NodeConfiguration(typeof(MyTriggerNodeConfiguration))]
public class MyTriggerNode : ITriggerPipelineNode
{
    public Task StartAsync(ITriggerContext context)
    {
        // Start listening/polling...
        // Call context.ExecuteAsync(...) to trigger pipeline execution
        return Task.CompletedTask;
    }

    public Task StopAsync(ITriggerContext context)
    {
        // Clean up resources
        return Task.CompletedTask;
    }
}
```

### Pipeline YAML Configuration

Pipelines are configured in YAML and deployed via the AdminPanel:

```yaml
triggers:
  - type: MyTrigger@1        # NodeName@Version
    interval: 5000            # Custom config properties

transformations:
  - type: MyNode@1
    description: Process data
    myParameter: value        # Custom config properties
    targetPath: $.result      # From TargetPathNodeConfiguration
```

## Adapter Lifecycle

### Startup Flow
1. Builder creates host (`AdapterBuilder` / `WebAdapterBuilder`)
2. Services registered (DI container)
3. `IAdapterService.StartupAsync()` called with `AdapterStartup` containing tenant ID and pipeline configurations
4. Pipelines registered via `IPipelineRegistryService.RegisterPipelinesAsync()`
5. EventHub connection started
6. Trigger nodes begin listening

### Shutdown Flow
1. `IAdapterService.ShutdownAsync()` called
2. EventHub connection stopped
3. All pipelines unregistered (trigger nodes stopped)

## Environment Variables

### Edge Adapter
| Variable | Description | Example |
|---|---|---|
| `OCTO_ADAPTER__TENANTID` | Target tenant ID | `meshtest` |
| `OCTO_ADAPTER__ADAPTERRTID` | Adapter runtime entity ID | `6760711ec4ff02221e0b532d` |

### Mesh Adapter (additional)
| Variable | Description | Example |
|---|---|---|
| `OCTO_ADAPTER__ADAPTERCKTYPEID` | CK type of the adapter | `System.Communication/MeshAdapter` |
| `OCTO_SYSTEM__AdminUserPassword` | Admin password for direct DB access | `OctoAdmin1` |
| `OCTO_SYSTEM__DatabaseUserPassword` | Database user password | `OctoUser1` |
| `OCTO_SYSTEM__UseDirectConnection` | Use direct MongoDB connection | `true` |

## Docker

Both adapters are built as multi-platform Docker images (linux/amd64, linux/arm64).

```bash
# Build Edge adapter image locally
docker build -f src/AdapterEdgeDemo/Dockerfile \
  --build-arg OCTO_VERSION=999.0.0 \
  -t octo-communication-edge-adapter-demo:local .

# Build Mesh adapter image locally
docker build -f src/AdapterMeshDemo/Dockerfile \
  --build-arg OCTO_VERSION=999.0.0 \
  -t octo-communication-mesh-adapter-demo:local .
```

**Note:** Local Docker builds require the `OCTO_PRIVATE_NUGET_SERVICE` and `OCTO_PRIVATE_NUGET_CERTIFICATE` build arguments for NuGet authentication against the private feed.

## CI/CD Pipeline (Azure DevOps)

The pipeline (`devops-build/azure-pipelines.yml`) runs on self-hosted `meshmakers-ci-agents`:

1. **Version** - Sets build number and assembly version
2. **Restore** - NuGet restore with private server authentication
3. **Test** - Runs all tests (excluding SystemTests)
4. **Docker Build** - Builds both adapter images for linux/arm64 + linux/amd64
5. **Artifacts** - Publishes NuGet packages and version info

### Branch Strategy
| Branch | Docker Push | Registry |
|---|---|---|
| `refs/tags/r*` (release) | Yes | Public (docker.io) + Private (docker.mm.cloud) |
| `test/*` | Yes | Private only (docker.mm.cloud) |
| `main` | Yes | Private only (docker.mm.cloud) |
| `dev/*` | Build only | No push |

## Error Handling

Custom exceptions should extend `PipelineExecutionException`:

```csharp
public class MyPipelineExecutionException : PipelineExecutionException
{
    public static Exception OperationFailed(Exception ex)
        => new MyPipelineExecutionException("Operation failed", ex);
}
```

## Logging

Both adapters use NLog with colored console output. Configuration in `nlog.config`:
- Debug level minimum logging
- Color-coded output (Gray=Debug, White=Info, Yellow=Warn, Red=Error/Fatal)

Within pipeline nodes, use `nodeContext.Info()`, `nodeContext.Error()` etc. for pipeline-scoped logging.
