namespace SPIC.MauiBlazorApp.Shared.Services
{
    public class AppSearchState
    {
        private string _query = "";

        public string Query
        {
            get => _query;
            set
            {
                var trimmed = value ?? "";
                if (_query != trimmed)
                {
                    _query = trimmed;
                    OnChange?.Invoke();
                }
            }
        }

        public event Action? OnChange;

        public bool HasQuery => !string.IsNullOrWhiteSpace(_query);

        /// <summary>
        /// Returns true if the given label matches the current search query
        /// (case-insensitive contains). Empty query = no match.
        /// </summary>
        public bool Matches(string? label)
        {
            if (!HasQuery || string.IsNullOrWhiteSpace(label)) return false;
            return label.Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public void Clear() => Query = "";
    }
}