using System.ComponentModel;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed record ListCalendarEventsRequest
{
    [Description("Inclusive start date.")]
    public required DateOnly StartDate { get; set; }

    [Description("Inclusive end date. The range may contain at most 3660 days.")]
    public required DateOnly EndDate { get; set; }
}
