using Apps.Appname.Actions;
using Apps.PhraseLanguageAI.Models.Request;
using Newtonsoft.Json;
using Tests.Appname.Base;

namespace Tests.Appname;

[TestClass]
public class TranslateTests : TestBase
{
    [TestMethod]
    public async Task GetLanguageList_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext);

        var response = await actions.ListTranslationProfiles();

        foreach (var item in response.Content)
        {
            Console.WriteLine($"{item.Uid} - {item.Name}");
            Assert.IsNotNull(item);
        }
    }
    //

    [TestMethod]
    public async Task TranslateText_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext);

        var response = await actions.TranslateText(new TranslateTextInput { SourceLang= "en", TargetLang="es", Text="Hello my dear friend how are you?"});

        Console.WriteLine($"{response.SourceLang} - {response.TargetLang} - {response.TranslatedTexts}");
        Assert.IsNotNull(response);
    }
}
