using Pango.Abstractions;
using Pango.Types;

namespace Pango.Services.RegistryManager;

public interface IRegistryError;

public record struct InvalidComponentPathError(string? Path) : IRegistryError;

public record struct CreateComponentMetadataInput(
    string LocalComponentPath
);

public record struct PackComponentInput(
    ComponentMetadata Metadata,
    string InputBasePath,
    string OutputPath
);

public interface IRegistryManager
{
  Result<ComponentMetadata, IRegistryError> CreateComponentMetadata(CreateComponentMetadataInput metadataInput);

  Task<Result<ComponentMetadata, IRegistryError>> PackComponent(PackComponentInput packInput);
}
