using Quantum.OfficialPlugins.Calendar.Domain;
using System.ComponentModel;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed record ListCalendarEntriesRequest
{
    [Description("Inclusive start date.")]
    public required DateOnly StartDate { get; set; }

    [Description("Inclusive end date.")]
    public required DateOnly EndDate { get; set; }

    [Description("Optional item kind filter: Event or Task.")]
    public CalendarEntryKind? Kind { get; set; }

    [Description("Optional zero-based stored-series page (0-100000), 100 entries per page ordered by anchor date then ID. Null expands occurrences. In series mode date bounds apply to stored anchors and the 3660-day expansion limit does not apply; no contributed rules are evaluated.")]
    public int? SeriesPage { get; set; }
}
