# Quantum.Plugin.Calendar

[English](../en/README.md)

提供日历条目、任务和提醒，包含本地持久化及本地化 Blazor 页面。

[架构](architecture.md) · [开发](contributing.md) · [术语表](glossary.md)

需要 .NET 10，依赖从公开 NuGet 源还原。已发布的 [SDK](glossary.md#sdk) 版本固定在 Directory.Packages.props，无需相邻项目源码。

```sh
dotnet restore Quantum.Plugin.Calendar.slnx
dotnet test Quantum.Plugin.Calendar.slnx -c Release
pwsh .ci/Build.ps1
```

构建工具在 artifacts 中生成经过验证的压缩包，解压后交给 Quantum 插件加载器；清单和 wwwroot 须与程序集一起保留。[CI](glossary.md#ci) 验证 Windows 和 Linux。

[Apache License 2.0](../../LICENSE) · [NOTICE](../../NOTICE)

## 开发规范

遵循[简体中文开发规范](../../.standards/docs/zh-CN/README.md)。.standards 子模块记录经过审查的确切版本；上游变更只有在显式更新该提交后才生效。开发前初始化：

```sh
git submodule update --init --recursive
python .standards/.ci/check-adoption.py
```

同步维护两种语言。AGENTS.md 指向采用的英文入口，项目专属指令补充共享规范。

[日历能力与重复日程](calendar-capabilities.md)

[月视图与周视图](calendar-views.md)

本版本面向已发布的 SDK `Quantum.Plugin.Abstraction` **0.3.0-rc.3**。Quantum 市场从安装包中的程序集识别这个精确版本。使用 Quantum CLI 0.3.0 或更高版本提交已验证的 ZIP，并由市场自动识别 SDK。升级顺序见[版本说明](releases.md)。

[日程扩展契约](scheduling-extensions.md)
