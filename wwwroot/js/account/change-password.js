$(function () {
    let passwordStrengthTimeout;

    // Password strength calculation (matches ViewModel logic)
    function calculatePasswordStrength(password) {
        if (!password) return { score: 0, level: 'none', text: 'No password', color: 'secondary' };

        let score = 0;
        let suggestions = [];

        // Length scoring
        if (password.length >= 8) score += 25;
        else suggestions.push('Use at least 8 characters');

        if (password.length >= 12) score += 10;

        // Character variety
        if (/[a-z]/.test(password)) score += 15;
        else suggestions.push('Include lowercase letters');

        if (/[A-Z]/.test(password)) score += 15;
        else suggestions.push('Include uppercase letters');

        if (/\d/.test(password)) score += 15;
        else suggestions.push('Include numbers');

        if (/[^\da-zA-Z]/.test(password)) score += 15;
        else suggestions.push('Include special characters');

        // Complexity bonus/penalties
        if (!/(.)\1{2,}/.test(password)) score += 5;
        else suggestions.push('Avoid repeating characters');

        if (!/123|abc|password/i.test(password)) score += 5;
        else suggestions.push('Avoid common patterns');

        score = Math.min(score, 100);

        // Determine level and display
        let level, color, text;
        if (score >= 85) {
            level = 'strong'; color = 'success'; text = 'Strong 💪';
            suggestions = ['Excellent password strength!'];
        } else if (score >= 65) {
            level = 'good'; color = 'info'; text = 'Good ✓';
        } else if (score >= 40) {
            level = 'fair'; color = 'warning'; text = 'Fair ⚠️';
        } else {
            level = 'weak'; color = 'danger'; text = 'Weak ❌';
        }

        return { score, level, text, color, suggestions };
    }

    // Update password strength indicator
    function updatePasswordStrength() {
        const password = $('#newPasswordField').val();
        const strength = calculatePasswordStrength(password);

        $('#passwordStrengthBar')
            .css('width', strength.score + '%')
            .removeClass('bg-secondary bg-danger bg-warning bg-info bg-success')
            .addClass('bg-' + strength.color);

        $('#passwordStrengthText')
            .text(strength.text)
            .removeClass('text-secondary text-danger text-warning text-info text-success')
            .addClass('text-' + strength.color);

        $('#securityScore')
            .text(strength.score)
            .parent()
            .removeClass('text-secondary text-danger text-warning text-info text-success')
            .addClass('text-' + strength.color);

        // Update suggestions
        const suggestionsHtml = strength.suggestions.map(s =>
            `<small class="text-muted d-block"><i class="fas fa-lightbulb me-1"></i>${s}</small>`
        ).join('');
        $('#passwordSuggestions').html(suggestionsHtml);

        // Update requirements checklist
        updateRequirementsChecklist(password);
    }

    // Update password requirements checklist
    function updateRequirementsChecklist(password) {
        const requirements = [
            { id: 'req-length', test: password.length >= 8 },
            { id: 'req-lowercase', test: /[a-z]/.test(password) },
            { id: 'req-uppercase', test: /[A-Z]/.test(password) },
            { id: 'req-number', test: /\d/.test(password) },
            { id: 'req-special', test: /[^\da-zA-Z]/.test(password) },
            { id: 'req-different', test: password !== $('#OldPassword').val() && password.length > 0 }
        ];

        requirements.forEach(req => {
            const icon = $(`#${req.id}`);
            if (req.test) {
                icon.removeClass('fas fa-times text-danger').addClass('fas fa-check text-success');
            } else {
                icon.removeClass('fas fa-check text-success').addClass('fas fa-times text-danger');
            }
        });
    }

    // Password field events
    $('#newPasswordField').on('input', function () {
        clearTimeout(passwordStrengthTimeout);
        passwordStrengthTimeout = setTimeout(updatePasswordStrength, 300);
        checkPasswordMatch();
    });

    $('#OldPassword').on('input', function () {
        setTimeout(() => updateRequirementsChecklist($('#newPasswordField').val()), 100);
    });

    // Check password confirmation match
    function checkPasswordMatch() {
        const newPassword = $('#newPasswordField').val();
        const confirmPassword = $('#confirmPasswordField').val();
        const matchIcon = $('#passwordMatchIcon');
        const mismatchIcon = $('#passwordMismatchIcon');

        if (confirmPassword) {
            if (newPassword === confirmPassword) {
                matchIcon.removeClass('d-none');
                mismatchIcon.addClass('d-none');
                $('#confirmPasswordField').removeClass('is-invalid').addClass('is-valid');
            } else {
                matchIcon.addClass('d-none');
                mismatchIcon.removeClass('d-none');
                $('#confirmPasswordField').removeClass('is-valid').addClass('is-invalid');
            }
        } else {
            matchIcon.addClass('d-none');
            mismatchIcon.addClass('d-none');
            $('#confirmPasswordField').removeClass('is-valid is-invalid');
        }
    }

    $('#confirmPasswordField').on('input', checkPasswordMatch);

    // Password visibility toggles
    $('#toggleCurrentPassword').on('click', function () {
        togglePasswordVisibility('#OldPassword', '#currentPasswordToggleIcon');
    });

    $('#toggleNewPassword').on('click', function () {
        togglePasswordVisibility('#newPasswordField, #confirmPasswordField', '#newPasswordToggleIcon');
    });

    function togglePasswordVisibility(fieldSelector, iconSelector) {
        const fields = $(fieldSelector);
        const icon = $(iconSelector);

        if (fields.first().attr('type') === 'password') {
            fields.attr('type', 'text');
            icon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            fields.attr('type', 'password');
            icon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    }

    // Form submission
    $('#changePasswordForm').on('submit', function (e) {
        const isValid = this.checkValidity();
        const newPassword = $('#newPasswordField').val();
        const oldPassword = $('#OldPassword').val();

        // Additional validations
        if (newPassword === oldPassword) {
            e.preventDefault();
            e.stopPropagation();
            $('#newPasswordField').addClass('is-invalid');
            showToast('New password must be different from your current password.', 'warning');
            return false;
        }

        if (!isValid) {
            e.preventDefault();
            e.stopPropagation();
            $(this).addClass('was-validated');
            return false;
        }

        // Show loading state
        $('#changePasswordBtn').prop('disabled', true);
        $('#changePasswordSpinner').removeClass('d-none');

        // Show confirmation dialog for logout option
        if ($('#LogoutFromAllDevices').is(':checked')) {
            if (!confirm('You will be logged out from all devices. Continue?')) {
                e.preventDefault();
                $('#changePasswordBtn').prop('disabled', false);
                $('#changePasswordSpinner').addClass('d-none');
                return false;
            }
        }
    });

    // Clear validation on input
    $('input').on('input', function () {
        if ($(this).hasClass('is-invalid')) {
            $(this).removeClass('is-invalid');
        }
    });

    // Auto-focus based on field states
    if (!$('#OldPassword').val()) {
        $('#OldPassword')[0].focus();
    }

    // Initialize password strength
    updatePasswordStrength();

    // Security tooltip
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Auto-dismiss success/error messages
    setTimeout(() => {
        $('.alert-dismissible').alert('close');
    }, 8000);
});

// Toast notification helper
function showToast(message, type = 'info') {
    const toastId = 'toast-' + Date.now();
    const toast = $(`
                <div id="${toastId}" class="toast align-items-center text-bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                    <div class="d-flex">
                        <div class="toast-body">${message}</div>
                        <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                    </div>
                </div>
            `);

    if (!$('.toast-container').length) {
        $('body').append('<div class="toast-container position-fixed bottom-0 end-0 p-3"></div>');
    }

    $('.toast-container').append(toast);
    const bsToast = new bootstrap.Toast(toast[0], { delay: 5000 });
    bsToast.show();

    // Remove from DOM after hiding
    toast.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

// Show success message if password was changed
if (Model.PasswordChanged) {
    <text>
        $(document).ready(function() {
            showToast('Password changed successfully! @(Model.LogoutFromAllDevices ? "You will be logged out from all devices." : "")', 'success')
            });
    </text>
}