namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Scoped on-duty flag shared between FieldDashboard and MarkAttendance. Each page navigation
/// creates a fresh component instance, so a plain private field can't survive the round trip
/// from "fill in the attendance form" back to "show the Tracking Active card" - this does.
/// Static placeholder only: no Attendance/Tracking API exists yet, so nothing here is persisted
/// beyond the current circuit.
/// </summary>
public class FieldAttendanceState
{
    public bool IsOnDuty { get; set; }
}
