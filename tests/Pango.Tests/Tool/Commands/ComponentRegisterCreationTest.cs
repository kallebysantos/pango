using System.Text.Json;
using Pango.Abstractions;
using Pango.Commands;
using Pango.Services.RegistryManager;
using Pango.Types;
using Spectre.Console.Testing;

namespace Pango.Tests;

public class ComponentRegisterCreationTest : IDisposable
{
    public string ComponentsPath => Path.Combine(Directory.GetCurrentDirectory(), "Mock/Files");
    public string OutputPath => Path.Combine(Directory.GetCurrentDirectory(), "publish-tool");

    public void Dispose()
    {
        var outputFolder = new DirectoryInfo(OutputPath);
        outputFolder.Delete(true);
    }

    [Theory]
    [InlineData(["Button", "button.json", "Button", 5])] // Folder component
    [InlineData(["HelloWorld.razor", "hello-world.json", "HelloWorld", 1])] // Single file component
    public void ShouldCreateRegisterForValidComponents(
        string componentInput,
        string metadataFileName,
        string outputFolderName,
        int generatedFilesCount
    )
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<ComponentRegisterCreation>();

        var componentPath = Path.Combine(ComponentsPath, componentInput);
        var result = app.Run(componentPath, "-o", OutputPath);
        Assert.Equal(0, result.ExitCode);

        var outputMetadataFilePath = Path.Combine(OutputPath, metadataFileName);
        var metadata = JsonSerializer.Deserialize<ComponentMetadata>(
            File.ReadAllText(outputMetadataFilePath),
            options: new(JsonSerializerDefaults.Web)
        );
        Assert.IsType<ComponentMetadata>(metadata);

        var expectedComponentName = Path.GetFileNameWithoutExtension(metadataFileName);
        Assert.Equal(expectedComponentName, metadata.Name);
        Assert.Equal(outputFolderName, metadata.Source);
        Assert.Equal(generatedFilesCount, metadata.Files.Length);

        var outputDir = new DirectoryInfo(Path.Combine(OutputPath, outputFolderName));
        Assert.True(outputDir.Exists);
        Assert.Equal(generatedFilesCount, outputDir.EnumerateFiles().Count());
    }
}
