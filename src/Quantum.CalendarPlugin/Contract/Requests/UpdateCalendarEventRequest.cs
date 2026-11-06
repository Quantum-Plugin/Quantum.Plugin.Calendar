using System.ComponentModel;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed record UpdateCalendarEventRequest
{
    [Description("Event ID. For recurring events, updates the entire series.")]
    public required Guid Id { get; set; }

    [Description("The complete replacement contents.")]
    public required SaveCalendarEventRequest Entry { get; set; }
}
