# 日程扩展

[English](../en/scheduling-extensions.md) · [能力接口](calendar-capabilities.md) · [架构](architecture.md)

日历负责日期、日程标识、校验和实例展开，不包含任何法定、地域、学校、公司或特定机构的调度政策。可选插件通过已发布 [SDK](glossary.md#sdk) 的 `IQuantumExtensions` 接口以 [JSON](glossary.md#json) 数据向[扩展点](glossary.md#extension-point)贡献可选规则，不引用兄弟仓库源码、不共享插件程序集，不增加清单贡献字段，也不执行网络查询或外部业务代码回调。

## 第 1 版契约

| 扩展点 | 数据结构 | 含义 |
| --- | --- | --- |
| `quantum.plugin.calendar/recurrence-rules` | [重复规则结构](../contracts/recurrence-rules.v1.schema.json) | 通用模式预设或有限的原定日期集合，例如指定教学周的周一。 |
| `quantum.plugin.calendar/date-adjustments` | [调休结构](../contracts/date-adjustments.v1.schema.json) | 在明确覆盖范围内，将原定日期映射到实际日期；null 表示取消该次实例。 |

扩展点完整标识带有所属插件命名空间：`quantum.plugin.calendar/recurrence-rules` 和 `quantum.plugin.calendar/date-adjustments`。`Define("recurrence-rules", 1)` 只接收局部名，是因为 SDK 会自动加上宿主认定的当前插件标识；`Register` 和 `Observe` 则始终使用完整扩展点标识。其他插件即使定义相同局部名，也属于不同扩展点。不要保存或解析裸局部名，也不要在调用 `Define` 时重复添加命名空间。

宿主提供贡献方的 `PluginId` 和 `LocalId`，日程保存二者组合成的 `plugin-id/local-id`，精确区分大小写；数据内容不能冒充其他贡献方。例如 `example.school/term` 与 `example.company/term` 可以同时存在，标题和局部名相同也不会导致选择或撤回一个时影响另一个；数据中的 `id`、`pluginId` 等字段不能替代宿主标注的身份。更新学期或来源数据时应保留稳定标识。标题不能为空，最多 120 个字符，界面按文本显示标题和来源标识。日期格式为 `yyyy-MM-dd`，不包含时区。未知字段会被忽略以支持兼容性扩展。契约版本必须为 1，其他版本不参与计算。

重复规则必须且只能采用以下一种形态：`pattern` 形态声明 `Daily`、`Weekdays`、`Weekly`、`Monthly`、`MonthlyWeekday`、`MonthlyLastDay` 或 `Yearly`，并可提供默认 `interval`（1–999，缺省为 1；Weekdays 必须为 1），不能同时包含 `dates`；有限日期形态包含 1–10,000 个不重复的 `dates`，不能同时包含 `pattern` 或 `interval`，输入顺序不限，由日历排序。有限日期形态表示一个完整的有限模式。贡献方将教学周、学期边界、单双周等规则计算成不同的可选日期集合，日历不解释这些概念。SDK 的 128 KiB 数据上限同时有效，可能先于数量上限达到。空日期集无效；规则消失时应撤回贡献。有限日期模式即使在编辑器中选择“永不”结束，也不会生成超出所发布集合的日期。通用模式预设使用日程保存的间隔、星期、月内序号及结束条件；贡献中的间隔用于编辑器初始值，不覆盖既有日程已经保存的参数。

日历通过相同的 `IQuantumExtensions.Register` 方法和契约版本注册自己的内置预设，其他观察者可以看到贡献方为 `quantum.plugin.calendar` 的这些贡献。稳定的局部标识为 `builtin-daily`、`builtin-weekdays`、`builtin-weekly`、`builtin-biweekly`、`builtin-monthly`、`builtin-monthly-weekday`、`builtin-monthly-last-day` 和 `builtin-yearly`。双周采用 `pattern=Weekly`、`interval=2`，其他默认间隔为 1。编辑器从同一个目录发现所有规则，遵循宿主标注的贡献顺序，并对日历自身的标题使用本地化资源。“不重复”表示未选择规则，不单独注册重复提供方。

内置或外部通用模式贡献使用相同的数据形态，例如：

```json
{"title":"每两周","pattern":"Weekly","interval":2}
```

调休数据要求 `validFrom <= validUntil`，最多 10,000 条 `changes`。原日期及非 null 的目标日期必须处于该闭区间内，同一原日期只能出现一次。覆盖范围内未列出的日期保持不变；范围外属于未知状态，明确报错。null 取消该次实例。移动一个日期不会自动交换另一个日期；交换需要提供两条映射。所有映射只应用一次，目标日期不会继续经过其他映射。

贡献插件持有以下注册句柄直至停止：

```csharp
using var repeat = JsonDocument.Parse("""
    {"title":"示例教学周周五", "dates":["2026-11-06","2026-11-20","2026-12-04"]}
    """);
var term = extensions.Register("quantum.plugin.calendar/recurrence-rules", 1,
    "teaching-fridays", repeat.RootElement);
using var adjustment = JsonDocument.Parse("""
    {"title":"示例公司调休", "validFrom":"2026-01-01", "validUntil":"2026-12-31",
     "changes":[{"date":"2026-11-06","targetDate":"2026-12-01"},
                {"date":"2026-11-07","targetDate":null}]}
    """);
var shifts = extensions.Register("quantum.plugin.calendar/date-adjustments", 1,
    "working-days", adjustment.RootElement);
// 停止时：term.Dispose(); shifts.Dispose();
```

通过 `IQuantumContribution.Update` 替换完整数据，释放句柄以撤回。注册和更新遵循 SDK 的运行时目录生命周期。每次操作使用不可变快照，更新仅影响后续读取。桌面端监听变化，在界面线程更新选项和日程；移动端沿用现有刷新循环。停止时释放定义和监听，重启后重新监听。无效贡献单独记录并忽略，不影响同批有效规则。扩展数据仅存在于宿主会话，贡献方负责持久化政策数据、来源追溯和刷新；不得贡献秘密信息。

## 每条日程的选择与[实例](glossary.md#occurrence)计算

现有和新建日程默认 `RepeatRuleId = null`、`AdjustmentRuleId = null`，不会自动选用任何来源。事件通过 `RepeatRuleId` 统一选择内置和外部贡献。选择通用模式时，`RepeatPattern` 必须与贡献声明的模式一致，首次选择时复制贡献的默认间隔；用户可自定义保存的间隔及适用的星期、月内序号。保存的模式必须持续与贡献匹配，来源不能在既有日程下静默替换计算模式。选择有限日期规则时，`RepeatPattern=None`，间隔为 1，不设置星期标志或月内序号，开始日期必须出现在贡献集合中。两种形态均支持按截止日期或次数结束。没有 `RepeatRuleId` 的旧日程会在计算时适配到对应内置描述，不改写存储字段、不要求数据迁移；编辑旧日程时，直到用户显式选择规则前都保留原有表示。

事件和任务均可独立选择一个 `AdjustmentRuleId`，也可以选择“保持原日期（不调休）”。不存在全局调休开关、隐式来源、来源优先级或自动叠加。需要组合政策时，可由学校或其他插件发布组合结果，日历不决定哪个组织的政策优先。任务仍不可重复。任务被映射为 null 后会从展开的任务列表中省略，直至政策变化或通过直接获取/更新接口修复其选择。

计算顺序为：从开始日期生成原定日期；按截止日期或次数限制；应用所选调休一次；按实际日期筛选查询窗口；排序。取消的实例仍消耗原来的重复次数。调休后的实际日期可以晚于 RepeatUntil。查询包含原日期在窗口外、实际日期在窗口内的映射。多个原定实例调到同一天时仍分别保留，用 `(Id, OriginalDate)` 区分；不会合并实例，也不会改变日程标识、一天内的时间或完成状态。

列表返回实际 `Date`、原定 `OriginalDate` 和系列起点 `SeriesStartDate`。直接获取/创建/更新返回存储的起点和完整选择；获取和删除不要求来源可用。编辑任意实例会使用原始起点并保留两个选择，编辑或删除影响整个系列。单次实例覆盖不属于本版契约。

## 故障与兼容性

来源插件停用、贡献无效或覆盖期结束时，不会静默替换已保存的规则。写入不可用选择返回 `calendar_schedule_unavailable`，起点或选项错误返回 `calendar_invalid_request`。受影响系列展开时返回 `calendar_schedule_unavailable` 或 `calendar_adjustment_out_of_range`。整次列表请求失败，以免展示不完整的日程。未选中该系列的其他查询仍可成功。

在日程编辑器中可以清除或替换规则。编辑已加载的日程时，编辑器会保留不可用来源的标识。如果日程没有出现在日历中，可以恢复或更新来源插件，也可以通过事件或待办的直接获取和更新接口修改。要查找存储的原始日程，可在调用 `calendar.entry.list` 时传入 `SeriesPage`。页码从 0 开始（0–100000），每页最多返回 100 条原始日程，包含被调休取消的日程。此查询按原始起点、标识排序，不计算贡献规则，日期范围筛选原始起点并可覆盖整个日期域。传入 null 时正常展开实例，日期范围仍受 3,660 天限制。

扩展回调不发起外部请求。原有请求取消机制、查询闭区间、3,660 天查询范围限制及 10,000 个返回实例限制继续有效。贡献仅用于宿主内协作，不构成新的授权边界；原有能力名称和请求生成机制不变。

日历 5.0.0 要求 SDK `>=0.3.0-rc.3 <0.4.0`，因为 rc.2 不具备扩展 API，必须先升级宿主。迁移 `004_scheduling_extensions.sql` 只增加可空选择字段，不重写原数据或历史迁移。消费端识别重复事件时，需要同时检查 `RepeatRuleId` 和 `RepeatPattern`，并保留通用模式贡献对应的已保存参数；忽略新字段的旧消费端无法完整编辑扩展日程。在创建扩展日程后降级，应恢复升级前的应用数据备份，旧版本无法解释这些选择。参见[发布说明](releases.md)。

验证覆盖独立的数据校验与生命周期、不可变操作快照、来源缺失、领域约束、次数和覆盖边界、取消、跨查询窗口调休、生成客户端、真实 SQLite 升级及持久化、编辑器往返。真实法定、公司或教学日历插件的跨插件集成验证属于消费宿主，不由日历提供具体政策实现。
