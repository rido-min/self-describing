﻿using System;
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
        static string deviceId = "ssd01";

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
            if (mid.AbsolutePath == "azure:common:SelfDescribing;1")
            {
                Console.WriteLine("Device is Self Reporting. Querying device for the model . . ");

                var resp = await dtc.InvokeCommandAsync(deviceId, "GetTargetModel");
                string modelPayload = resp.Body.Payload;
                Console.Write("Device::GetTargetModel() ok..");

                string expectedHash = GetPropFromModelOrTwin(twin, "tmhash", "TargetModelHash");
                string expectedId = GetPropFromModelOrTwin(twin, "tmid", "TargetModelId");

                if (!string.IsNullOrEmpty(expectedHash))
                {
                    CheckHash(expectedHash, modelPayload);
                    Console.WriteLine("Hash check succeed\n");
                }

                model = await modelParser.ParseAsync(new string[] { modelPayload });

                if (!string.IsNullOrEmpty(expectedId))
                {
                    CheckId(modelPayload, expectedId);
                    CheckExtends(model, expectedId);
                    Console.WriteLine("Self Describing protocol checks succeed\n");
                }
                Console.WriteLine("\n Parsed Model \n");
            }
            else
            {
                Console.WriteLine("Resolving from repo");
                var models = dmrClient.GetModels(mid.ToString());
                model = await modelParser.ParseAsync(models.Values.ToArray());
            }
            return model;
        }


        private static string GetPropFromModelOrTwin(BasicDigitalTwin twin, string propName, string twinName)
        {
            if (twin.CustomProperties.ContainsKey(twinName))
            {
                var hashFromProp = twin.CustomProperties[twinName].ToString();
                return hashFromProp;
            }
            else
            {
                Uri mid = new Uri(twin.Metadata.ModelId);
                var hashFromQS = HttpUtility.ParseQueryString(mid.Query).Get(propName);
                if (!string.IsNullOrEmpty(hashFromQS))
                {
                    return hashFromQS;
                }
                else
                {
                    Console.Write(".. Hash not found .. ");
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
            if (root.Extends.Count > 0 && root.Extends[0].Id.AbsoluteUri == "dtmi:azure:common:SelfDescribing;1")
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