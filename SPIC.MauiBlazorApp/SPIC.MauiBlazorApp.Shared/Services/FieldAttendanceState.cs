namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Scoped on-duty flag shared between FieldDashboard and MarkAttendance. Each page navigation
/// creates a fresh component instance, so a plain private field can't survive the round trip
/// from "fill in the attendance form" back to "show the Tracking Active card" - this does.
/// A cache only - the database (JMDOAttendance.DutyStatus via api/JMDOAttendance/today) is the
/// source of truth. Pages overwrite this only after loading today's attendance, saving it, or a
/// successful Stop Tracking; nothing here is persisted beyond the current circuit.
/// </summary>
public class FieldAttendanceState
{
    public bool IsOnDuty { get; set; }
}
