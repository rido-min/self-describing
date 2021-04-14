using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Serialization;

namespace service
{
    class Program
    {
        static string cs = System.Environment.GetEnvironmentVariable("HUB_CS");
        static async Task Main(string[] args)
        {
            var dtc = DigitalTwinClient.CreateFromConnectionString(cs);

            var respt = await dtc.GetDigitalTwinAsync<BasicDigitalTwin>("self");
            var mid = new Uri(respt.Body.Metadata.ModelId);
            Console.WriteLine(mid.ToString());
            var resolution = HttpUtility.ParseQueryString(mid.Query).Get("resolution");

            if (resolution=="self")
            {
                Console.WriteLine("Device is Self Reporting, querying for the model");
                var resp = await dtc.InvokeCommandAsync("self", "GetModel");
                var model = resp.Body.Payload;
                string hash = common.Hash.GetHashString(model);
                var expectedHash = HttpUtility.ParseQueryString(mid.Query).Get("hash");
                if (hash.Equals(expectedHash))
                {
                    Console.WriteLine("Hash validation passed");
                    Console.WriteLine(model);
                }
                else
                {
                    Console.WriteLine("Wrong Hash");
                }
            } else
            {
                // try to resolve with DMR client
            }
            Console.ReadLine();
        }
    }
}
