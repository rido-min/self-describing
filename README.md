# IoT Plug and Play self-describing devices

A standard IoT Plug and Play device announces the model it implements, letting the service use the model ID to retrieve the model from a repository. To enable dynamic scenarios where the model might change, a self-describing device can send its model on request to the service. To enable this scenario, the convention is that:

- A self-describing device includes the `dtmi:azure:common:SelfDescribing;1` model ID in the connection string it uses to connect. If you're using the Device Provisioning Service (DPS) to manage connections, include the model ID in the DPS payload the device sends.
- Optionally, the device decorates the model ID in the DPS payload with a hash of the model, and the model ID of the model the device implements. For example: `dtmi:azure:common:SelfDescribing;1?tmhash=07C6435C9DDC626&tmid=dtmi:com:example:myDevice;1`. The hash value lets service developers implement a cache when multiple devices announce the same model ID with the same hash value. The service can cache the model removing the need to call `GetTargetModel` multiple times.
- A self-describing device implements the `targetModelId` and `targetModelHash` properties defined in the `dtmi:azure:common:SelfDescribing;1` interface. These properties hold the same values as the `tmid` and `tmhash` query string parameters that optionally decorate the model ID sent by a self-describing device.
- A self-describing device implements the `GetTargetModel` command defined in the `dtmi:azure:common:SelfDescribing;1` interface to return the model DTDL.

The following JSON shows the standard `dtmi:azure:common:SelfDescribing;1` interface:

```json
{ 
  "@context": "dtmi:dtdl:context;2", 
  "@id": "dtmi:azure:common:SelfDescribing;1", 
  "@type": "Interface", 
  "displayName": "IDispatch", 
  "contents": [ 
    { 
      "@type": "Command", 
      "name": "GetTargetModel", 
      "response": { 
        "name": "GetTargetModelResponse", 
        "schema": "string" 
      } 
    }, 
    { 
      "@type": "Property", 
      "name": "targetModelHash", 
      "schema": "string", 
      "writable": false 
    }, 
    { 
      "@type": "Property", 
      "name": "targetModelId", 
      "schema": "string", 
      "writable": false 
    } 
  ] 
} 
```

## Code sample

The code sample in this repository shows an implementation of the self-describing device convention. The sample includes a self-describing device and a service application that interacts with the device.

When you run the [device sample](./device/Program.cs) in this repository, the output looks like the following:

```
dtmi:azure:common:SelfDescribing;1?tmhash=07C643...&tmid=dtmi:com:example:myDevice;1

Device Client connected: HostName=domsampleiothub.azure-devices.net;DeviceId=sdd-001;SharedAccessKey=iafzvv...=

{
  "targetModelId": "dtmi:com:example:myDevice;1",
  "targetModelHash": "07C643..."
}
```

The device:

- Prints to the console the decorated model ID it will send to IoT Hub.
- Connects to IoT Hub.
- Sends model information to the service using the propereties defined in the `dtmi:azure:common:SelfDescribing;1` interface.
- Starts listening for call to the `GetTargetModel` command. Responds to calls by sending a copy of the device model.
- Sends telemetry.

When you run the [service sample](./service/Program.cs) in this repository, the output looks like the following:

```
Device 'sdd-001' announced: dtmi:azure:common:SelfDescribing;1?tmhash=07C643...&tmid=dtmi:com:example:myDevice;1

Device is Self Reporting. Querying device for the model . .
Device::GetTargetModel() ok..Discovered ModelId: dtmi:com:example:myDevice;1 Hash check ok..  @Id checks ok..  Extends check ok..
Model Parsed !!


dtmi:com:example:myDevice:_contents:__temp;1
dtmi:com:example:myDevice:_contents:__SerialNumber;1
dtmi:com:example:myDevice;1
dtmi:dtdl:instance:Schema:double;2
dtmi:dtdl:instance:Schema:string;2
dtmi:azure:common:SelfDescribing:_contents:__GetTargetModel:_response;1
dtmi:azure:common:SelfDescribing:_contents:__GetTargetModel;1
dtmi:azure:common:SelfDescribing:_contents:__targetModelHash;1
dtmi:azure:common:SelfDescribing:_contents:__targetModelId;1
dtmi:azure:common:SelfDescribing;1
```

The service:

- Prints the decorated model ID it receives from the device to the console.
- Checks if the device is a self-describing device.
- Calls the `GetTargetModel` command on the device to retrieve the model.
- Checks the hash of the model.
- Checks the model ID of the model.
- Checks that the device model extends `dtmi:azure:common:SelfDescribing;1`.
- Parses the model and prints the results to the console.