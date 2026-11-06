# Quantum.Plugin.Calendar

[简体中文](../zh-CN/README.md)

Calendar entries, tasks and reminders with local persistence and localized Blazor pages.

[Architecture](architecture.md) · [Development](contributing.md) · [Glossary](glossary.md)

Requires .NET 10. Dependencies restore from public NuGet. The published [SDK](glossary.md#sdk) version is fixed in Directory.Packages.props. No sibling checkout is needed.

```sh
dotnet restore Quantum.Plugin.Calendar.slnx
dotnet test Quantum.Plugin.Calendar.slnx -c Release
pwsh .ci/Build.ps1
```

The build helper creates a validated archive in artifacts. Extract it for the Quantum plugin loader; keep the manifest and wwwroot alongside the assembly. [CI](glossary.md#ci) verifies Windows and Linux.

[Apache License 2.0](../../LICENSE) · [NOTICE](../../NOTICE)

## Development standards

Follow the [English development standards](../../.standards/docs/en/README.md). The .standards submodule records the exact reviewed revision; upstream changes apply only after an explicit update of that recorded commit. Initialize it before development:

```sh
git submodule update --init --recursive
python .standards/.ci/check-adoption.py
```

Keep both language editions aligned. AGENTS.md links to the adopted English entry point; project-specific instructions supplement the shared requirements.

[Calendar capabilities and repeating events](calendar-capabilities.md)

[Month and week views](calendar-views.md)

This release targets the published SDK `Quantum.Plugin.Abstraction` **0.3.0-rc.3**. Publish it with `--sdk-version-support ">=0.3.0-rc.3 <0.4.0"`; this compatibility value identifies the host SDK rather than the Desktop application version. See [release notes](releases.md) for upgrade ordering.

[Scheduling extension contracts](scheduling-extensions.md)
