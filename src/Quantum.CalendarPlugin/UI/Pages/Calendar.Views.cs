using Microsoft.AspNetCore.Components;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using System.Globalization;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

public partial class Calendar
{
    private bool _isWeekView;
    private ElementReference _weekViewport;
    private DateOnly WeekStartDate => _selectedDate.AddDays(-((int)_selectedDate.DayOfWeek + 6) % 7);
    private DateOnly WeekEndDate => AddDaysClamped(WeekStartDate, 6);
    private DateOnly VisibleStartDate => _isWeekView ? WeekStartDate : _visibleMonth;
    private DateOnly VisibleEndDate => _isWeekView ? WeekEndDate : new(_visibleMonth.Year, _visibleMonth.Month, DateTime.DaysInMonth(_visibleMonth.Year, _visibleMonth.Month));
    private string PeriodTitle => _isWeekView ? $"{VisibleStartDate.ToString("d", CultureInfo.CurrentCulture)} – {VisibleEndDate.ToString("d", CultureInfo.CurrentCulture)}" : MonthTitle;
    private IReadOnlyList<CalendarDay> WeekDays => [.. Enumerable.Range(0, WeekEndDate.DayNumber - WeekStartDate.DayNumber + 1).Select(offset =>
    {
        var date = WeekStartDate.AddDays(offset);
        return new CalendarDay { Date = date, IsVisibleMonth = true, IsToday = date == _today, Items = [.. _items.Where(item => item.Date == date)] };
    })];
    private double WeekScrollMinute => Math.Min(480, _items.Where(item => item.Kind == CalendarEntryKind.Event).Select(item => Math.Floor(item.StartTime.ToTimeSpan().TotalHours) * 60).DefaultIfEmpty(480).Min());
    private bool CanGoBack => VisibleStartDate > DateOnly.MinValue;
    private bool CanGoForward => VisibleEndDate < DateOnly.MaxValue;

    private async Task SetWeekViewAsync(bool isWeekView)
    {
        if (_isBusy || _isWeekView == isWeekView)
        {
            return;
        }

        _isWeekView = isWeekView;
        SetVisibleMonth(new(_selectedDate.Year, _selectedDate.Month, 1));
        _previousMonthDays = null;
        CancelDelete();
        CancelEdit();
        await LoadVisiblePeriodAsync();
    }

    private async Task ChangePeriodAsync(int direction)
    {
        if (_isBusy || (direction < 0 ? !CanGoBack : !CanGoForward))
        {
            return;
        }

        if (_isWeekView)
        {
            _selectedDate = AddDaysClamped(_selectedDate, direction * 7);
            SetVisibleMonth(new(_selectedDate.Year, _selectedDate.Month, 1));
            CancelEdit();
            CancelDelete();
            await LoadVisiblePeriodAsync();
        }
        else
        {
            await ChangeMonthAsync(direction);
        }
    }

    private void BeginCreateWeekEvent(DateOnly date, int hour)
    {
        _selectedDate = date;
        CancelDelete();
        BeginCreate();
        _draft.StartTime = new(hour, 0);
        _draft.EndTime = hour < 23 ? new(hour + 1, 0) : new(23, 59);
    }

    private void BeginEditWeekEntry(CalendarEntryDetails item)
    {
        _selectedDate = item.Date;
        CancelDelete();
        BeginEdit(item);
    }

    private string WeekEntryLabel(CalendarEntryDetails item)
    {
        var label = L["Edit {0}, {1}, {2}", item.Title, item.Date.ToString("D", CultureInfo.CurrentCulture), FormatTime(item)].Value;
        return item.OriginalDate is { } original && original != item.Date ? $"{label}. {L["Adjusted from {0}", original.ToString("d", CultureInfo.CurrentCulture)]}" : label;
    }
    private static DateOnly AddDaysClamped(DateOnly date, int days) => DateOnly.FromDayNumber(Math.Clamp(date.DayNumber + days, DateOnly.MinValue.DayNumber, DateOnly.MaxValue.DayNumber));
}
