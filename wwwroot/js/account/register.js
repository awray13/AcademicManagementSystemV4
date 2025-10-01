/*
 * Registration Page JavaScript
 * Handles registration form interactions, validation, and user experience
 * File: /wwwroot/js/account/register.js
 */

// Password visibility toggle
function togglePassword(fieldId) {
    const field = document.getElementById(fieldId);
    const icon = document.getElementById(fieldId + '-icon');

    if (field.type === 'password') {
        field.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        field.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
}

// Password strength checker
function checkPasswordStrength(password) {
    let score = 0;
    const requirements = {
        length: password.length >= 8,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /\d/.test(password),
        special: /[^\da-zA-Z]/.test(password)
    };

    Object.values(requirements).forEach(met => met && score++);

    // Update requirement indicators
    updateRequirement('req-length', requirements.length);
    updateRequirement('req-uppercase', requirements.uppercase);
    updateRequirement('req-lowercase', requirements.lowercase);
    updateRequirement('req-number', requirements.number);
    updateRequirement('req-special', requirements.special);

    const strength = ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong'][score];
    const colors = ['danger', 'danger', 'warning', 'info', 'success'];
    const widths = [20, 40, 60, 80, 100];

    return {
        score: score,
        strength: strength,
        color: colors[score],
        width: widths[score]
    };
}

function updateRequirement(elementId, met) {
    const element = document.getElementById(elementId);
    if (met) {
        element.classList.add('text-success');
        element.classList.remove('text-muted');
        element.innerHTML = element.innerHTML.replace(/^/, '✓ ');
    } else {
        element.classList.remove('text-success');
        element.classList.add('text-muted');
        element.innerHTML = element.innerHTML.replace('✓ ', '');
    }
}

$(function () {
    // Real-time email availability checking
    let emailCheckTimeout;
    $('#Email').on('input', function () {
        const email = $(this).val();
        clearTimeout(emailCheckTimeout);

        if (email.length > 5 && email.includes('@')) {
            $('#email-availability').show().html('<small class="text-muted"><i class="fas fa-spinner fa-spin me-1"></i>Checking availability...</small>');

            emailCheckTimeout = setTimeout(function () {
                $.post('@Url.Action("CheckEmailAvailability")', { email: email })
                    .done(function (result) {
                        if (result.available) {
                            $('#email-availability').html('<small class="text-success"><i class="fas fa-check me-1"></i>Email is available</small>');
                        } else {
                            $('#email-availability').html('<small class="text-danger"><i class="fas fa-times me-1"></i>Email is already in use</small>');
                        }
                    })
                    .fail(function () {
                        $('#email-availability').html('<small class="text-warning"><i class="fas fa-exclamation-triangle me-1"></i>Unable to check availability</small>');
                    });
            }, 1000);
        } else {
            $('#email-availability').hide();
        }
    });

    // Real-time password strength checking
    $('#Password').on('input', function () {
        const password = $(this).val();
        const result = checkPasswordStrength(password);

        $('#password-strength')
            .removeClass('bg-danger bg-warning bg-info bg-success')
            .addClass('bg-' + result.color)
            .css('width', result.width + '%');

        if (password.length > 0) {
            $('#password-strength-text').text(result.strength + ' password');
        } else {
            $('#password-strength-text').text('Password strength will appear here');
        }
    });

    // Password confirmation matching
    $('#ConfirmPassword').on('input', function () {
        const password = $('#Password').val();
        const confirmPassword = $(this).val();
        const matchDiv = $('#password-match');

        if (confirmPassword.length > 0) {
            matchDiv.show();
            if (password === confirmPassword) {
                $(this).removeClass('is-invalid').addClass('is-valid');
                matchDiv.html('<small class="text-success"><i class="fas fa-check me-1"></i>Passwords match</small>');
            } else {
                $(this).removeClass('is-valid').addClass('is-invalid');
                matchDiv.html('<small class="text-danger"><i class="fas fa-times me-1"></i>Passwords do not match</small>');
            }
        } else {
            matchDiv.hide();
            $(this).removeClass('is-valid is-invalid');
        }
    });

    // Phone number formatting
    $('#PhoneNumber').on('input', function () {
        let value = $(this).val().replace(/\D/g, '');
        if (value.length >= 10) {
            const formatted = value.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
            $(this).val(formatted);
        }
    });

    // Form validation before submit
    $('#registerForm').on('submit', function (e) {
        const password = $('#Password').val();
        const result = checkPasswordStrength(password);

        if (result.score < 3) {
            e.preventDefault();
            alert('Please choose a stronger password before continuing.');
            return false;
        }

        if (!$('#AgreeToTerms').is(':checked')) {
            e.preventDefault();
            alert('You must agree to the terms and conditions to create an account.');
            return false;
        }

        // Disable submit button to prevent double submission
        $('#registerBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Creating Account...');
    });

    // Name field formatting
    $('#FirstName, #LastName').on('blur', function () {
        const value = $(this).val();
        if (value) {
            // Title case formatting
            const formatted = value.toLowerCase().replace(/\b\w/g, l => l.toUpperCase());
            $(this).val(formatted);
        }
    });
});