using Microsoft.Azure.Devices.Serialization;
using Microsoft.Azure.DigitalTwins.Parser;
using System;
using System.Collections.Generic;
using System.Web;

namespace service
{
    class SelfDescribingChecks
    {
        public  static string GetPropFromModelOrTwin(BasicDigitalTwin twin, string propName, string twinName)
        {
            if (twin.CustomProperties.ContainsKey(twinName))
            {
                var hashFromProp = twin.CustomProperties[twinName].ToString();
                return hashFromProp;
            }
            else
            {
                Uri modelId = new Uri(twin.Metadata.ModelId);
                var hashFromQS = HttpUtility.ParseQueryString(modelId.Query).Get(propName);
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
        public static void CheckHash(string targetModelHash, string modelPayload)
        {
            string hash = common.Hash.GetHashString(modelPayload);
            if (hash.Equals(targetModelHash, StringComparison.InvariantCulture))
            {
                Console.Write(" Hash check ok.. ");
            }
            else
            {
                throw new InvalidOperationException("Wrong Hash value");
            }
        }

        public static void CheckExtends(string targetModelId, IReadOnlyDictionary<Dtmi, DTEntityInfo> model)
        {
            var root = model.GetValueOrDefault(new Dtmi(targetModelId)) as DTInterfaceInfo;
            if (root.Extends.Count > 0 && root.Extends[0].Id.AbsoluteUri == "dtmi:azure:common:SelfDescribing;1")
            {
                Console.Write(" Extends check ok.. ");
            }
            else
            {
                throw new InvalidOperationException("Root Id does not extends 'dtmi:azure:common:SelfDescribing;1'. " + targetModelId);
            }
        }

        public static void CheckId(string targetModelId, string rootId)
        {
            if (targetModelId == rootId)
            {
                Console.Write(" @Id checks ok.. ");
            }
            else
            {
                throw new InvalidOperationException("Root Id does not match announced Id. " + rootId);
            }
        }
    }
}
