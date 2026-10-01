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

    destroyMultiple: function (ids) {
        ids.forEach(id => {
            var $el = $('#' + id);
            if ($el.length && $el.hasClass("select2-hidden-accessible")) {
                $el.select2('destroy');
            }
        });
    }
};
