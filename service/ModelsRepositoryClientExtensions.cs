using Azure.IoT.ModelsRepository;
using Microsoft.Azure.DigitalTwins.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace service
{
    static class ModelsRepositoryClientExtensions
    {
        public static async Task<IEnumerable<string>> ParserDtmiResolver(this ModelsRepositoryClient client, IReadOnlyCollection<Dtmi> dtmis)
        {
            IEnumerable<string> dtmiStrings = dtmis.Select(s => s.AbsoluteUri);
            IDictionary<string, string> result = await client.GetModelsAsync(dtmiStrings);
            return result.Values.ToList();
        }
    }
}