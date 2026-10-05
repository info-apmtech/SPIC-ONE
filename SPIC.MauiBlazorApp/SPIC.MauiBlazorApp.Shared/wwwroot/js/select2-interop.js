window.spicSelect2 = {
    initMultiple: function (configs, dotnetHelper) {
        configs.forEach(c => {
            var $el = $('#' + c.id);
            if (!$el.hasClass("select2-hidden-accessible")) {
                $el.select2({
                    placeholder: "Select an option",
                    allowClear: true,
                    width: '100%'
                });

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
