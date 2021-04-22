using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        static string deviceId = "self";

        static async Task Main(string[] args)
        {
            var respt = await dtc.GetDigitalTwinAsync<BasicDigitalTwin>(deviceId);
            Console.WriteLine($"Device '{deviceId}' announced: {respt.Body.Metadata.ModelId}\n");

            var model = await ResolveAndParse(new Uri(respt.Body.Metadata.ModelId));
            model.ToList().ForEach(i => Console.WriteLine(i.Key));

            Console.ReadLine();
        }

        private static async Task<IReadOnlyDictionary<Dtmi, DTEntityInfo>> ResolveAndParse(Uri mid)
        {
            IReadOnlyDictionary<Dtmi, DTEntityInfo> model;
        
            string repo = "https://raw.githubusercontent.com/iotmodels/iot-plugandplay-models/selfdescribing";
            ModelsRepositoryClient dmrClient = new ModelsRepositoryClient(new Uri(repo));
            ModelParser modelParser = new ModelParser() 
            {
                DtmiResolver = dmrClient.ParserDtmiResolver
            };

            if (mid.AbsolutePath == "std:selfreporting;1")
            {
                Console.WriteLine("Device is Self Reporting. Querying device for the model . . ");

                var resp = await dtc.InvokeCommandAsync(deviceId, "GetModel");
                string modelPayload = resp.Body.Payload;
                Console.Write("Device::GetModel() ok..");

                CheckHash(mid, modelPayload);

                model = await modelParser.ParseAsync(new string[] { modelPayload });

                Console.WriteLine("Self Describing protocol checks succeed\n");
            }
            else
            {
                Console.WriteLine("Resolving from repo");
                var models = dmrClient.GetModels(mid.ToString());
                model = await modelParser.ParseAsync(models.Values.ToArray());
            }
            return model;
        }
        private static void CheckHash(Uri mid, string modelPayload)
        {
            var expectedHash = HttpUtility.ParseQueryString(mid.Query).Get("hash");
            string hash = common.Hash.GetHashString(modelPayload);
            if (hash.Equals(expectedHash, StringComparison.InvariantCulture))
            {
                Console.Write(" Hash check ok.. ");
            }
            else
            {
                throw new ApplicationException("Wrong Hash value");
            }
        }
    }
}
