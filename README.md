# Project Pango

## Overview
Pango is a .NET management system for components, providing:
- A CLI for managing components
- HTTP services for component metadata

## Project Structure
- **Pango**: The core library
- **Pango.Tool**: Command-line tool for component management
- **Pango.Tests**: Test suite for the project
- **Abstractions**: Common interfaces and types

## Features
- **CLI**: Commands for:
  - Initializing configurations
  - Adding components
  - Creating metadata
- **Component Management**: 
  - Download components from a registry
  - Pack components into a compressed format
  - Metadata creation

## Usage
### Initializing
```bash
pango init --registry-uri <your-registry-uri> --namespace <your-namespace>
```

### Adding Components
```bash
pango add --name <component-name> --output <output-directory>
```

### Creating Metadata
```bash
pango registry create-metadata --source <component-source-path> --output <metadata-output-directory>
```

## Configuration
- **LocalConfig**: Defines:
  - `RegistrySchemaUri`: The URI of the component registry
  - `TargetComponentNamespace`: Namespace for components
  - `LocalComponentPath`: Path where components are stored

## Components
- **Component Structure**: Includes:
  - `Name`: Name of the component
  - `Source`: Source path or URL
  - `Files`: List of files associated with the component

## Error Handling
- **IOErrorKind**: Defines errors like:
  - `FileTooLargeError`
  - `NotFoundError`
  - `PermissionDeniedError`

## License
This project is licensed under the Apache License, Version 2.0.

## Dependencies
- **Spectre.Console**: For CLI operations
- **System.Text.Json**: For JSON serialization/deserialization
- **System.IO.Compression**: For file compression

## Project Files
- **LICENSE**: Apache License 2.0
- **Pango.sln**: Visual Studio solution file
- **components.json**: JSON file listing components
- **assets/logo.txt**: ASCII art logo
- **src/Pango.Tool/Pango.Tool.csproj**: Project file for the CLI tool
- **src/Pango/Pango.csproj**: Project file for the core library

## Development
- **SDK**: Microsoft.NET.Sdk
- **Target Framework**: .NET 8.0
- **Testing**: Unit tests in Pango.Tests
- **Documentation**: This README.md file

For more detailed information on each part of the project, refer to the respective files or the source code.