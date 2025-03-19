using Pango.Abstractions;
using Pango.Services.RegistryManager;
using Pango.Types;

namespace Pango.Tests;

public class CreateMetadataTests : RegistryManagerTests
{
    [Theory]
    [InlineData(["button", "Button", 4])] // Folder component
    [InlineData(["hello-world", "HelloWorld.razor", 1])] // Single file component
    public void ShouldCreateMetadataForValidComponent(string name, string component, int fileCount)
    {
        var manager = new RegistryManager();

        var componentPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            ComponentsFolder,
            component
        );

        var componentMetadata = manager.CreateComponentMetadata(new(componentPath));

        Assert.True(componentMetadata.IsOk());

        var metadata = componentMetadata.Expect();
        Assert.IsType<ComponentMetadata>(metadata);

        Assert.Equal(expected: name, actual: metadata.Name);
        Assert.EndsWith(
            expectedEndString: component.Replace(".razor", string.Empty),
            actualString: metadata.Source
        );
        Assert.Equal(expected: fileCount, actual: metadata.Files.Length);
    }

    [Theory]
    [InlineData("NotComponent.cs")]
    [InlineData("NotComponent")]
    [InlineData("*.razor")]
    [InlineData(".razor")]
    [InlineData(".")]
    public void ShouldErrorWhenInvalidComponent(string component)
    {
        var manager = new RegistryManager();

        var componentPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            ComponentsFolder,
            component
        );

        var componentMetadata = manager.CreateComponentMetadata(new(componentPath));

        Assert.True(componentMetadata.IsErr());
        Assert.IsType<InvalidComponentPathError>(componentMetadata.ExpectErr());
    }
}
