using System.IO.Compression;
using System.Net;
using System.Text;
using Pango.Abstractions;
using Pango.Abstractions.ErrorKinds;
using Pango.Extensions;
using Pango.Types;

namespace Pango.Services.RegistryClient;

public interface IComponentError;

public interface IComponentState;

public record struct Resolved : IComponentState;

public record struct Streaming(Stream FileStream, string FileName) : IComponentState;

public record struct Downloaded(string Filepath) : IComponentState;

public record struct Component<TState>(ComponentMetadata Metadata, TState State)
    where TState : IComponentState, new()
{
    public Component(ComponentMetadata Metadata)
        : this(Metadata, new()) { }
}

public record struct ComponentStreamLine(string Line)
{
    static readonly string[] namespacePatterns = ["namespace", "@namespace"];

    const int NotFoundIndex = -1;

    readonly Option<int> NamespaceWordIndex =>
        Option
            .From(Line)
            .Filter(line => namespacePatterns.Any(pattern => line.TrimStart().StartsWith(pattern)))
            .Map(line => line.IndexOf("namespace"))
            .DiscardIf(idx => idx == NotFoundIndex);

    public readonly bool IsNamespaceLine => NamespaceWordIndex.IsSome();

    ComponentStreamLine ApplyLineEnding(string fileExtension) =>
        fileExtension.EndsWith(".razor") ? this : this with { Line = Line + ';' };

    public readonly ComponentStreamLine ApplyNamespace(string @namespace, string fileExtension)
    {
        if (NamespaceWordIndex is not Some<int> namespaceIdx)
            return this;

        var updatedNamespaceLine = this with
        {
            Line = Line.Replace(
                oldValue: Line[namespaceIdx.Value..],
                newValue: $"namespace {@namespace}"
            ),
        };

        return updatedNamespaceLine.ApplyLineEnding(fileExtension);
    }

    public static implicit operator string(ComponentStreamLine streamLine) => streamLine.Line;

    public static Option<ComponentStreamLine> From(string? line) =>
        Option.From(line).Map(line => new ComponentStreamLine(line));
}

public static class Component
{
    /// <summary>
    /// Resolves a component metadata by fetching the registry
    /// </summary>
    /// <param name="httpClient">A HttpClient instance</param>
    /// <param name="remoteUri">The registry uri to fetch</param>
    /// <param name="name">The name of the component to be resolved</param>
    /// <returns>The resolved component</returns>
    public static async Task<Result<Component<Resolved>, IError>> ResolveFrom(
        HttpClient httpClient,
        string name
    )
    {
        var componentMetadataName = Path.ChangeExtension(name, "json");

        var response = await httpClient.GetAsync(componentMetadataName);

        var result = await response
            .ToResult()
            .MapErr(err =>
                err.HttpResponse.StatusCode == HttpStatusCode.NotFound
                    ? new NotFoundError().WithMessage("Component could not be found or not exists!")
                    : err.Error
            )
            .AndThen(HttpClientExtensions.ReadFromJsonAsync<ComponentMetadata>);

        return result.Map(metadata => new Component<Resolved>(metadata));
    }
}

public static class ResolvedComponent
{
    /// <summary>
    /// Enumerates the component source and yield the files as streams
    /// </summary>
    /// <param name="component"></param>
    /// <param name="httpClient"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<Result<Component<Streaming>, IError>> GetComponentStreams(
        this Component<Resolved> component,
        HttpClient httpClient
    )
    {
        string GetFileUrl(string file) => Path.Combine(component.Metadata.Source, file);

        Task<Result<Stream, Exception>> GetFileStream(string fileUrl) =>
            Result.TryFrom(() => httpClient.GetStreamAsync(fileUrl));

        foreach (var filename in component.Metadata.Files)
        {
            var fileUrl = GetFileUrl(filename);
            var streamResult = await GetFileStream(fileUrl);

            yield return streamResult
                .MapErr(ExceptionError.From)
                .Map(stream => new Component<Streaming>(
                    Metadata: component.Metadata,
                    State: new(FileStream: stream, FileName: filename)
                ));
        }
    }
}

public static class StreamingComponent
{
    /// <summary>
    /// Writes the component stream to a local component file,
    /// also creates components subfolder and apply the target namespace
    /// </summary>
    /// <param name="component"></param>
    /// <param name="localBaseComponentPath"></param>
    /// <param name="localBaseNamespace"></param>
    /// <returns>The downloaded component</returns>
    public static async Task<Result<Component<Downloaded>, IError>> Download(
        this Component<Streaming> component,
        string localBaseComponentPath,
        string localBaseNamespace
    )
    {
        var isFolderComponent = component.Metadata.Files.Length > 1;
        var componentFilePath = isFolderComponent
            ? Path.Combine(component.Metadata.Name.ToTitleCase(), component.State.FileName)
            : component.State.FileName;

        var filepath = new FileInfo(
            fileName: Path.Combine(localBaseComponentPath, componentFilePath)
        );

        filepath.Directory?.Create();

        var outputFilePath = filepath.FullName.EndsWith(".br")
            ? new StringBuilder(filepath.FullName)
                .Remove(filepath.FullName.Length - 3, 3) // removes ".br" Brotli exentsion
                .ToString()
            : filepath.FullName;

        var writeResult = await WriteComponentStream(
            stream: component.State.FileStream,
            filepath: outputFilePath,
            @namespace: localBaseNamespace
        );

        return writeResult.Map(_ => new Component<Downloaded>(
            Metadata: component.Metadata,
            State: new(Filepath: outputFilePath)
        ));
    }

    /// <summary>
    /// Writes the component stream to a file
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="filepath"></param>
    /// <param name="namespace"></param>
    /// <returns></returns>
    public static async Task<Result<IOk, IError>> WriteComponentStream(
        Stream stream,
        string filepath,
        string @namespace
    )
    {
        using var contentStream = new MemoryStream();
        using var decompressor = new BrotliStream(stream, CompressionMode.Decompress);
        await decompressor.CopyToAsync(contentStream);
        contentStream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(contentStream);
        using var writer = new StreamWriter(filepath);

        while (
            ComponentStreamLine.From(await reader.ReadLineAsync()) is Some<ComponentStreamLine> line
        )
        {
            await writer.WriteLineAsync(
                value: line.Value.ApplyNamespace(@namespace: @namespace, fileExtension: filepath)
            );
        }

        return new OkResult();
    }
}
