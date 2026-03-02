# CLAUDE.md - OctoMesh Adapter Demos

## Project Overview
Demo adapters for OctoMesh showing how to implement Edge and Mesh adapters using the OctoMesh SDK.

## Build Commands
```bash
# Local development build (uses local NuGet packages from ../nuget/)
dotnet build Octo.AdapterDemos.sln -c DebugL

# Release build
dotnet build Octo.AdapterDemos.sln -c Release

# Run tests
dotnet test Octo.AdapterDemos.sln -c DebugL
```

## Project Structure
- `src/AdapterEdgeDemo/` - Edge adapter (runs on edge devices, communicates via DistributionEventHub)
- `src/AdapterMeshDemo/` - Mesh adapter (runs in cloud, connects directly to OctoMesh repositories)
- `scripts/` - PowerShell scripts for tenant/adapter setup via octo-cli
- `devops-build/` - Azure DevOps CI/CD pipeline templates

## Key Patterns
- **Edge Adapter**: Uses `AdapterBuilder` (active/Plug adapter), SDK: `Microsoft.NET.Sdk.Worker`
- **Mesh Adapter**: Uses `WebAdapterBuilder` (passive/Socket adapter), SDK: `Microsoft.NET.Sdk`
- Both implement `IAdapterService` for startup/shutdown lifecycle
- Pipeline nodes implement `IPipelineNode`, trigger nodes implement `ITriggerPipelineNode`
- Node configuration via `[NodeName]` and `[NodeConfiguration]` attributes
- Primary constructors with DI (C# 12+ pattern)

## Configuration
- Build configurations: `Debug`, `Release`, `DebugL` (local NuGet at `../nuget/`)
- Target framework: `net10.0`
- NuGet version strategy: `999.0.0` (DebugL), `0.1.*` (private server), `3.3.*` (public)
- Environment variables: `OCTO_ADAPTER__TENANTID`, `OCTO_ADAPTER__ADAPTERRTID`

## Conventions
- `TreatWarningsAsErrors` is enabled - no warnings allowed
- Nullable reference types enabled
- `LangVersion: latestmajor`
- Assembly names follow `Meshmakers.Octo.Communication.{Type}Adapter.Demo` pattern

## Development Rules (MANDATORY)

### Documentation
- **Every code change MUST include updated developer documentation** (`DEVELOPMENT.md`, `readme.md`)
- When adding new nodes, features, or configuration options, update the corresponding documentation sections
- When changing APIs, update code examples in documentation

### Testing
- **Unit tests are REQUIRED for all new and changed code**
- **Integration tests are REQUIRED for adapter services and pipeline nodes**
- Tests must be written before or alongside the code change (not deferred)
- All existing tests must continue to pass

### Pre-Commit Checklist (ALL steps MUST pass before committing)
1. **Code style check**: `dotnet format Octo.AdapterDemos.sln --verify-no-changes`
2. **Build**: `dotnet build Octo.AdapterDemos.sln -c DebugL`
3. **Unit & integration tests**: `dotnet test Octo.AdapterDemos.sln -c DebugL`

```bash
# Run all pre-commit checks in sequence
dotnet format Octo.AdapterDemos.sln --verify-no-changes && \
dotnet build Octo.AdapterDemos.sln -c DebugL && \
dotnet test Octo.AdapterDemos.sln -c DebugL
```

If any step fails, fix the issue before committing. Do NOT skip these checks.
