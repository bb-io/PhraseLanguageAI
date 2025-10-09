using Apps.Appname.Actions;
using Apps.Appname.Handlers;
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
    public async Task TranslateFileGenericPretranslate_WithoutTransMemories_IsSuccess()
    {
        // Arrange
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
        var emptyTransMemories = new TransMemoriesConfig { };

        // Act
        var response = await actions.TranslateFileGenericPretranslate(fileInput, emptyTransMemories);

        // Assert
        Assert.IsNotNull(response);
    }
    
    [TestMethod]
    public async Task TranslateFileGenericPretranslate_WithTransMemories_IsSuccess()
    {
        // Arrange
        var actions = new TranslateActions(InvocationContext, FileManager);
        var fileInput = new TranslateFileInput
        {
            SourceLang = "en_us",
            TargetLanguage = "ar",
            File = new FileReference
            {
                Name = "to-translate.html"
            },
            FileTranslationStrategy = "blackbird",
            Uid= "aAyctAOzRwUVcF5L01HKo0"
        };
        var transMemory = new TransMemoriesConfig
        {
            TransMemoryUid = "H3mIrir42i854ZecHkafg3",
        };

        // Act
        var response = await actions.TranslateFileGenericPretranslate(fileInput, transMemory);

        // Assert
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
}
