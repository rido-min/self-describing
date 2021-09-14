using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace device
{
    class DpsX509Client
    {
        public static async Task<DeviceClient> SetupDeviceClientAsync(string IdScope, string modelId, CancellationToken cancellationToken)
        {
            var certpath = @"C:\Users\rido2\Downloads\myFirstDevice.pfx";    
            var transport = new ProvisioningTransportHandlerMqtt();
            var cert = new X509Certificate2(certpath, "1234");
            var security = new SecurityProviderX509Certificate(cert);
            var provClient = ProvisioningDeviceClient.Create("global.azure-devices-provisioning.net", IdScope, security, transport);
            var provResult = await provClient.RegisterAsync(new ProvisioningRegistrationAdditionalData { JsonData = "{ modelId: '" + modelId + "'}" });
            Console.WriteLine(provResult.AssignedHub + provResult.Status);
            var csBuilder = IotHubConnectionStringBuilder.Create(provResult.AssignedHub, new DeviceAuthenticationWithX509Certificate(provResult.DeviceId, security.GetAuthenticationCertificate()));
            string connectionString = csBuilder.ToString();
            var client = DeviceClient.Create(provResult.AssignedHub, new DeviceAuthenticationWithX509Certificate(provResult.DeviceId, security.GetAuthenticationCertificate()), TransportType.Mqtt, new ClientOptions { ModelId = modelId });
            return client;
        }
    }
}
