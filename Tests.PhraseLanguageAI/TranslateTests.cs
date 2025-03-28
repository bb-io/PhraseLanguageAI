using Apps.Appname.Actions;
using Apps.Appname.Handlers;
using Apps.PhraseLanguageAI.Models.Request;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Tests.Appname.Base;

namespace Tests.Appname;

[TestClass]
public class TranslateTests : TestBase
{
    [TestMethod]
    public async Task TranslateText_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext,FileManager);

        var response = await actions.TranslateText(new TranslateTextInput { SourceLang= "en", TargetLang="es", Text="Hello my dear friend how are you?"});

        Console.WriteLine($"{response.SourceLang} - {response.TargetLang} - {response.TranslatedTexts}");
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task UplloadFileGeneric_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "en",
            TargetLang = "es",
            File = new FileReference
            {
                Name = "test.txt"
            }
        };

        var response = await actions.UploadFileForTranslation(fileInput, "QUALITY_ESTIMATION");

        Console.WriteLine($"{response.Uid} - {response.Actions} - ");
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task GetFileStatus_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);

        var response = await actions.GetFileTranslationStatus("n4z039HevEn3kGcQl01f3t");

        Console.WriteLine($"{response.Uid} - {response.Actions} - ");
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task DownloadFile_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);

        var response = await actions.DownloadFileTranslation("0rWrCIa3kvMCsW1LCGE0K7", "MT_GENERIC_PRETRANSLATE", "es", "test.html");
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task TranslateFileGenericPretranslate_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "en",
            TargetLang = "es",
            File = new FileReference
            {
                Name = "test.html"
            }
        };

        var response = await actions.TranslateFileGenericPretranslate(fileInput);

        Assert.IsNotNull(response);
    }


    [TestMethod]
    public async Task GetFileScore_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "en",
            TargetLang = "es",
            File = new FileReference
            {
                Name = "test.txt"
            }
        };

        var response = await actions.TranslateFileWithQualityEstimation(fileInput);

        Console.WriteLine($"{response.Uid} - {response.Score}");
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task LanguageProfileReturnsValues()
    {
        var action = new LanguageAiProfilesDataHandler(InvocationContext);
       
        var response = await action.GetDataAsync(new DataSourceContext { }, CancellationToken.None);
        foreach (var profile in response)
        {
            Console.WriteLine($"{profile.Key} - {profile.Value}");
        }
    }

}
