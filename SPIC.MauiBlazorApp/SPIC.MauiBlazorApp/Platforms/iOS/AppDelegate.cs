using Foundation;

namespace SPIC.MauiBlazorApp
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp()
        {
            // Ensure SceneDelegate type is initialized for Objective-C runtime scene resolution
            _ = typeof(SceneDelegate);
            return MauiProgram.CreateMauiApp();
        }
    }
}
