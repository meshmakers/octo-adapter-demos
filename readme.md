# OctoMesh Adapter Demos

This repository contains demo adapters for OctoMesh.
The demo adapters demonstrate how to create an adapter for OctoMesh.

OctoMesh is a cloud-native, event-driven, and low-code integration platform.
It is designed to be a scalable, secure, and easy-to-use platform for building and managing integrations.

OctoMesh distinguishes between Edge and Mesh adapters.
Edge adapters are designed to run (optionally) on the edge, while Mesh adapters are designed to run in the cloud only.
The big difference is that Edge adapters can be deployed on devices with limited resources and communicate with the OctoMesh repositories through a MeshAdapter using the OctoMesh DistributionEventHub.
Mesh adapters can be deployed in the cloud and connect directly to the OctoMesh repositories with higher bandwidth and lower latency.

This repository contains a sample MeshAdapter and a sample EdgeAdapter.

Adapters are written in C# and can be run on Windows, Linux, and macOS. The adapters are built using the OctoMesh SDK.
OctoMesh SDK is a .NET library that provides the necessary interfaces and classes to create adapters for OctoMesh and is available as a NuGet package for .NET Standard 2.0 and .NET 9.0.

Edge adapters may target .NET 9 or .NET Framework 4.8. Mesh adapters must target .NET 9.

## Plug or Socket?
Adapters may be of type Plug or Socket, or both.
A Socket adapter is a passive adapter that listens for incoming connections,
while a Plug adapter is an active adapter that connects to a remote endpoint.

## AdapterEgeDemo
The demo adapter contains the following components:

- DemoNode: A simple node that writes the defined message in node configuration to pipeline.
- DemoTriggerNode: A simple trigger node that triggers the pipeline when a TCP message is received at configured port
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
  You should find the adapter configuration in the Admin Panel under the `Communication/Adapters` section.

* Build the adapter

* Set environment variables of the IDE to prepare for running the adapter.
  ```bash
  OCTO_ADAPTER__TENANTID=meshtest
  OCTO_ADAPTER__ADAPTERRTID=6760711ec4ff02221e0b532d
  ```
  The Runtime ID is the ID of the adapter configuration created in the previous step, see the `rtid` field in the adapter configuration YAML at [./scripts/_general/rt-adapters-demo.yaml](./scripts/_general/rt-adapters-demo.yaml).

* Run the adapter

* The adapter should be shown in Admin Panel with communication state `ONLINE`.

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

* Deploy the pipeline to the adapter using AdminPanel.
* Send a TCP message to the trigger node port to trigger the pipeline. Use this script to send a message to the trigger node:
  ```bash
  ./scripts/om_send_tcp_message.ps1 -port 8001
  ```

### Uninstall

* Stop the adapter.

* To delete the test tenant, use the following command:
    ```bash
    ./scripts/om_delete_tenants.ps1
    ```

## Further reading
* [Write your first Edge Adapter](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/adapters/createEdgeAdapter)
* [Write your first Mesh Adapter](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/adapters/createMeshAdapter)
* [Write your first Trigger Node](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/adapters/createTriggerNode)
