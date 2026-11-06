using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

internal static class CalendarWeekLayout
{
    public static IReadOnlyList<WeekEventLayout> Arrange(IEnumerable<CalendarEntryDetails> entries)
    {
        var events = entries.Where(entry => entry.Kind == CalendarEntryKind.Event)
            .Select(entry => new
            {
                Entry = entry,
                Start = Math.Min(entry.StartTime.ToTimeSpan().TotalMinutes, 1410),
                End = Math.Min(1440, Math.Max(entry.EndTime?.ToTimeSpan().TotalMinutes ?? entry.StartTime.ToTimeSpan().TotalMinutes + 30, Math.Min(entry.StartTime.ToTimeSpan().TotalMinutes, 1410) + 30))
            })
            .OrderBy(entry => entry.Start).ThenBy(entry => entry.End).ThenBy(entry => entry.Entry.Id).ThenBy(entry => entry.Entry.OriginalDate);
        var result = new List<WeekEventLayout>();
        var group = new List<WeekEventLayout>();
        var columnEnds = new List<double>();
        var groupEnd = 0d;
        foreach (var item in events)
        {
            if (group.Count > 0 && item.Start >= groupEnd)
            {
                result.AddRange(group.Select(layout => layout with { ColumnCount = columnEnds.Count }));
                group.Clear();
                columnEnds.Clear();
            }

            var column = columnEnds.FindIndex(end => end <= item.Start);
            if (column < 0)
            {
                column = columnEnds.Count;
                columnEnds.Add(item.End);
            }
            else
            {
                columnEnds[column] = item.End;
            }

            group.Add(new WeekEventLayout { Entry = item.Entry, StartMinute = item.Start, EndMinute = item.End, Column = column, ColumnCount = 1 });
            groupEnd = Math.Max(groupEnd, item.End);
        }

        result.AddRange(group.Select(layout => layout with { ColumnCount = columnEnds.Count }));
        return result;
    }
}
