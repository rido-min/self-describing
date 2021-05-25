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
        static string deviceId = "mySSDDevice";

        static async Task Main(string[] args)
        {
            var respt = await dtc.GetDigitalTwinAsync<BasicDigitalTwin>(deviceId);
            Console.WriteLine($"Device '{deviceId}' announced: {respt.Body.Metadata.ModelId}\n");

            var model = await ResolveAndParse(respt.Body);
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

            Uri mid = new Uri(twin.Metadata.ModelId);
            if (mid.AbsolutePath == "std:selfreporting;1")
            {
                Console.WriteLine("Device is Self Reporting. Querying device for the model . . ");

                var resp = await dtc.InvokeCommandAsync(deviceId, "GetModel");
                string modelPayload = resp.Body.Payload;
                Console.Write("Device::GetModel() ok..");

                string expectedHash = GetExpectedHash(twin);
                string expectedId = GetExpectedId(twin);

                if (!string.IsNullOrEmpty(expectedHash))
                {
                    CheckHash(expectedHash, modelPayload);
                }
                
                model = await modelParser.ParseAsync(new string[] { modelPayload });

                if (!string.IsNullOrEmpty(expectedId))
                {
                    CheckId(modelPayload, expectedId);
                    CheckExtends(model, expectedId);
                }

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

        private static string GetExpectedHash(BasicDigitalTwin twin)
        {
            Uri mid = new Uri(twin.Metadata.ModelId);
            var hashFromQS = HttpUtility.ParseQueryString(mid.Query).Get("SHA256"); 
            if (!string.IsNullOrEmpty(hashFromQS))
            {
                return hashFromQS;
            } else
            {
                var hashFromProp = twin.CustomProperties["ReportedModelHash"].ToString();
                if (!string.IsNullOrEmpty(hashFromProp))
                {
                    return hashFromProp;
                }
                else
                {
                    Console.WriteLine("Hash not found");
                    return null;
                }
            }
        }

        private static string GetExpectedId(BasicDigitalTwin twin)
        {
            Uri mid = new Uri(twin.Metadata.ModelId);
            var idFromQS = HttpUtility.ParseQueryString(mid.Query).Get("id");
            if (!string.IsNullOrEmpty(idFromQS))
            {
                return idFromQS;
            }
            else
            {
                var idFromProp = twin.CustomProperties["ReportedModelId"].ToString();
                if (!string.IsNullOrEmpty(idFromProp))
                {
                    return idFromProp;
                }
                else
                {
                    Console.WriteLine("Hash not found");
                    return null;
                }
            }
        }

        private static void CheckHash(string expectedHash, string modelPayload)
        {
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

        private static void CheckExtends(IReadOnlyDictionary<Dtmi, DTEntityInfo> model, string expectedId)
        {
            
            var root = model.GetValueOrDefault(new Dtmi(expectedId)) as DTInterfaceInfo;
            if (root.Extends.Count > 0 && root.Extends[0].Id.AbsoluteUri == "dtmi:std:selfreporting;1")
            {
                Console.Write(" Extends check ok.. ");
            }
            else
            {
                throw new ApplicationException("Root Id does not extends std:selfreporting. " + expectedId);
            }
        }

        private static void CheckId(string modelPayload, string expectedId)
        {
            var rootId = JsonDocument.Parse(modelPayload).RootElement.GetProperty("@id").GetString();
            if (expectedId == rootId)
            {
                Console.Write(" @Id checks ok ..");
            }
            else
            {
                throw new ApplicationException("Root Id does not match announced Id. " + rootId);
            }
        }
    }
}