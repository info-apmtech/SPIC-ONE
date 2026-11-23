window.spicSelect2 = {
    initMultiple: function (configs, dotnetHelper) {
        configs.forEach(c => {
            var $el = $('#' + c.id);
            if ($el.length && !$el.hasClass("select2-hidden-accessible")) {
                var options = {
                    placeholder: "Select an option",
                    allowClear: true,
                    width: '100%'
                };
                // Optional: attach the dropdown inside the nearest matching ancestor
                // (e.g. a BottomSheet, which sits above Select2's default z-index).
                if (c.parent) {
                    var $parent = $el.closest(c.parent);
                    if ($parent.length) options.dropdownParent = $parent;
                }
                $el.select2(options);

                $el.on('change', function (e) {
                    var value = $(this).val();
                    if (!value) value = [];
                    else if (!Array.isArray(value)) value = [value];
                    dotnetHelper.invokeMethodAsync(c.method, value);
                });
            }
        });
    },

    // initMultiple only ever calls $el.select2(options) ONCE per element (guarded by the
    // select2-hidden-accessible class) - correct for a fresh page load, but a Blazor component that
    // gets REUSED across navigations (e.g. PublicityForm.razor editing one request, then another)
    // re-renders the underlying <select> with a new `selected` option without Select2 ever noticing,
    // since Select2 owns its own separate rendered display once initialized. This explicitly re-syncs
    // an already-initialized widget's displayed value to match the current DOM state, without
    // destroying/recreating it (so its own 'change' binding from initMultiple stays intact).
    setValue: function (id, value) {
        var $el = $('#' + id);
        if ($el.length && $el.hasClass("select2-hidden-accessible")) {
            $el.val(value == null ? null : String(value)).trigger('change.select2');
        }
    },

    destroyMultiple: function (ids) {
        ids.forEach(id => {
            var $el = $('#' + id);
            if ($el.length && $el.hasClass("select2-hidden-accessible")) {
                $el.select2('destroy');
            }
        });
    }
};
