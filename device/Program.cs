﻿using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace device
{
    class Program
    {
        static string cs = System.Environment.GetEnvironmentVariable("DEVICE_CS");
        static string model = File.ReadAllText(@"..\..\..\deviceModel.json");

        static async Task Main(string[] args)
        {
            string modelIdSD = "dtmi:std:selfreporting;1";
            string reportedModelId = "dtmi:com:example:myDevice;1";
            string hash = common.Hash.GetHashString(model);

            Uri u = new Uri($"{modelIdSD}?SHA256={hash}&id={reportedModelId}");
            string modelId = $"{u.Scheme}:{u.AbsolutePath}{Uri.EscapeDataString(u.Query)}";

            //string modelId = modelIdSD;

            var dc = DeviceClient.CreateFromConnectionString(cs, TransportType.Mqtt, 
                new ClientOptions { ModelId = modelId });

            //TwinCollection reported = new TwinCollection();
            //reported["ReportedModelId"] = reportedModelId;
            //reported["ReportedModelHash"] = hash;
            //await dc.UpdateReportedPropertiesAsync(reported);
            //Console.WriteLine(reported.ToJson(Newtonsoft.Json.Formatting.Indented));

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
