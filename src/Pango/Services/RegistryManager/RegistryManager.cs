using System.IO.Compression;
using Pango.Abstractions;
using Pango.Extensions;
using Pango.Types;

namespace Pango.Services.RegistryManager;

public class RegistryManager() : IRegistryManager
{
    public const string PackFileExtension = ".br"; // Brotli
    public const string RazorFileExtension = ".razor"; // Brotli

    public Result<ComponentMetadata, IRegistryError> CreateComponentMetadata(
        CreateComponentMetadataInput metadataInput
    )
    {
        var isComponentPath = Path.HasExtension(metadataInput.LocalComponentPath);

        if (isComponentPath)
            return ResolveComponent(metadataInput.LocalComponentPath);

        var componentFolder = Result.TryFrom(
            () => new DirectoryInfo(metadataInput.LocalComponentPath)
        );
        if (componentFolder.IsErr())
            return new InvalidComponentPathError();

        return componentFolder
            .Ok()
            .Filter(dir => dir.Exists)
            .Filter(dir =>
                dir.Name == Path.GetFileNameWithoutExtension(metadataInput.LocalComponentPath)
            )
            .Map(dir => dir.EnumerateFiles($"*{RazorFileExtension}*"))
            .Filter(files => files.Any())
            .OkOr<IRegistryError>(new InvalidComponentPathError())
            .AndThen(files =>
                ResolveComponent(
                        files.First(f => f.Extension.StartsWith(RazorFileExtension)).FullName
                    )
                    .Map(component =>
                        component with
                        {
                            Files = [.. files.Select(f => f.Name).Reverse()],
                        }
                    )
            );
    }

    protected static Result<ComponentMetadata, IRegistryError> ResolveComponent(string path) =>
        Result
            .TryFrom(() => new FileInfo(path))
            .Ok()
            .Filter(file => file.Exists)
            .Filter(file => file.Extension == RazorFileExtension)
            .Map(file => new ComponentMetadata(
                Name: ResolveComponentName(file),
                Source: ResolveComponentSource(file),
                Files: [file.Name]
            ))
            .OkOr<IRegistryError>(new InvalidComponentPathError());

    protected static string ResolveComponentName(FileInfo componentFile) =>
        componentFile.Name.Replace(componentFile.Extension, string.Empty).ToKebabCase();

    protected static string ResolveComponentSource(FileInfo componentFile) =>
        componentFile.Name.Replace(componentFile.Extension, string.Empty);

    public async Task<Result<ComponentMetadata, IRegistryError>> PackComponent(
        PackComponentInput packInput
    )
    {
        // TODO: Maybe use AsyncEnumerable to yield compressed files
        var toPackTasks = packInput.Metadata.Files.Select(async fileName =>
        {
            // TODO: Consider using StringBuilder to improve performance
            var originalFileExtension = Path.GetExtension(fileName);
            var outputFileName = Path.ChangeExtension(
                fileName,
                originalFileExtension + PackFileExtension
            );
            var ouputDir = Path.Combine(packInput.OutputPath, packInput.Metadata.Source);

            // Ensures ouput dir exists
            Directory.CreateDirectory(ouputDir);

            var inputFilePath =
                packInput.Metadata.Files.Length > 1
                    ? Path.Combine(packInput.Metadata.Source, fileName)
                    : fileName;

            using var inputFileStream = File.OpenRead(
                Path.Combine(packInput.InputBasePath, inputFilePath)
            );
            using var outputFileStream = File.Create(
                Path.Combine(packInput.OutputPath, ouputDir, outputFileName)
            );
            using var compressor = new BrotliStream(outputFileStream, CompressionLevel.Optimal);
            await inputFileStream.CopyToAsync(compressor);

            return outputFileName;
        });

        return packInput.Metadata with
        {
            Files = await Task.WhenAll(toPackTasks),
        };
    }
}
