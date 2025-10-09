using Apps.Appname.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Tests.Appname.Base;

namespace Tests.PhraseLanguageAI;
[TestClass]
public class DataSourceTests : TestBase
{
    [TestMethod]
    public async Task GetDataAsync_Profiles()
    {
        var handler = new LanguageAiProfilesDataHandler(InvocationContext);
        var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
        Assert.AreNotEqual(data.Count(), 0);
    }
}
