using System;
using System.IO;
using ObjCRuntime;
using UIKit;

namespace SPIC.MauiBlazorApp
{
    public class Program
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "startup_log.txt");
            try
            {
                File.WriteAllText(logPath, $"[{DateTime.UtcNow:O}] Program.Main started\n");
            }
            catch { }

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] CRITICAL UNHANDLED: {e.ExceptionObject}\n");
                }
                catch { }
            };

            try
            {
                // Ensure SceneDelegate type is registered with the Objective-C runtime before UIKit processes UIApplicationSceneManifest
                _ = typeof(SceneDelegate);
                File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] Registered SceneDelegate, entering UIApplication.Main\n");

                UIApplication.Main(args, null, typeof(AppDelegate));
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] UIApplication.Main EXCEPTION: {ex}\n");
                }
                catch { }
                throw;
            }
        }
    }
}
