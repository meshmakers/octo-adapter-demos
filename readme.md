# OctoMesh Demo Plug

This is a demo plug for OctoMesh. It is a simple plug that demonstrates how to create a plug for OctoMesh.

The demo plug contains following components:

- DemoNode: A simple node that writes the defined message in node configuration to pipeline.
- DemoTriggerNode: A simple trigger node that triggers the pipeline when a TCP message is received at configured port
- Scripts to deploy the plug to OctoMesh.

## Setup

* Setup an OctoMesh instance and install the OctoMesh CLI. Log-in to the OctoMesh instance using the CLI. For a local instance, use the following command:
    ```bash
    ./scripts/om_login_local.ps1
    ```
* Create a test tenant to deploy the plug. To create a tenant named `meshtest`, use the following command:
    ```bash
    ./scripts/om_create_tenants.ps1
    ```

* Create the adapter configuration for the plug. To create the adapter configuration, use the following command:
    ```bash
    ./scripts/om_importrt_sample_general.ps1
    ```
  You should find the adapter configuration in the Admin Panel under the `Communication/Adapters` section.

* Build the plug

* Set environment variables of the IDE to prepare for running the plug.
  ```bash
  OCTO_ADAPTER__TENANTID=meshtest
  OCTO_ADAPTER__ADAPTERRTID=6760711ec4ff02221e0b532d
  ```
  The Runtime ID is the ID of the adapter configuration created in the previous step, see the `rtid` field in the adapter configuration YAML at [./scripts/_general/rt-adapters-demo.yaml](./scripts/_general/rt-adapters-demo.yaml).

* Run the plug

* The plug should be shown in Admin Panel with communication state `ONLINE`.

* Create a pipeline with the plug nodes. This sample pipeline configuration demonstrates how to use the plug nodes:
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

## Uninstall

* Stop the plug.

* To delete the test tenant, use the following command:
    ```bash
    ./scripts/om_delete_tenants.ps1
    ```

## Further reading
* [Create first Adapter](https://docs.meshmakers.cloud/docs/developerGuide/Sdk/creatingAdapter)
