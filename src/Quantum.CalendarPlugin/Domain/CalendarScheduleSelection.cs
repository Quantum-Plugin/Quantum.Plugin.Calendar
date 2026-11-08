using NOF.Domain;

namespace Quantum.OfficialPlugins.Calendar.Domain;

public static class CalendarScheduleSelection
{
    public static void ValidateId(string? id)
    {
        if (id is null)
        {
            return;
        }

        var parts = id.Split('/');
        if (id.Length > 257 || parts.Length != 2 || parts.Any(part => part.Length is < 1 or > 128 || part.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or '_'))))
        {
            throw new DomainException("calendar_invalid_request", "The scheduling contribution ID is invalid.");
        }
    }
}
