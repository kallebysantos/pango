# Project Pango

## Overview

This is a .NET project named **Pango**, which appears to be a component management system or tool. Here's a quick rundown:

- **License**: The project is licensed under Apache License 2.0.
- **Solution**: Contains multiple projects like `Pango`, `Pango.Tool`, and `Pango.Tests`.
- **Components**: Includes components like `alert`, `dropdown`, and `button`, managed through a JSON file.
- **Tools**: Utilizes `Spectre.Console` for CLI interactions.
- **Abstractions**: Implements custom types like `Option<T>`, `Result<T, E>`, and various error handling mechanisms.
- **Services**: Features HTTP services for fetching components and a Registry Manager for metadata creation and component packing.

## Directory Structure

```
./LICENSE
./Pango.sln
./README.md
./components.json
./assets/logo.txt
./src/
    Pango.Tool/
        Pango.Tool.csproj
        Program.cs
        Commands/
            ComponentRegisterCreation.cs
            ConfigurationInit.cs
            DownloadComponent.cs
        Helpers/
            CommandsHelper.cs
        Services/
            ServicesHttp.cs
    Pango/
        Pango.csproj
        Abstractions/
            Option.cs
            PatternMatching.cs
            Result.cs
        Converters/
            OptionJsonConverter.cs
        Extensions/
            HttpClientExtensions.cs
            StringExtensions.cs
        Types/
            Component.cs
            ComponentMetadata.cs
            LocalConfig.cs
        Services/
            RegistryManager/
                IRegistryManager.cs
                RegistryManager.cs
    ```

## Key Features

- **Component Management**: Download, initialize, and register components.
- **CLI Tool**: `Pango.Tool` provides command-line interface for managing components.
- **Registry System**: Manages component metadata and ensures proper component packaging.
- **Functional Programming**: Utilizes abstractions like `Option<T>` and `Result<T, E>` for better error handling and type safety.
- **HTTP Services**: Fetches component JSON data from a local server.
- **Compression**: Uses Brotli compression for component files.

## Usage

To use this project, you would typically:

1. **Initialize**: Run `pango init` to set up the configuration.
2. **Download Components**: Use `pango add <component-name>` to fetch components.
3. **Create Metadata**: Use `pango registry create-metadata` to generate metadata for components.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
