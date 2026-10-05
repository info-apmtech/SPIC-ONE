using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    public class ExportColumn<T>
    {
        public string Header { get; set; } = "";
        public Func<T, string> ValueSelector { get; set; } = _ => "";
    }
}
