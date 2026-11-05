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
