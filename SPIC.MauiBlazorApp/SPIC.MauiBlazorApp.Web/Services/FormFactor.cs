using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Web.Services
{
    public class FormFactor : IFormFactor
    {
        public string GetFormFactor()
        {
            return "Web";
        }

        public string GetPlatform()
        {
            return Environment.OSVersion.ToString();
        }
    }
}
