// Client-side counterpart of NotInPastAttribute: a date may not be before today
// unless the related status field holds one of the exempt values.
(function ($) {
    $.validator.addMethod("notinpast", function (value, element, params) {
        if (!value) return true;
        var status = $(element.form).find("[name='" + params.statusfield + "']").val();
        if (params.exempt && params.exempt.split(",").indexOf(status) >= 0) return true;
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        return new Date(value + "T00:00:00") >= today;
    });

    $.validator.unobtrusive.adapters.add("notinpast", ["statusfield", "exempt"], function (options) {
        options.rules.notinpast = options.params;
        options.messages.notinpast = options.message;
    });
})(jQuery);
