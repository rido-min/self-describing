using Rido.IoTHubClient;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace device
{
    class Program
    {
        static readonly bool sendModelAtConnection = true;
        static readonly bool sendModelAsProperties = true;
        static readonly string cs = System.Environment.GetEnvironmentVariable("DEVICE_CS");
        static string model = File.ReadAllText(@"./deviceModel.json");

        static async Task Main()
        {
            string selfDescribingDtmi = "dtmi:azure:common:SelfDescribing;1";
            string targetModelId = "dtmi:com:example:myDevice;1";
            string targetModelHash = common.Hash.GetHashString(model);

            string modelId = selfDescribingDtmi;

            if (sendModelAtConnection)
            {
                Uri u = new($"{selfDescribingDtmi}?tmhash={targetModelHash}&tmid={targetModelId}");
                modelId = System.Web.HttpUtility.UrlEncode($"{u.Scheme}:{u.AbsolutePath}{u.Query}");
                Console.WriteLine(modelId);
            }

            var client = await HubMqttClient.CreateFromConnectionStringAsync(cs + ";ModelId=" + modelId);
            Console.WriteLine("Device Client connected: " + cs);

            if (sendModelAsProperties)
            {
                var v = await client.UpdateTwinAsync(new { targetModelId, targetModelHash });
                Console.WriteLine("Update Twin with Std, v: " + v);
            }

            client.OnCommand = e =>
            {
                model = File.ReadAllText(@"./deviceModel.json");
                Console.WriteLine("GetModel called");
                //await client.CommandResponseAsync(e.Rid, e.CommandName, "200", model);
                return  new CommandResponse()
                {
                    CommandName = e.CommandName,
                    CommandResponsePayload = model,
                    _status = 200
                };
            };

            await SendEvents(client);

            Console.ReadLine();
        }

        private static async Task SendEvents(IHubMqttClient dc)
        {
            Console.Write("Sending events ");
            for (int i = 0; i < 100; i++)
            {
                await dc.SendTelemetryAsync(new { temper = i });
                await Task.Delay(1000);
                Console.Write('.');
            }
        }
    }
}
