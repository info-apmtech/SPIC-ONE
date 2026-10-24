using System;
using System.IO;
using Foundation;

namespace SPIC.MauiBlazorApp
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp()
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "startup_log.txt");
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] AppDelegate.CreateMauiApp called\n");
                // Ensure SceneDelegate type is initialized for Objective-C runtime scene resolution
                _ = typeof(SceneDelegate);
                var app = MauiProgram.CreateMauiApp();
                File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] AppDelegate.CreateMauiApp completed successfully\n");
                return app;
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] AppDelegate.CreateMauiApp EXCEPTION: {ex}\n");
                }
                catch { }
                throw;
            }
        }
    }
}
