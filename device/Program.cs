using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace device
{
    class Program
    {
        static bool sendModelAtConnection = true;
        static bool sendModelAsProperties = true;
        static string cs = System.Environment.GetEnvironmentVariable("DEVICE_CS");
        static string model = File.ReadAllText(@"./deviceModel.json");

        static async Task Main(string[] args)
        {
            string selfDescribingDtmi = "dtmi:azure:common:SelfDescribing;1";
            string targetModelId = "dtmi:com:example:myDevice;1";
            string targetModelhash = common.Hash.GetHashString(model);

            string modelId = selfDescribingDtmi;

            if (sendModelAtConnection)
            {
                Uri u = new($"{selfDescribingDtmi}?tmhash={targetModelhash}&tmid={targetModelId}");
                modelId = $"{u.Scheme}:{u.AbsolutePath}{u.Query}";
                Console.WriteLine(modelId);
            }

            var dc = DeviceClient.CreateFromConnectionString(cs, TransportType.Mqtt,
                new ClientOptions { ModelId = modelId });

            //var dc = await DpsX509Client.SetupDeviceClientAsync("0ne003861C6", modelId, new System.Threading.CancellationToken());

            Console.WriteLine("Device Client connected: " + cs);

            if (sendModelAsProperties)
            {
                TwinCollection reported = new TwinCollection();
                reported["targetModelId"] = targetModelId;
                reported["targetModelHash"] = targetModelhash;
                await dc.UpdateReportedPropertiesAsync(reported);

                Console.WriteLine(reported.ToJson(Newtonsoft.Json.Formatting.Indented));
            }

            await dc.SetMethodHandlerAsync("GetTargetModel", (MethodRequest req, object ctx) =>
            {
                model = File.ReadAllText(@"./deviceModel.json");
                Console.WriteLine("GetModel called");
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(model), 200));
            }, null);

            await SendEvents(dc);

            Console.ReadLine();
        }

        private static async Task SendEvents(DeviceClient dc)
        {
            Console.Write("Sending events ");
            for (int i = 0; i < 100; i++)
            {
                var message = new Message(Encoding.UTF8.GetBytes("{ \"temp\": " + i + "}"));
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";
                await dc.SendEventAsync(message);
                await Task.Delay(1000);
                Console.Write('.');
            }
        }
    }
}
