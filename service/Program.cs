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
        static string repo = "https://raw.githubusercontent.com/iotmodels/iot-plugandplay-models/selfdescribing";
        static IDictionary<string, string> cache = new Dictionary<string, string>();

        static async Task Main(string[] args)
        {
            var respt = await dtc.GetDigitalTwinAsync<BasicDigitalTwin>("self");
            Console.WriteLine(respt.Body.Metadata.ModelId);

            var model = await ResolveAndParse(respt.Body.Metadata.ModelId);
            model.ToList().ForEach(i => Console.WriteLine(i.Key));

            Console.ReadLine();
        }

        private static async Task<IReadOnlyDictionary<Dtmi, DTEntityInfo>> ResolveAndParse(string modelId)
        {
            var mid = new Uri(modelId);
            ModelsRepositoryClient dmrClient = new ModelsRepositoryClient(new Uri(repo));
            ModelParser modelParser = new ModelParser() 
            {
                DtmiResolver = dmrClient.ParserDtmiResolver
            };

            IReadOnlyDictionary<Dtmi, DTEntityInfo> model;

            if (mid.AbsolutePath == "std:selfreporting;1")
            {
                Console.WriteLine("Device is Self Reporting");
                var expectedHash = HttpUtility.ParseQueryString(mid.Query).Get("hash");
                string modelPayload;
                if (cache.ContainsKey(expectedHash))
                {
                    modelPayload = cache[expectedHash];
                    Console.WriteLine("Model found in cache");
                }
                else
                {
                    Console.WriteLine("Querying device for the model");
                    var resp = await dtc.InvokeCommandAsync("self", "GetModel");
                    modelPayload = resp.Body.Payload;
                    string hash = common.Hash.GetHashString(modelPayload);
                    if (hash.Equals(expectedHash))
                    {
                        Console.WriteLine("Hash validation passed");
                        cache.Add(hash, resp.Body.Payload);
                    }
                    else
                    {
                        throw new ApplicationException("Wrong Hash value");

                    }
                }
                model = await modelParser.ParseAsync(new string[] { modelPayload });
                var rootId = GetRootId(modelPayload);
                var root = model.GetValueOrDefault(new Dtmi(rootId)) as DTInterfaceInfo;
                if (root.Extends.Count > 0 && root.Extends[0].Id.AbsoluteUri == "dtmi:std:selfreporting;1")
                {
                    Console.WriteLine("Extends Check OK\n");
                } 
                else
                {
                    throw new ApplicationException("Root Id does not extends std:selfreporting. " + rootId);
                }
            }
            else
            {
                var models = dmrClient.GetModels(modelId);
                model = await modelParser.ParseAsync(models.Values.ToArray());
            }
            return model;
        }

        

        static string GetRootId(string modelPayload)
        {
            var doc = JsonDocument.Parse(modelPayload).RootElement;
            string id;
            if (doc.ValueKind == JsonValueKind.Array)
            {
                id = JsonDocument.Parse(modelPayload).RootElement[0].GetProperty("@id").GetString();
            }
            else
            {
                id = JsonDocument.Parse(modelPayload).RootElement.GetProperty("@id").GetString();
            }
            return id;
        }
    }
}
