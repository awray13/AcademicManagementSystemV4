/*
 * Enhanced Registration Page JavaScript
 * Handles registration form interactions, validation, and user experience
 * Ensures proper redirection to Home View after successful registration
 * File: /wwwroot/js/account/register.js
 */

// Global variables
let emailCheckTimeout;
let isEmailAvailable = false;
let passwordStrengthScore = 0;

// Password visibility toggle function
function togglePasswordVisibility(fieldId) {
    const field = document.getElementById(fieldId);
    const icon = document.getElementById(fieldId + '-toggle-icon');

    if (field && icon) {
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
}

// Enhanced password strength checker
function checkPasswordStrength(password) {
    let score = 0;
    const requirements = {
        length: password.length >= 8,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /\d/.test(password),
        special: /[^\da-zA-Z]/.test(password)
    };

    // Calculate score
    Object.values(requirements).forEach(met => met && score++);

    // Bonus points for extra length and complexity
    if (password.length >= 12) score += 0.5;
    if (password.length >= 16) score += 0.5;

    // Penalty for common patterns
    const commonPatterns = ['123', 'abc', 'password', 'qwerty'];
    if (commonPatterns.some(pattern => password.toLowerCase().includes(pattern))) {
        score -= 1;
    }

    score = Math.max(0, Math.min(5, score)); // Clamp between 0 and 5

    // Update requirement indicators
    updateRequirement('req-length', requirements.length);
    updateRequirement('req-uppercase', requirements.uppercase);
    updateRequirement('req-lowercase', requirements.lowercase);
    updateRequirement('req-number', requirements.number);
    updateRequirement('req-special', requirements.special);

    const strengthLevels = ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong'];
    const colors = ['danger', 'danger', 'warning', 'info', 'success'];
    const widths = [20, 40, 60, 80, 100];

    const strengthIndex = Math.floor(score);
    passwordStrengthScore = score;

    return {
        score: score,
        strength: strengthLevels[strengthIndex] || 'Very Weak',
        color: colors[strengthIndex] || 'danger',
        width: widths[strengthIndex] || 20
    };
}

function updateRequirement(elementId, met) {
    const element = document.getElementById(elementId);
    if (element) {
        if (met) {
            element.classList.add('text-success', 'password-requirement', 'met');
            element.classList.remove('text-muted');
        } else {
            element.classList.remove('text-success', 'password-requirement', 'met');
            element.classList.add('text-muted');
        }
    }
}

// Show loading overlay
function showLoadingOverlay() {
    document.getElementById('loadingOverlay').style.display = 'flex';
}

// Hide loading overlay
function hideLoadingOverlay() {
    document.getElementById('loadingOverlay').style.display = 'none';
}

// Show success message and redirect
function showSuccessAndRedirect(message, firstName) {
    hideLoadingOverlay();
    
    // Create success notification
    const successAlert = `
        <div class="alert alert-success alert-dismissible fade show position-fixed" 
             style="top: 20px; right: 20px; z-index: 10000; min-width: 300px;" role="alert">
            <i class="fas fa-check-circle me-2"></i>
            <strong>Welcome, ${firstName}!</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', successAlert);
    
    // Redirect to Home Index after a short delay
    setTimeout(() => {
        window.location.href = '/Home/Index';
    }, 2000);
}

// Enhanced form validation
function validateRegistrationForm() {
    let isValid = true;
    const errors = [];

    // Get form values
    const firstName = $('#FirstName').val().trim();
    const lastName = $('#LastName').val().trim();
    const email = $('#Email').val().trim();
    const password = $('#Password').val();
    const confirmPassword = $('#ConfirmPassword').val();
    const agreeToTerms = $('#AgreeToTerms').is(':checked');

    // Clear previous validation states
    $('.form-control').removeClass('is-invalid is-valid');

    // Validate first name
    if (!firstName) {
        errors.push('First name is required.');
        $('#FirstName').addClass('is-invalid');
        isValid = false;
    } else if (firstName.length < 2) {
        errors.push('First name must be at least 2 characters.');
        $('#FirstName').addClass('is-invalid');
        isValid = false;
    } else {
        $('#FirstName').addClass('is-valid');
    }

    // Validate last name
    if (!lastName) {
        errors.push('Last name is required.');
        $('#LastName').addClass('is-invalid');
        isValid = false;
    } else if (lastName.length < 2) {
        errors.push('Last name must be at least 2 characters.');
        $('#LastName').addClass('is-invalid');
        isValid = false;
    } else {
        $('#LastName').addClass('is-valid');
    }

    // Validate email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!email) {
        errors.push('Email address is required.');
        $('#Email').addClass('is-invalid');
        isValid = false;
    } else if (!emailRegex.test(email)) {
        errors.push('Please enter a valid email address.');
        $('#Email').addClass('is-invalid');
        isValid = false;
    } else if (!isEmailAvailable) {
        errors.push('This email address is already in use.');
        $('#Email').addClass('is-invalid');
        isValid = false;
    } else {
        $('#Email').addClass('is-valid');
    }

    // Validate password
    if (!password) {
        errors.push('Password is required.');
        $('#Password').addClass('is-invalid');
        isValid = false;
    } else if (password.length < 8) {
        errors.push('Password must be at least 8 characters long.');
        $('#Password').addClass('is-invalid');
        isValid = false;
    } else if (passwordStrengthScore < 3) {
        errors.push('Please choose a stronger password.');
        $('#Password').addClass('is-invalid');
        isValid = false;
    } else {
        $('#Password').addClass('is-valid');
    }

    // Validate password confirmation
    if (!confirmPassword) {
        errors.push('Password confirmation is required.');
        $('#ConfirmPassword').addClass('is-invalid');
        isValid = false;
    } else if (password !== confirmPassword) {
        errors.push('Passwords do not match.');
        $('#ConfirmPassword').addClass('is-invalid');
        isValid = false;
    } else {
        $('#ConfirmPassword').addClass('is-valid');
    }

    // Validate terms agreement
    if (!agreeToTerms) {
        errors.push('You must agree to the terms and conditions.');
        isValid = false;
    }

    // Display errors if any
    if (errors.length > 0) {
        const errorHtml = `
            <div class="alert alert-danger" role="alert">
                <h6><i class="fas fa-exclamation-triangle me-2"></i>Please correct the following errors:</h6>
                <ul class="mb-0">
                    ${errors.map(error => `<li>${error}</li>`).join('')}
                </ul>
            </div>
        `;
        
        // Remove existing error alerts
        $('.alert-danger').remove();
        
        // Add new error alert at the top of the form
        $('#registerForm').prepend(errorHtml);
        
        // Scroll to top of form
        $('#registerForm')[0].scrollIntoView({ behavior: 'smooth' });
    }

    return isValid;
}

// Document ready function
$(function () {
    console.log('Registration page JavaScript loaded');

    // Email availability checking
    $('#Email').on('input', function () {
        const email = $(this).val().trim();
        clearTimeout(emailCheckTimeout);

        if (email.length > 5 && email.includes('@')) {
            $('#email-availability').show().html('<small class="text-muted"><i class="fas fa-spinner fa-spin me-1"></i>Checking availability...</small>');
            isEmailAvailable = false;

            emailCheckTimeout = setTimeout(function () {
                $.post('/Account/CheckEmailAvailability', { email: email })
                    .done(function (result) {
                        if (result.available) {
                            $('#email-availability').html('<small class="text-success"><i class="fas fa-check me-1"></i>Email is available</small>');
                            $('#Email').removeClass('is-invalid').addClass('is-valid');
                            isEmailAvailable = true;
                        } else {
                            $('#email-availability').html('<small class="text-danger"><i class="fas fa-times me-1"></i>Email is already in use</small>');
                            $('#Email').removeClass('is-valid').addClass('is-invalid');
                            isEmailAvailable = false;
                        }
                    })
                    .fail(function () {
                        $('#email-availability').html('<small class="text-warning"><i class="fas fa-exclamation-triangle me-1"></i>Unable to check availability</small>');
                        isEmailAvailable = true; // Allow form submission if check fails
                    });
            }, 1000);
        } else {
            $('#email-availability').hide();
            $('#Email').removeClass('is-valid is-invalid');
            isEmailAvailable = false;
        }
    });

    // Real-time password strength checking
    $('#Password').on('input', function () {
        const password = $(this).val();
        
        if (password.length > 0) {
            const result = checkPasswordStrength(password);

            $('#password-strength')
                .removeClass('bg-danger bg-warning bg-info bg-success')
                .addClass('bg-' + result.color)
                .css('width', result.width + '%');

            $('#password-strength-text')
                .removeClass('text-danger text-warning text-info text-success text-muted')
                .addClass('text-' + result.color)
                .text(result.strength + ' password');
        } else {
            $('#password-strength').css('width', '0%');
            $('#password-strength-text').removeClass().addClass('text-muted').text('Password strength will appear here');
            passwordStrengthScore = 0;
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

    // Name field formatting
    $('#FirstName, #LastName').on('blur', function () {
        const value = $(this).val().trim();
        if (value) {
            // Title case formatting
            const formatted = value.toLowerCase().replace(/\b\w/g, l => l.toUpperCase());
            $(this).val(formatted);
        }
    });

    // Enhanced form submission with proper redirection
    $('#registerForm').on('submit', function (e) {
        console.log('Form submission started');
        
        // Prevent default submission initially
        e.preventDefault();
        
        // Remove any existing error alerts
        $('.alert-danger').remove();
        
        // Perform client-side validation
        if (!validateRegistrationForm()) {
            console.log('Client-side validation failed');
            return false;
        }

        // Show loading state
        const $submitBtn = $('#registerBtn');
        const originalBtnContent = $submitBtn.html();
        $submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Creating Account...');
        
        // Show loading overlay
        showLoadingOverlay();

        console.log('Submitting form via AJAX...');

        // Submit form via AJAX to handle response properly
        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: $(this).serialize(),
            success: function(response) {
                console.log('Registration response received:', response);
                
                // Check if it's a JSON response with success indicator
                if (typeof response === 'object' && response.success) {
                    console.log('Registration successful');
                    showSuccessAndRedirect(response.message, response.firstName);
                } else if (typeof response === 'string' && (response.includes('Welcome') || response.includes('success'))) {
                    // Fallback for HTML responses
                    const firstName = $('#FirstName').val();
                    showSuccessAndRedirect('Your account has been created successfully!', firstName);
                } else {
                    // If there are validation errors, update the page content
                    console.log('Registration failed with validation errors');
                    hideLoadingOverlay();
                    $('body').html(response);
                }
            },
            error: function(xhr, status, error) {
                console.error('Registration error:', error);
                hideLoadingOverlay();
                
                // Restore button state
                $submitBtn.prop('disabled', false).html(originalBtnContent);
                
                // Show error message
                const errorAlert = `
                    <div class="alert alert-danger" role="alert">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        An error occurred during registration. Please try again.
                    </div>
                `;
                $('#registerForm').prepend(errorAlert);
            }
        });

        return false; // Prevent normal form submission
    });

    // Re-enable submit button if there are validation errors on page load
    $(function() {
        if ($('.alert-danger').length > 0 || $('.text-danger:visible').length > 0) {
            $('#registerBtn').prop('disabled', false).html('<i class="fas fa-user-plus me-2"></i>Create Account');
        }
    });

    // Initialize email availability for existing value
    const existingEmail = $('#Email').val();
    if (existingEmail) {
        $('#Email').trigger('input');
    }

    // Initialize password strength for existing value
    const existingPassword = $('#Password').val();
    if (existingPassword) {
        $('#Password').trigger('input');
    }
});