using Azure.IoT.ModelsRepository;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Serialization;
using Microsoft.Azure.DigitalTwins.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace service
{

    class Program
    {
        static readonly string cs = Environment.GetEnvironmentVariable("HUB_CS");
        static readonly DigitalTwinClient dtc = DigitalTwinClient.CreateFromConnectionString(cs);
        static readonly string deviceId = "sdd";

        static async Task Main(string[] args)
        {
            var twinResponse = await dtc.GetDigitalTwinAsync<BasicDigitalTwin>(deviceId);
            Console.WriteLine($"Device '{deviceId}' announced: {twinResponse.Body.Metadata.ModelId}\n");

            var model = await ResolveAndParse(twinResponse.Body);
            Console.WriteLine("\nModel Parsed !! \n\n");
            model.ToList().ForEach(i => Console.WriteLine(i.Key));

            Console.ReadLine();
        }

        private static async Task<IReadOnlyDictionary<Dtmi, DTEntityInfo>> ResolveAndParse(BasicDigitalTwin twin)
        {
            IReadOnlyDictionary<Dtmi, DTEntityInfo> model;

            string repo = "https://raw.githubusercontent.com/iotmodels/iot-plugandplay-models/selfdescribing";
            ModelsRepositoryClient dmrClient = new ModelsRepositoryClient(new Uri(repo));
            ModelParser modelParser = new ModelParser()
            {
                DtmiResolver = dmrClient.ParserDtmiResolver
            };

            Uri modelId = new Uri(twin.Metadata.ModelId);
            if (modelId.AbsolutePath == "azure:common:SelfDescribing;1")
            {
                Console.WriteLine("Device is Self Reporting. Querying device for the model . . ");

                var commandResponse = await dtc.InvokeCommandAsync(deviceId, "GetTargetModel");
                string modelPayload = commandResponse.Body.Payload;
                Console.Write("Device::GetTargetModel() ok..");
                string discoveredModelId = JsonDocument.Parse(modelPayload).RootElement.GetProperty("@id").GetString();
                Console.Write("Discovered ModelId: " + discoveredModelId);

                string targetModelHash = SelfDescribingChecks.GetPropFromModelOrTwin(twin, "tmhash", "targetModelHash");
                if (!string.IsNullOrEmpty(targetModelHash))
                {

                    SelfDescribingChecks.CheckHash(targetModelHash, modelPayload);
                }

                string targetModelId = SelfDescribingChecks.GetPropFromModelOrTwin(twin, "tmid", "targetModelId");
                if (!string.IsNullOrEmpty(targetModelId))
                {
                    SelfDescribingChecks.CheckId(targetModelId, discoveredModelId);
                }


                model = await modelParser.ParseAsync(new string[] { modelPayload });
                SelfDescribingChecks.CheckExtends(discoveredModelId, model);
            }
            else
            {
                Console.WriteLine("Resolving from repo");
                var models = dmrClient.GetModels(modelId.ToString());
                model = await modelParser.ParseAsync(models.Values.ToArray());
            }
            return model;
        }
    }
}