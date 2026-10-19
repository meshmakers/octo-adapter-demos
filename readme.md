# OctoMesh Adapter Demos

This repository contains demo adapters for OctoMesh.
The demo adapters demonstrate how to create an adapter for OctoMesh.

OctoMesh is a cloud-native, event-driven, and low-code integration platform.
It is designed to be a scalable, secure, and easy-to-use platform for building and managing integrations.

OctoMesh distinguishes between Edge and Mesh adapters.
Edge adapters are designed to run (optionally) on the edge, while Mesh adapters are designed to run in the cloud only.
The big difference is that Edge adapters can be deployed on devices with limited resources and communicate with the OctoMesh repositories through a MeshAdapter using the OctoMesh DistributionEventHub.
Mesh adapters can be deployed in the cloud and connect directly to the OctoMesh repositories with higher bandwidth and lower latency.

This repository contains a sample Mesh adapter and a sample Edge adapter.
Both are configured in a tenant as an entity of the CK type `System.Communication/Adapter`; whether an adapter runs as Edge or Mesh adapter is decided by the code that hosts it (`AdapterBuilder` vs. `WebAdapterBuilder`), not by the CK type.

Adapters are written in C# and can be run on Windows, Linux, and macOS. The adapters are built using the OctoMesh SDK.
OctoMesh SDK is a .NET library that provides the necessary interfaces and classes to create adapters for OctoMesh and is available as a NuGet package for .NET 10.0.

Edge adapters may target .NET 10 or .NET Framework 4.8. Mesh adapters must target .NET 10.

## Plug or Socket?
Adapters may be of type Plug or Socket, or both.
A Socket adapter is a passive adapter that listens for incoming connections,
while a Plug adapter is an active adapter that connects to a remote endpoint.

## AdapterEdgeDemo
The Edge demo adapter is an active (Plug) adapter that connects to an OctoMesh instance via the DistributionEventHub. It contains the following components:

- DemoNode: A simple node that writes the defined message in node configuration to pipeline.
- DemoTriggerNode: A simple trigger node that triggers the pipeline when a TCP message is received at configured port.
- Scripts to deploy the adapter to OctoMesh.

### Setup

* Setup an OctoMesh instance and install the OctoMesh CLI. Log-in to the OctoMesh instance using the CLI. For a local instance, use the following command:
    ```bash
    ./scripts/om_login_local.ps1
    ```
* Create a test tenant to deploy the adapter. To create a tenant named `meshtest`, use the following command:
    ```bash
    ./scripts/om_create_tenants.ps1
    ```

* Create the configuration for the adapter. To create the adapter configuration, use the following command:
    ```bash
    ./scripts/om_importrt_sample_general.ps1
    ```
  The script enables communication for the tenant (`EnableCommunication`) and imports two adapter entities of CK type `System.Communication/Adapter`. You should find them in Refinery Studio under `Communication > Adapters` (`/<tenantId>/communication/adapters`).

* Build the adapter
    ```bash
    dotnet build Octo.AdapterDemos.sln -c DebugL
    ```

* Set environment variables of the IDE to prepare for running the adapter.
  ```bash
  OCTO_ADAPTER__TENANTID=meshtest
  OCTO_ADAPTER__ADAPTERRTID=6760711ec4ff02221e0b532d
  ```
  The Runtime ID is the ID of the adapter configuration created in the previous step, see the `rtid` field in the adapter configuration YAML at [./scripts/_general/rt-adapters-demo.yaml](./scripts/_general/rt-adapters-demo.yaml).

* Run the adapter

* In Refinery Studio, open `Communication > Adapters` of the tenant `meshtest`: the adapter `Demo Edge Adapter` should show the communication state `Online`.

* Create a pipeline with the adapter nodes. This sample pipeline configuration demonstrates how to use the adapter nodes:
  ```yaml
  triggers:
    - type: DemoTrigger@1
      port: 8000
  transformations:
    - type: Demo@1
      description: Simulates data
      myMessage: Hello, Mars!
  ```

