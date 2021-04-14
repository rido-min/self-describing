using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Azure.IoT.ModelsRepository;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Serialization;
using Microsoft.Azure.DigitalTwins.Parser;

namespace service
{
    class Program
    {
        static string cs = System.Environment.GetEnvironmentVariable("HUB_CS");
        static DigitalTwinClient dtc = DigitalTwinClient.CreateFromConnectionString(cs);
        static async Task Main(string[] args)
        {
            var respt = await dtc.GetDigitalTwinAsync<BasicDigitalTwin>("self");
            Console.WriteLine(respt.Body.Metadata.ModelId);

            var model = await ResolveAndParse(respt.Body.Metadata.ModelId);
            
            foreach (var item in model) Console.WriteLine($"{item.Key} - {item.Value}");

            Console.ReadLine();
        }

        private static async Task<IReadOnlyDictionary<Dtmi, DTEntityInfo>> ResolveAndParse(string modelId)
        {
            var mid = new Uri(modelId);
            ModelParser modelParser = new ModelParser();
            IReadOnlyDictionary<Dtmi, DTEntityInfo> model;

            var resolution = HttpUtility.ParseQueryString(mid.Query).Get("resolution");

            if (!string.IsNullOrEmpty(resolution) && resolution == "self")
            {
                Console.WriteLine("Device is Self Reporting, querying for the model");
                var resp = await dtc.InvokeCommandAsync("self", "GetModel");
                var modelPayload = resp.Body.Payload;
                string hash = common.Hash.GetHashString(modelPayload);
                var expectedHash = HttpUtility.ParseQueryString(mid.Query).Get("hash");
                if (hash.Equals(expectedHash))
                {
                    Console.WriteLine("Hash validation passed");
                    Console.WriteLine(modelPayload);
                }
                else
                {
                    throw new ApplicationException("Wrong Hash value");

                }
                model = await modelParser.ParseAsync(new string[] { modelPayload });
            }
            else
            {
                ModelsRepositoryClient dmrClient = new ModelsRepositoryClient();
                var models = dmrClient.GetModels(mid.AbsoluteUri);
                model = await modelParser.ParseAsync(models.Values.ToArray());
            }
            return model;
        }
    }
}
