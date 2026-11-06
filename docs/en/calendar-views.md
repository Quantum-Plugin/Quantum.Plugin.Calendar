# Calendar views

[简体中文](../zh-CN/calendar-views.md) · [Calendar capabilities](calendar-capabilities.md)

The calendar opens in Month view. Use the Month/Week buttons beside the date heading to switch views while retaining the selected date. Previous/next moves by a month or a week, depending on the current view. Today selects the current local date and loads its month or week.

Week view runs from Monday through Sunday, including days in neighboring months or years. The heading shows both boundary dates. The page queries the existing `calendar.entry.list` capability for that exact inclusive range, so repeating events are expanded even when their series started before the visible week. Summary counts cover the displayed period and count event occurrences rather than series.

Each day has a header and a task strip above the 24-hour timeline. Tasks show their due times; their completion, editing and deletion remain available in the existing task list and selected-day panel. Selecting a day updates that panel. Click a timeline hour to create an event at that date and time; click an event or task card to edit it. Repeating events still open their series anchor and rule, and saving them keeps the displayed week.

Overlapping events appear in separate columns. Connected overlap groups share a consistent column count, and later independent events regain the full day width. Events without an end time have a 30-minute visual footprint. Short events also receive a minimum 30-minute footprint to keep their controls visible, with late events fitted inside the last half-hour. Displayed times and saved values retain their original precision.

The timeline initially scrolls to 08:00 or the earliest earlier event. It retains manual vertical scrolling while the same week remains open. At narrow widths, the timeline has its own horizontal and vertical scroll areas, keeps the time ruler and day/task headers visible, and reveals the selected day when opening the view or selecting another day. The day panel and task list stack beneath the calendar. Month view retains its existing calendar grid and month animation.

View behavior and layout are owned by `Calendar.Views.cs`, `CalendarWeekLayout.cs` and `WeekEventLayout.cs` under [UI](glossary.md#ui)/Pages. The plugin adds no view-specific capability or database migration. Tests cover boundary ranges, navigation, repeated-event editing, overlap placement, rendered controls and scroll behavior. Run the checks in [Development](contributing.md); temporary visual previews and acceptance screenshots belong outside the source repository.

[Scheduling extension contracts](scheduling-extensions.md)
