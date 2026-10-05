using Microsoft.AspNetCore.Components;

namespace SPIC.MauiBlazorApp.Shared.Pages.DealerReview
{
    /// <summary>
    /// Base class for the SavedDealerReview section components.
    ///
    /// SavedDealerReview used to be one ~4,000 line component, so every state toggle
    /// (opening an accordion, typing in the approval popup) diffed the whole tree. The
    /// page is now composed of sections, but Blazor still re-renders a child whenever its
    /// parent re-renders, so splitting alone would not have removed the work.
    ///
    /// This base adds the missing piece: a section renders only when one of its own
    /// parameters actually changed. Reference types are compared by reference, so the page
    /// must pass the SAME object instance (the loaded DTO/list/dictionary, never a freshly
    /// allocated wrapper) on every render; value types and strings are compared by value.
    /// EventCallbacks and delegates are ignored because the page always supplies method
    /// groups, and a changed handler never changes the markup.
    ///
    /// The flag is reset to true after each check so renders the component triggers itself
    /// (an @bind inside the approval popup, for example) are never suppressed.
    /// </summary>
    public abstract class DealerReviewSectionBase : ComponentBase
    {
        private readonly Dictionary<string, object?> _lastParameters = new(StringComparer.Ordinal);
        private bool _hasParameters;
        private bool _shouldRender = true;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            var changed = !_hasParameters;

            foreach (var parameter in parameters)
            {
                var value = parameter.Value;

                // Handlers are supplied as method groups by the page; they never affect markup.
                if (value is EventCallback || value is MulticastDelegate)
                    continue;

                if (!_lastParameters.TryGetValue(parameter.Name, out var previous) || !IsSameValue(previous, value))
                    changed = true;

                _lastParameters[parameter.Name] = value;
            }

            _hasParameters = true;
            _shouldRender = changed;
            return base.SetParametersAsync(parameters);
        }

        protected override bool ShouldRender()
        {
            var render = _shouldRender;

            // Any later render this component asks for itself must go through.
            _shouldRender = true;
            return render;
        }

        private static bool IsSameValue(object? previous, object? current)
        {
            if (previous is null || current is null)
                return ReferenceEquals(previous, current);

            var type = previous.GetType();
            if (type != current.GetType())
                return false;

            // Value types (bool/int/decimal/enum) and strings carry their meaning in the value;
            // everything else is page state whose identity changes when the data is reloaded.
            if (type.IsValueType || previous is string)
                return Equals(previous, current);

            return ReferenceEquals(previous, current);
        }
    }
}
