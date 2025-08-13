using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Pango.Abstractions;
using Pango.Services.RegistryClient;
using Pango.Abstractions.ErrorKinds;
using Pango.Extensions;
using Pango.Types;
using Spectre.Console;
using Spectre.Console.Cli;
using ComponentModel = System.ComponentModel;
using System.Net;

namespace Pango.Commands;

public sealed class ListComponentsSettings : CommandSettings
{
  [NotNull]
  [ComponentModel.Description("URI of the registry to fetch the component.")]
  [CommandOption("--registry-uri")]
  public required string RegistryUri { get; set; }

  [NotNull]
  [ComponentModel.Description("Output folder for the component download.")]
  [CommandOption("-o|--output")]
  public required string Output { get; set; }

  [NotNull]
  [ComponentModel.Description("Target namespace.")]
  [CommandOption("-n|--namespace")]
  public required string Namespace { get; set; }
}

public sealed class ListComponents : AsyncCommand<ListComponentsSettings>
{
  public static async Task<Result<IOk, IError>> List(ListComponentsSettings settings)
  {

    AnsiConsole.MarkupLineInterpolated(
        $"[bold grey]Loading Components:[/] [underline]{settings.RegistryUri}[/]"
    );

    var httpClient = new HttpClient() { BaseAddress = new Uri(settings.RegistryUri) };
    var listComponentsResult = (
        await AnsiConsole
            .Status()
            .StartAsync(
                "Fetching registry index...",
                async ctx =>
                {

                  var response = await httpClient.GetAsync("index.json");

                  return await response
                      .ToResult()
                      .MapErr(err =>
                          err.HttpResponse.StatusCode == HttpStatusCode.NotFound
                              ? new NotFoundError().WithMessage("Registry index could not be found or not exists!")
                              : err.Error
                      )
                      .AndThen(HttpClientExtensions.ReadFromJsonAsync<string[]>);
                }
            )
    )
        .Inspect(result =>
            AnsiConsole.MarkupLineInterpolated(
                $"[bold grey]Fetch:[/] found {result.Length} components."
            )
        )
        .InspectErr(err => AnsiConsole.MarkupLineInterpolated($"[bold red]Fail: {err}[/]."));

    if (listComponentsResult.IsErr())
      return Result.Err<IOk, IError>(listComponentsResult.ExpectErr());

    var selectedComponents = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("Select components to install")
            .NotRequired() // Not required to have a favorite fruit
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to scroll)[/]")
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle a component, " +
                "[green]<enter>[/] to accept)[/]")
            .AddChoices(listComponentsResult.Expect()));

    var config = new LocalConfig()
    {
      RegistrySchemaUri = settings.RegistryUri,
      TargetComponentNamespace = settings.Namespace,
      LocalComponentPath = settings.Output,
    };

    foreach (string component in selectedComponents)
    {
      await DownloadComponent.AddComponent(config, component);
    }

    return new OkResult();
  }

  public override async Task<int> ExecuteAsync(
      [NotNull] CommandContext context,
      [NotNull] ListComponentsSettings settings
  )
  {
    var result = await List(settings);

    return Convert.ToInt32(result.IsOk());
  }

  public override ValidationResult Validate(
      CommandContext context,
      ListComponentsSettings settings
  )
  {
    var loadConfigTask = AnsiConsole
        .Status()
        .StartAsync(
            "Loading pango configuration file",
            async ctx =>
            {
              var configFileInfo = new FileInfo("./pango-ui.config.json");

              if (!configFileInfo.Exists)
              {
                AnsiConsole.MarkupLine(
                        "[grey]No configuration file found, try run [/][underline]pango init[/]"
                    );
                return;
              }

              using var configFileStream = configFileInfo.OpenRead();
              var config = await JsonSerializer.DeserializeAsync<LocalConfig>(
                      configFileStream
                  );

              settings.Namespace ??= config.TargetComponentNamespace;
              settings.Output ??= config.LocalComponentPath;
              settings.RegistryUri ??= config.RegistrySchemaUri;
            }
        );

    loadConfigTask.Wait();

    if (settings.Namespace is null)
    {
      return ValidationResult.Error("Must specify a target namespace.");
    }

    if (settings.Output is null)
    {
      return ValidationResult.Error("Must specify a output folder.");
    }

    if (settings.RegistryUri is null)
    {
      return ValidationResult.Error("Must specify a component registry URI.");
    }

    return base.Validate(context, settings);
  }
}
