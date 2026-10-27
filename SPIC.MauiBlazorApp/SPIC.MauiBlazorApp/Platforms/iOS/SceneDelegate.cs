using System;
using System.IO;
using Foundation;
using Microsoft.Maui;
using UIKit;

namespace SPIC.MauiBlazorApp;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "startup_log.txt");
        try
        {
            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] SceneDelegate.WillConnect called\n");
            base.WillConnect(scene, session, connectionOptions);
            File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] SceneDelegate.WillConnect finished\n");
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] SceneDelegate.WillConnect EXCEPTION: {ex}\n");
            }
            catch { }
            throw;
        }
    }
}
