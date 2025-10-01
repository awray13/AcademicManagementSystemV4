$(function () {
    // Form validation enhancement
    $('#profileForm').on('submit', function (e) {
        var isValid = true;

        // Validate required fields
        $('input[required]').each(function () {
            if (!$(this).val().trim()) {
                isValid = false;
                $(this).addClass('is-invalid');
            } else {
                $(this).removeClass('is-invalid');
            }
        });

        if (!isValid) {
            e.preventDefault();
            return false;
        }
    });

    // Real-time validation feedback
    $('input').on('blur', function () {
        if ($(this).attr('required') && !$(this).val().trim()) {
            $(this).addClass('is-invalid');
        } else {
            $(this).removeClass('is-invalid');
        }
    });

    // Phone number formatting
    $('#PhoneNumber').on('input', function () {
        var value = $(this).val().replace(/\D/g, '');
        if (value.length >= 10) {
            var formatted = value.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
            $(this).val(formatted);
        }
    });
});