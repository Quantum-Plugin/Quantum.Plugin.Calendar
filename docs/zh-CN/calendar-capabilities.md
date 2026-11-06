# 日历能力与重复日程

[English](../en/calendar-capabilities.md) · [架构](architecture.md)

版本 4 移除原有 `calendar.create`、`calendar.update`、`calendar.delete`、`calendar.get`、`calendar.list`、`calendar.list-tasks`、`calendar.set-completed`、`calendar.mobile-refresh` 及 `calendar.created/updated/deleted` 能力名，不保留兼容别名。升级时须同步安装消费方更新；插件标识、已存条目 ID、路由和已有迁移文件保持稳定。

## 资源命名

| 资源 | 请求能力 |
| --- | --- |
| 日程 | `calendar.event.create`、`.get`、`.list`、`.update`、`.delete` |
| 待办 | `calendar.task.create`、`.get`、`.list`、`.update`、`.delete`、`.set-completed` |
| 日程与待办的统一读取 | `calendar.entry.get`、`calendar.entry.list` |
| 移动页面 | `calendar.mobile-page.refresh` |

日程和待办分别发布 `calendar.event.created/updated/deleted` 和 `calendar.task.created/updated/deleted` 通知，内容为 `id`。通知在数据库提交成功后发布。读取采用 All 分发；写入采用 Ordered 和唯一归属策略；创建与移动页面刷新要求唯一提供方。其他资源类型的 ID 视为不存在，不会转换类型或修改内容。只有待办可以标记完成。

[RPC](glossary.md#rpc) 契约以 [ICalendarRpcService](../../src/Quantum.CalendarPlugin/Contract/ICalendarRpcService.cs) 为准。日程与待办使用独立的保存结构，不再传入 `kind`；待办也不包含结束时间和重复字段。`calendar.task.list` 接受 `includeCompleted`（默认 true）；日程与统一列表接受包含边界的 `startDate` 和 `endDate`。`kind` 仅用于统一列表。

## 重复日程

重复字段均可选，省略时创建单次日程。日期格式为 `yyyy-MM-dd`，时间为 `HH:mm:ss`，ID 为 UUID 字符串。沿用插件的本地日期与时刻模型，重复计算不会进行跨时区换算。

| `repeatPattern` | 行为 |
| --- | --- |
| `None` | 不重复，默认值 |
| `Daily` | 每隔 `repeatInterval` 天 |
| `Weekdays` | 每个周一至周五；间隔必须为 1；不包含法定节假日和调休规则 |
| `Weekly` | 以周一为一周起点，每隔 N 周在选中的星期重复；间隔 2 即每两周 |
| `Monthly` | 每隔 N 个月，与起始日期为同一日号 |
| `MonthlyWeekday` | 每隔 N 个月，与起始日期为相同的星期几与次序；`repeatWeekOfMonth` 可为 1–5 或 -1（最后一个），省略时使用起始日期的次序 |
| `MonthlyLastDay` | 每隔 N 个月的最后一天 |
| `Yearly` | 每隔 N 年，与起始日期为相同月日 |

`repeatInterval` 默认为 1，可取 1–999。按月间隔 3 为每季度，间隔 6 为每半年。每周的 `repeatWeekdays` 为标志值：周日=1、周一=2、周二=4、周三=8、周四=16、周五=32、周六=64，可组合数值（周一三五=42），也可使用 `"Monday, Wednesday, Friday"` 等名称字符串。省略或 None 时使用起始日期的星期。系列起始日期必须符合规则。不存在的日期（如 2 月 31 日、平年的 2 月 29 日）会跳过且不占次数，不会漂移到较短月份的月底。

结束条件可选包含当天的 `repeatUntil` 或 `repeatCount`（1–10000，包含起始日），两者不能同时设置；都省略表示永不结束。不适用于当前模式的字段会被拒绝。

`calendar.event.create` 示例：

```json
{
  "title": "团队例会",
  "notes": "计划讨论",
  "date": "2026-11-06",
  "startTime": "09:00:00",
  "endTime": "09:30:00",
  "style": "blue",
  "repeatPattern": "Weekly",
  "repeatInterval": 2,
  "repeatWeekdays": "Monday, Friday",
  "repeatCount": 10
}
```

列表查询会展开窗口内的实例，包括早于查询窗口开始的系列。每项保留系列 `id`，`date` 为本次发生日期，`seriesStartDate` 为系列起始日期。读取单项、创建和更新的结果中，`date` 为系列起始日期。从任意实例编辑时，编辑器加载系列起始日期与完整规则。编辑和删除作用于整个系列，暂不支持单次例外。记账关联继续使用系列 ID。

列表查询最多包含 3660 天，结果最多 10000 项；超限会失败而不会截断，请缩小日期窗口。无效规则或范围返回 `calendar_invalid_request`。不存在或类型错误的 ID 在本地返回 `calendar_entry_not_found`，宿主分发时将归属判断传递给下一个提供方。取消遵循宿主请求管道；身份认证、权限、限流和超时由宿主执行。

## 存储升级

宿主在迁移 001、002 后执行 `003_event_recurrence.sql`，仅新增重复列，已有条目默认为不重复，保留 ID 与内容。此[迁移](glossary.md#migration)为增量变更，不含数据回填或破坏性清理。升级前保留宿主常规数据库备份。回退应用可恢复旧插件；旧插件仅查询原有列，但不能展开新创建的重复系列。保留新增列，后续数据迁移另行评审。
