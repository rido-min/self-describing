using Azure.IoT.ModelsRepository;
using Microsoft.Azure.DigitalTwins.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace service
{
    static class ModelsRepositoryClientExtensions
    {
        public static async Task<IEnumerable<string>> ParserDtmiResolver(this ModelsRepositoryClient client, IReadOnlyCollection<Dtmi> dtmis)
        {
            IEnumerable<string> dtmiStrings = dtmis.Select(s => s.AbsoluteUri);
            List<string> modelDefinitions = new List<string>();
            foreach (var dtmi in dtmiStrings)
            {
                ModelResult result = await client.GetModelAsync(dtmi);
                modelDefinitions.Add(result.Content[dtmi]);
            }
            return modelDefinitions;
        }
    }
}