# Architecture and contracts

[简体中文](../zh-CN/architecture.md)

Source uses technical responsibilities: Contract for messages, Application for use cases, Domain for invariants and [UI](glossary.md#ui) for pages/localization. Runtime adapters, initialization and database contributors stay at the project root. Tests mirror these responsibilities.

[NOF](glossary.md#nof) generates [RPC](glossary.md#rpc) clients and servers. Pages use local generated clients. Cross-plugin requests use IQuantumRequester and named capabilities. The host owns routing, serialization, database lifetime and installation. Host and cross-plugin integration tests belong to the consuming host.

| Identity | Value |
| --- | --- |
| Plugin | quantum.plugin.calendar |
| Assembly | Quantum.CalendarPlugin |

Preserve existing [CLR](glossary.md#clr) names, named requests, route components and migration ordering. The host executes database migrations; never edit a released migration. Add a new numbered migration for new schema changes. Public contracts are defined by:

- [ICalendarRpcService.cs](../../src/Quantum.CalendarPlugin/Contract/ICalendarRpcService.cs)
- [CalendarEntryChanged.cs](../../src/Quantum.CalendarPlugin/Contract/Models/CalendarEntryChanged.cs)
- [CalendarEntryDetails.cs](../../src/Quantum.CalendarPlugin/Contract/Models/CalendarEntryDetails.cs)
- [CalendarEntryKind.cs](../../src/Quantum.CalendarPlugin/Contract/Models/CalendarEntryKind.cs)
- [CalendarEntryLookup.cs](../../src/Quantum.CalendarPlugin/Contract/Models/CalendarEntryLookup.cs)
- [CalendarEntryRequest.cs](../../src/Quantum.CalendarPlugin/Contract/Requests/CalendarEntryRequest.cs)
- [ListCalendarEntriesRequest.cs](../../src/Quantum.CalendarPlugin/Contract/Requests/ListCalendarEntriesRequest.cs)
- [ListCalendarTasksRequest.cs](../../src/Quantum.CalendarPlugin/Contract/Requests/ListCalendarTasksRequest.cs)
- [RefreshCalendarMobilePageRequest.cs](../../src/Quantum.CalendarPlugin/Contract/Requests/RefreshCalendarMobilePageRequest.cs)
- [SaveCalendarEntryRequest.cs](../../src/Quantum.CalendarPlugin/Contract/Requests/SaveCalendarEntryRequest.cs)
- [SetCalendarEntryCompletedRequest.cs](../../src/Quantum.CalendarPlugin/Contract/Requests/SetCalendarEntryCompletedRequest.cs)
- [UpdateCalendarEntryRequest.cs](../../src/Quantum.CalendarPlugin/Contract/Requests/UpdateCalendarEntryRequest.cs)