* Create the pipeline in Refinery Studio under `Communication > Data Flows`, assign it to the adapter and deploy it.
* Send a TCP message to the trigger node port to trigger the pipeline. Use this script to send a message to the trigger node:
  ```bash
  ./scripts/om_send_tcp_message.ps1 -port 8000
  ```

### Uninstall

* Stop the adapter.

* To delete the test tenant, use the following command:
    ```bash
    ./scripts/om_delete_tenants.ps1
    ```

## AdapterMeshDemo
The Mesh demo adapter is a passive (Socket) adapter that connects directly to OctoMesh repositories. It runs as a web host and provides higher bandwidth and lower latency compared to Edge adapters. It contains the following components:

- DemoNode: A simple node that writes the defined message in node configuration to pipeline.
- DemoTriggerNode: A simple trigger node that triggers the pipeline when a TCP message is received at configured port.

### Setup

* Complete the general setup steps described in the [AdapterEdgeDemo](#adapteredgedemo) section (OctoMesh instance, tenant, adapter configuration).

* Build the adapter
    ```bash
    dotnet build Octo.AdapterDemos.sln -c DebugL
    ```

* Set environment variables of the IDE to prepare for running the adapter.
  ```bash
  OCTO_ADAPTER__TENANTID=meshtest
  OCTO_ADAPTER__ADAPTERRTID=6760711ec4ff02221e0b532e
  OCTO_ADAPTER__ADAPTERCKTYPEID=System.Communication/Adapter
  OCTO_SYSTEM__AdminUserPassword=OctoAdmin1
  OCTO_SYSTEM__DatabaseUserPassword=OctoUser1
  OCTO_SYSTEM__UseDirectConnection=true
  ```
  The Mesh adapter requires additional system configuration (database credentials, direct connection mode) since it connects directly to the OctoMesh repositories.

* Run the adapter

* In Refinery Studio, open `Communication > Adapters` of the tenant `meshtest`: the adapter `Demo Mesh Adapter` should show the communication state `Online`.

* Create and deploy a pipeline using the same node configuration as described in the Edge adapter section.

* Send a TCP message to the trigger node port:
  ```bash
  ./scripts/om_send_tcp_message.ps1 -port 8000
  ```

## Deployed adapters
The steps above run a demo adapter from your IDE against a local OctoMesh instance. In a deployed environment, adapters are rolled out by the Communication Operator (Helm); there is no `dotnet run` involved.

* **Mesh adapter of the tenant (`octo-mesh-adapter` chart).** The local [getting-started](https://github.com/meshmakers/getting-started) stack (kind cluster, OctoMesh 3.5.x) installs the platform; its `scripts/om-bootstrap-tenant.ps1` creates the tenant, enables communication (this seeds the default deployment site and the mesh adapter workload) and deploys both through the operator. The script can be re-run.
* **In Refinery Studio** (`/<tenantId>/communication/...`): `Deployment sites` lists the sites (pools) the operator manages, `Adapters` the adapter entities with their communication state (`Online`), `Data Flows` the pipelines.
* A deployed adapter is started from Studio (`Adapters`) or with `octo-cli -c DeployWorkload -id <adapterRtId>`, after the deployment site was deployed (`octo-cli -c DeployDeploymentSite -id <siteRtId>`). Packaging your own adapter as a chart for the operator is not described in this repository.

The IDE steps in this README expect the dev-stack service URLs of `scripts/om_login_local.ps1` (`https://localhost:5001/`, `:5003`, `:5009`, `:5015`, started with `Start-Octo`). Running the IDE adapters against the getting-started stack needs its URLs (`*.127-0-0-1.nip.io`) instead; that route is not covered here yet.

## Further reading
* [Write your first Edge Adapter](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/adapters/createEdgeAdapter)
* [Write your first Mesh Adapter](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/adapters/createMeshAdapter)
* [Write your first Trigger Node](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/adapters/createTriggerNode)
