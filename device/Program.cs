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
        static bool sendModelAtConnection = false;
        static string cs = System.Environment.GetEnvironmentVariable("DEVICE_CS");
        static string model = File.ReadAllText(@"..\..\..\deviceModel.json");

        static async Task Main(string[] args)
        {
            string selfDescribingDtmi = "dtmi:azure:common:SelfDescribing;1";
            string targetModelId = "dtmi:com:example:myDevice;1";
            string targetModelhash = common.Hash.GetHashString(model);

            string modelId = selfDescribingDtmi;
            if (sendModelAtConnection)
            {
                Uri u = new Uri($"{selfDescribingDtmi}?tmhash={targetModelhash}&tmid={targetModelId}");
                modelId = $"{u.Scheme}:{u.AbsolutePath}{Uri.EscapeDataString(u.Query)}";
                Console.WriteLine(modelId);
            }

            var dc = DeviceClient.CreateFromConnectionString(cs, TransportType.Mqtt,
                new ClientOptions { ModelId = modelId });

            
            TwinCollection reported = new TwinCollection();
            reported["targetModelId"] = targetModelId;
            reported["targetModelHash"] = targetModelhash;
            await dc.UpdateReportedPropertiesAsync(reported);
            
            Console.WriteLine(reported.ToJson(Newtonsoft.Json.Formatting.Indented));

            await dc.SetMethodHandlerAsync("GetTargetModel", (MethodRequest req, object ctx) =>
            {
                Console.WriteLine("GetModel called");
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(model), 200));
            }, null);

            await SendEvents(dc);

            Console.ReadLine();
        }

        private static async Task SendEvents(DeviceClient dc)
        {
            for (int i = 0; i < 10; i++)
            {
                var message = new Message(Encoding.UTF8.GetBytes("{temp: " + i + "}"));
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";
                await dc.SendEventAsync(message);
                await Task.Delay(400);
            }
        }
    }
}
