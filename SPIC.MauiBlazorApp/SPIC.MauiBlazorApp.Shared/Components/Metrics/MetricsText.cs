using System.Globalization;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Components.Metrics;

/// <summary>Formatting shared by the Metrics page components. Times arrive in UTC and show in IST.</summary>
public static class MetricsText
{
    private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");
    private static readonly TimeSpan Ist = TimeSpan.FromMinutes(330);

    /// <summary>The apps offered by the app filter (Mac and Server rows still show in the tiles).</summary>
    public static readonly TelemetryApp[] FilterApps =
        { TelemetryApp.Web, TelemetryApp.Android, TelemetryApp.iOS, TelemetryApp.Windows };

    public static string Num(int? n) => n is { } v ? v.ToString("N0", India) : "–";
    public static string Num(long? n) => n is { } v ? v.ToString("N0", India) : "–";
    public static string Ms(int? n) => n is { } v ? $"{v.ToString("N0", India)} ms" : "–";

    public static DateTime ToIst(DateTime utc) =>
        (utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc)).Add(Ist);

    public static string Stamp(DateTime utc) => ToIst(utc).ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
    public static string Stamp(DateTime? utc) => utc is { } v ? Stamp(v) : "–";
    public static string Day(DateTime day) => day.ToString("dd MMM", CultureInfo.InvariantCulture);

    /// <summary>"just now", "5 min ago", "3 h ago", "2 days ago", then the date.</summary>
    public static string Ago(DateTime utc)
    {
        var at = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var span = DateTime.UtcNow - at;
        if (span < TimeSpan.FromMinutes(1)) return "just now";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes} min ago";
        if (span < TimeSpan.FromDays(1)) return $"{(int)span.TotalHours} h ago";
        if (span < TimeSpan.FromDays(7)) return (int)span.TotalDays == 1 ? "1 day ago" : $"{(int)span.TotalDays} days ago";
        return ToIst(at).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }

    public static string AppName(TelemetryApp app) => app switch
    {
        TelemetryApp.Web => "Web",
        TelemetryApp.Android => "Android",
        TelemetryApp.iOS => "iOS",
        TelemetryApp.Windows => "Windows",
        TelemetryApp.MacCatalyst => "Mac",
        TelemetryApp.Server => "Server",
        _ => "Unknown"
    };

    public static string AppIcon(TelemetryApp app) => app switch
    {
        TelemetryApp.Web => "bi-globe2",
        TelemetryApp.Android => "bi-android2",
        TelemetryApp.iOS or TelemetryApp.MacCatalyst => "bi-apple",
        TelemetryApp.Windows => "bi-windows",
        TelemetryApp.Server => "bi-hdd-rack",
        _ => "bi-question-circle"
    };

    public static string AppTone(TelemetryApp app) => app switch
    {
        TelemetryApp.Web => "blue",
        TelemetryApp.Android => "green",
        TelemetryApp.iOS or TelemetryApp.MacCatalyst => "violet",
        TelemetryApp.Windows => "teal",
        _ => "grey"
    };

    public static string SourceName(TelemetrySource source) => source switch
    {
        TelemetrySource.Api => "API",
        TelemetrySource.WebHost => "Web host",
        TelemetrySource.Client => "App",
        TelemetrySource.Js => "JavaScript",
        _ => source.ToString()
    };

    public static string SourceTone(TelemetrySource source) => source switch
    {
        TelemetrySource.Api => "orange",
        TelemetrySource.WebHost => "pink",
        TelemetrySource.Client => "violet",
        TelemetrySource.Js => "yellow",
        _ => "grey"
    };

    /// <summary>"System.NullReferenceException" -> "NullReferenceException".</summary>
    public static string ShortType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Error";
        var dot = type.LastIndexOf('.');
        return dot >= 0 && dot < type.Length - 1 ? type[(dot + 1)..] : type;
    }

    /// <summary>"GET api/Lab/batches/{id}" -> ("GET", "api/Lab/batches/{id}").</summary>
    public static (string Method, string Path) SplitEndpoint(string route)
    {
        var space = route.IndexOf(' ');
        return space > 0 ? (route[..space], route[(space + 1)..]) : ("", route);
    }

    public static string Dash(string? s) => string.IsNullOrWhiteSpace(s) ? "–" : s;
}
