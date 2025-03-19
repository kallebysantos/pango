using System.IO.Compression;
using Pango.Abstractions;
using Pango.Services.RegistryManager;
using Pango.Types;

namespace Pango.Tests;

public class PackComponentTests : RegistryManagerTests, IDisposable
{
    static string ComponentsPath => Path.Combine(Directory.GetCurrentDirectory(), ComponentsFolder);
    static string OutputPath => Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);

    public void Dispose()
    {
        var outputFolder = new DirectoryInfo(OutputPath);
        outputFolder.Delete(true);
    }

    [Theory]
    [InlineData(["Button"])] // Folder component
    [InlineData(["HelloWorld.razor"])] // Single file component
    public async Task ShouldPackComponent(string component)
    {
        var manager = new RegistryManager();

        var componentPath = Path.Combine(ComponentsPath, component);

        var metadata = manager
            .CreateComponentMetadata(new CreateComponentMetadataInput(componentPath))
            .Expect();

        var packResult = await manager.PackComponent(
            new PackComponentInput(metadata, ComponentsFolder, OutputPath)
        );
        Assert.True(packResult.IsOk());

        var packedMetadata = packResult.Expect();
        Assert.IsType<ComponentMetadata>(packedMetadata);

        Assert.Equal(expected: packedMetadata.Name, actual: metadata.Name);
        Assert.Equal(expected: packedMetadata.Source, actual: metadata.Source);
        Assert.Equal(expected: packedMetadata.Files.Length, actual: metadata.Files.Length);

        foreach (var fileName in packedMetadata.Files)
        {
            var packedFile = new FileInfo(
                Path.Combine(OutputPath, packedMetadata.Source, fileName)
            );

            Assert.NotNull(packedFile);
            Assert.True(packedFile.Exists);
            Assert.Equal(RegistryManager.PackFileExtension, packedFile.Extension);

            // Components can be folder or single
            var originalFileName =
                packedMetadata.Files.Length > 1
                    ? Path.Combine(packedMetadata.Source, fileName)
                    : fileName;

            var originalFilePath = Path.Combine(
                ComponentsPath,
                originalFileName[..^RegistryManager.PackFileExtension.Length] // removes the ".br" extension
            );

            var originalFileBytes = await File.ReadAllBytesAsync(originalFilePath);
            var packedFileBytes = await File.ReadAllBytesAsync(packedFile.FullName);

            var output = new Span<byte>(originalFileBytes);
            var hasDecompressed = BrotliDecoder.TryDecompress(
                packedFileBytes,
                output,
                out var bytesWritten
            );

            Assert.True(hasDecompressed);
            Assert.Equal(originalFileBytes.Length, bytesWritten);
            Assert.True(originalFileBytes.AsSpan().SequenceEqual(output));
        }
    }
}
