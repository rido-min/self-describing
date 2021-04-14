using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace device
{
    class Program
    {
        static string cs = System.Environment.GetEnvironmentVariable("DEVICE_CS");
        static string model = File.ReadAllText(@"..\..\..\deviceModel.json");

        static async Task Main(string[] args)
        {
            var id = JsonDocument.Parse(model).RootElement.GetProperty("@id").GetString();
            var hash = common.Hash.GetHashString(model);
            Uri u = new Uri($"{id}?resolution=self&hash={hash}");
            string modelId = $"{u.Scheme}:{u.AbsolutePath}{Uri.EscapeDataString(u.Query)}";

            var dc = DeviceClient.CreateFromConnectionString(cs, TransportType.Mqtt, new ClientOptions { ModelId = modelId });
            Console.WriteLine(modelId);

            await dc.SetMethodHandlerAsync("GetModel", (MethodRequest req, object ctx) =>
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
