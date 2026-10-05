using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    public class ExportColumn<T>
    {
        public string Header { get; set; } = "";
        public Func<T, string> ValueSelector { get; set; } = _ => "";

        /// <summary>
        /// When true the column is still exported (CSV/Excel/PDF/Print) but is left out of the
        /// auto-generated phone card in <c>SpicDataTable</c>. Defaults to false, so existing
        /// callers are unaffected. Columns whose Header is "S:No" are always skipped in cards
        /// because the card shows its own row number.
        /// </summary>
        public bool HideInCard { get; set; }
    }
}
