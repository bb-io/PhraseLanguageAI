using Apps.Appname.Handlers;
using Apps.PhraseLanguageAI.Actions;
using Apps.PhraseLanguageAI.Models.Request;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.Appname.Base;

namespace Tests.Appname;

[TestClass]
public class TranslateTests : TestBase
{
    [TestMethod]
    public async Task TranslateText_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext,FileManager);

        var response = await actions.TranslateText(new TranslateTextInput { TargetLanguage = "es", Text="Hello my dear friend how are you? This is a text that should be longer than 50 characters."});

        Console.WriteLine($"{response.SourceLang} - {response.TargetLang} - {response.TranslatedText}");
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task UploadFileGeneric_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "en",
            TargetLanguage = "es",
            File = new FileReference
            {
                Name = "test.txt"
            }
        };

        var response = await actions.UploadFileForTranslation(fileInput, "QUALITY_ESTIMATION", null);

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

        var response = await actions.DownloadFileTranslation("9S0QGXC6ANkz42EI4OnqC0", "MT_GENERIC_PRETRANSLATE", "es", "test.html");
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task TranslateFileGenericPretranslate_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "en_us",
            TargetLanguage = "es_419",
            Uid = "QC7pl7aJ7jaq02ypFz9ZR4",
            File = new FileReference
            {
                Name = "test.html"
            },
            FileTranslationStrategy = "plai",
            OutputFileHandling = "original",
        };

        var response = await actions.TranslateFileGenericPretranslate(fileInput, null);

        Assert.IsNotNull(response);
    }
    
    [TestMethod]
    public async Task TranslateFileGenericPretranslate_WithTransMemories_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "en",
            TargetLanguage = "fi_fi",
            //Uid = "QC7pl7aJ7jaq02ypFz9ZR4",
            File = new FileReference
            {
                Name = "test.html"
            },
            FileTranslationStrategy = "plai",
            OutputFileHandling = "original",
        };
        var transMemory = new TransMemoriesConfig
        {
            TargetLanguage = "fi_fi",
            TransMemoryUid = "TC0zd28l9IQ9tG6uYkiIt3",
            TmSourceLanguage = "en",
            TmTargetLanguage = "fi_fi"
        };

        var response = await actions.TranslateFileGenericPretranslate(fileInput, transMemory);

        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task GetFileScore_IsSuccess()
    {
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "aa",
            TargetLanguage = "es",
            File = new FileReference
            {
                Name = "test.docx"
            }
        };

        var response = await actions.TranslateFileWithQualityEstimation(fileInput, null);

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
