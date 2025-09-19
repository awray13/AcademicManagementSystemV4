/*
 * Registration Page JavaScript
 * Handles registration form interactions, validation, and user experience
 * File: /wwwroot/js/account/register.js
 */

$(function () {
    'use strict';

    // Initialize registration page functionality
    const RegisterPage = {

        // Configuration
        config: {
            passwordStrengthDelay: 300,
            emailCheckDelay: 1000,
            maxNameLength: 50,
            allowedEmailDomains: [
                // Educational domains
                'edu', 'university', 'college', 'wgu.edu', 'student.wgu.edu',
                // Major email providers
                'gmail.com', 'yahoo.com', 'outlook.com', 'hotmail.com', 'live.com'
            ]
        },

        // State tracking
        state: {
            emailAvailable: null,
            passwordStrength: null,
            formSubmitting: false
        },

        // Initialize all registration page features
        init: function () {
            this.initPasswordStrength();
            this.initEmailValidation();
            this.initPasswordToggle();
            this.initFormValidation();
            this.initFormSubmission();
            this.initInputSanitization();
            this.initTermsModals();
            this.initAccessibility();
            this.initTooltips();
            this.handleExistingErrors();
        },

        // Initialize password strength checking
        initPasswordStrength: function () {
            let strengthTimeout;

            // Set up real-time password strength monitoring
            const strengthHandler = PasswordStrength.createInputHandler('#passwordField', {
                strengthSelector: '#password-strength-container',
                suggestionsSelector: '#passwordSuggestions',
                debounceMs: RegisterPage.config.passwordStrengthDelay,
                personalInfo: {
                    email: () => $('#emailField').val(),
                    firstName: () => $('#FirstName').val(),
                    lastName: () => $('#LastName').val()
                }
            });

            // Custom strength display update
            $('#passwordField').on('input', function () {
                clearTimeout(strengthTimeout);
                strengthTimeout = setTimeout(() => {
                    RegisterPage.updatePasswordStrengthDisplay();
                    RegisterPage.checkPasswordMatch();
                }, RegisterPage.config.passwordStrengthDelay);
            });

            // Initial strength calculation
            RegisterPage.updatePasswordStrengthDisplay();
        },

        // Update password strength visual indicators
        updatePasswordStrengthDisplay: function () {
            const password = $('#passwordField').val();
            const personalInfo = {
                email: $('#emailField').val(),
                firstName: $('#FirstName').val(),
                lastName: $('#LastName').val()
            };

            const result = PasswordStrength.calculate(password);
            const personalCheck = PasswordStrength.containsPersonalInfo(password, personalInfo);

            // Update progress bar
            $('#passwordStrengthBar')
                .css('width', result.score + '%')
                .removeClass('bg-secondary bg-danger bg-warning bg-info bg-success')
                .addClass('bg-' + result.level.color);

            // Update strength text
            $('#passwordStrengthText')
                .text(result.level.text)
                .removeClass('text-secondary text-danger text-warning text-info text-success')
                .addClass('text-' + result.level.color);

            // Update suggestions
            let suggestions = [...result.suggestions];
            if (personalCheck.contains) {
                suggestions.unshift(`Avoid using your ${personalCheck.info.join(', ')} in your password`);
            }

            if (suggestions.length === 0) {
                suggestions = ['Excellent password strength! 🔒'];
            }

            const suggestionsHtml = suggestions.map(s =>
                `<small class="text-muted d-block"><i class="fas fa-lightbulb me-1"></i>${s}</small>`
            ).join('');
            $('#passwordSuggestions').html(suggestionsHtml);

            // Store strength for form validation
            RegisterPage.state.passwordStrength = result;
        },

        // Initialize email availability checking
        initEmailValidation: function () {
            let emailTimeout;

            $('#emailField').on('blur', function () {
                clearTimeout(emailTimeout);
                emailTimeout = setTimeout(() => {
                    RegisterPage.checkEmailAvailability();
                }, RegisterPage.config.emailCheckDelay);
            });

            // Auto-correct common email typos
            $('#emailField').on('blur', function () {
                const email = $(this).val().trim();
                if (email) {
                    const correctedEmail = RegisterPage.correctEmailTypos(email);
                    if (correctedEmail !== email) {
                        $(this).val(correctedEmail);
                        showToast(`Email corrected to: ${correctedEmail}`, 'info');
                    }
                }
            });
        },

        // Check email availability (simulated - replace with actual API call)
        checkEmailAvailability: function () {
            const email = $('#emailField').val().trim().toLowerCase();
            const $field = $('#emailField');
            const $availability = $('#emailAvailability');
            const $icon = $('#emailValidationIcon');

            if (!email || !RegisterPage.isValidEmail(email)) {
                $availability.text('');
                $icon.removeClass('fa-check text-success fa-times text-danger').addClass('fa-at text-muted');
                RegisterPage.state.emailAvailable = null;
                return;
            }

            // Show loading state
            $availability.html('<i class="fas fa-spinner fa-spin me-1"></i>Checking availability...');
            $icon.removeClass('fa-check text-success fa-times text-danger fa-at text-muted').addClass('fa-spinner fa-spin text-primary');

            // Simulate API call (replace with actual implementation)
            setTimeout(() => {
                // Simple simulation logic - in real app, make AJAX call to CheckEmailAvailability endpoint
                const isAvailable = !email.includes('taken@') && !email.includes('admin@');

                RegisterPage.state.emailAvailable = isAvailable;

                if (isAvailable) {
                    $availability.html('<i class="fas fa-check text-success me-1"></i>Email is available');
                    $icon.removeClass('fa-spinner fa-spin text-primary fa-times text-danger').addClass('fa-check text-success');
                    RegisterPage.setFieldValid($field);
                } else {
                    $availability.html('<i class="fas fa-times text-danger me-1"></i>Email is already registered');
                    $icon.removeClass('fa-spinner fa-spin text-primary fa-check text-success').addClass('fa-times text-danger');
                    RegisterPage.setFieldError($field, 'This email address is already registered. Please use a different email or try signing in.');
                }
            }, 1500);
        },

        // Email typo correction
        correctEmailTypos: function (email) {
            const commonCorrections = {
                'gmial.com': 'gmail.com',
                'gmai.com': 'gmail.com',
                'gmail.co': 'gmail.com',
                'yahooo.com': 'yahoo.com',
                'yahoo.co': 'yahoo.com',
                'hotmial.com': 'hotmail.com',
                'hotmail.co': 'hotmail.com',
                'outlok.com': 'outlook.com',
                'outlook.co': 'outlook.com'
            };

            const emailParts = email.split('@');
            if (emailParts.length === 2) {
                const domain = emailParts[1].toLowerCase();
                if (commonCorrections[domain]) {
                    return `${emailParts[0]}@${commonCorrections[domain]}`;
                }
            }
            return email;
        },

        // Password visibility toggle
        initPasswordToggle: function () {
            $('#togglePassword').on('click', function () {
                const passwordField = $('#passwordField');
                const confirmPasswordField = $('#confirmPasswordField');
                const icon = $('#passwordToggleIcon');

                if (passwordField.attr('type') === 'password') {
                    passwordField.attr('type', 'text');
                    confirmPasswordField.attr('type', 'text');
                    icon.removeClass('fa-eye').addClass('fa-eye-slash');
                    $(this).attr('aria-label', 'Hide passwords');
                } else {
                    passwordField.attr('type', 'password');
                    confirmPasswordField.attr('type', 'password');
                    icon.removeClass('fa-eye-slash').addClass('fa-eye');
                    $(this).attr('aria-label', 'Show passwords');
                }
            });
        },

        // Initialize form validation
        initFormValidation: function () {
            // Real-time validation for each field
            $('#FirstName').on('blur', function () {
                RegisterPage.validateNameField($(this), 'First name');
            });

            $('#LastName').on('blur', function () {
                RegisterPage.validateNameField($(this), 'Last name');
            });

            $('#emailField').on('blur', function () {
                RegisterPage.validateEmailField($(this));
            });

            $('#passwordField').on('input', function () {
                RegisterPage.validatePasswordField($(this));
                RegisterPage.checkPasswordMatch();
            });

            $('#confirmPasswordField').on('input', function () {
                RegisterPage.checkPasswordMatch();
            });

            $('#PhoneNumber').on('blur', function () {
                RegisterPage.validatePhoneField($(this));
            });

            // Clear validation on input
            $('input').on('input', function () {
                if ($(this).hasClass('is-invalid')) {
                    $(this).removeClass('is-invalid');
                }
            });
        },

        // Validate name fields
        validateNameField: function ($field, fieldName) {
            const name = $field.val().trim();

            if (!name) {
                this.setFieldError($field, `${fieldName} is required.`);
                return false;
            }

            if (name.length > RegisterPage.config.maxNameLength) {
                this.setFieldError($field, `${fieldName} cannot exceed ${RegisterPage.config.maxNameLength} characters.`);
                return false;
            }

            if (!/^[a-zA-Z\s\-'\.]+$/.test(name)) {
                this.setFieldError($field, `${fieldName} can only contain letters, spaces, hyphens, apostrophes, and periods.`);
                return false;
            }

            this.setFieldValid($field);
            return true;
        },

        // Validate email field
        validateEmailField: function ($field) {
            const email = $field.val().trim();

            if (!email) {
                this.setFieldError($field, 'Email address is required.');
                return false;
            }

            if (!this.isValidEmail(email)) {
                this.setFieldError($field, 'Please enter a valid email address.');
                return false;
            }

            if (!this.isAllowedEmailDomain(email)) {
                this.setFieldError($field, 'Please use a valid educational email address or major email provider.');
                return false;
            }

            if (RegisterPage.state.emailAvailable === false) {
                this.setFieldError($field, 'This email address is already registered.');
                return false;
            }

            this.setFieldValid($field);
            return true;
        },

        // Validate password field
        validatePasswordField: function ($field) {
            const password = $field.val();

            if (!password) {
                this.setFieldError($field, 'Password is required.');
                return false;
            }

            if (RegisterPage.state.passwordStrength && RegisterPage.state.passwordStrength.score < 40) {
                this.setFieldError($field, 'Password does not meet security requirements. Please follow the suggestions.');
                return false;
            }

            this.setFieldValid($field);
            return true;
        },

        // Validate phone field
        validatePhoneField: function ($field) {
            const phone = $field.val().trim();

            if (!phone) {
                // Phone is optional
                $field.removeClass('is-invalid is-valid');
                return true;
            }

            const phoneRegex = /^[\+]?[\d\s\-\(\)\.]{10,20}$/;
            if (!phoneRegex.test(phone)) {
                this.setFieldError($field, 'Please enter a valid phone number format.');
                return false;
            }

            this.setFieldValid($field);
            return true;
        },

        // Check password confirmation match
        checkPasswordMatch: function () {
            const password = $('#passwordField').val();
            const confirmPassword = $('#confirmPasswordField').val();
            const $confirmField = $('#confirmPasswordField');
            const $matchIcon = $('#passwordMatchIcon');

            if (!confirmPassword) {
                $matchIcon.addClass('d-none');
                $confirmField.removeClass('is-valid is-invalid');
                return true;
            }

            if (password === confirmPassword) {
                $matchIcon.removeClass('d-none fa-times text-danger').addClass('fa-check text-success');
                RegisterPage.setFieldValid($confirmField);
                return true;
            } else {
                $matchIcon.removeClass('d-none fa-check text-success').addClass('fa-times text-danger');
                RegisterPage.setFieldError($confirmField, 'Passwords do not match.');
                return false;
            }
        },

        // Validate entire form
        validateForm: function () {
            let isValid = true;

            // Validate all required fields
            if (!this.validateNameField($('#FirstName'), 'First name')) isValid = false;
            if (!this.validateNameField($('#LastName'), 'Last name')) isValid = false;
            if (!this.validateEmailField($('#emailField'))) isValid = false;
            if (!this.validatePasswordField($('#passwordField'))) isValid = false;
            if (!this.checkPasswordMatch()) isValid = false;
            if (!this.validatePhoneField($('#PhoneNumber'))) isValid = false;

            // Check terms agreement
            if (!$('#AgreeToTerms').is(':checked')) {
                $('#AgreeToTerms').addClass('is-invalid');
                showToast('Please accept the Terms and Conditions to continue.', 'warning');
                isValid = false;
            }

            // Check email availability
            if (RegisterPage.state.emailAvailable === false) {
                RegisterPage.setFieldError($('#emailField'), 'This email address is already registered.');
                isValid = false;
            }

            return isValid;
        },

        // Form submission handling
        initFormSubmission: function () {
            $('#registerForm').on('submit', function (e) {
                const $form = $(this);

                if (RegisterPage.state.formSubmitting) {
                    e.preventDefault();
                    return false;
                }

                if (!RegisterPage.validateForm() || !$form[0].checkValidity()) {
                    e.preventDefault();
                    e.stopPropagation();
                    $form.addClass('was-validated');
                    return false;
                }

                // Show loading state
                RegisterPage.state.formSubmitting = true;
                const $submitBtn = $('#registerButton');
                const $spinner = $('#registerSpinner');

                $submitBtn.prop('disabled', true);
                $spinner.removeClass('d-none');

                // Update button text
                $submitBtn.find('.fas').hide();
                $submitBtn.append('<span class="button-text">Creating Account...</span>');

                // Form will continue to submit naturally
            });
        },

        // Input sanitization
        initInputSanitization: function () {
            // Name fields - proper case conversion
            $('#FirstName, #LastName').on('blur', function () {
                let value = $(this).val().trim();
                if (value) {
                    // Convert to proper case
                    value = value.toLowerCase().replace(/\b\w/g, l => l.toUpperCase());
                    // Handle special cases like O'Connor, McDonald
                    value = value.replace(/\b(Mc|Mac)([a-z])/g, (match, prefix, letter) => prefix + letter.toUpperCase());
                    value = value.replace(/\b([A-Z]')([a-z])/g, (match, prefix, letter) => prefix + letter.toUpperCase());
                    $(this).val(value);
                }
            });

            // Phone number formatting
            $('#PhoneNumber').on('input', function () {
                let value = $(this).val().replace(/\D/g, ''); // Remove non-digits

                // Format as (XXX) XXX-XXXX
                if (value.length >= 10) {
                    value = value.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
                } else if (value.length >= 6) {
                    value = value.replace(/(\d{3})(\d{3})/, '($1) $2-');
                } else if (value.length >= 3) {
                    value = value.replace(/(\d{3})/, '($1) ');
                }

                $(this).val(value);
            });

            // Email field - lowercase conversion
            $('#emailField').on('blur', function () {
                $(this).val($(this).val().trim().toLowerCase());
            });
        },

        // Initialize terms and privacy modals
        initTermsModals: function () {
            // Accept terms button in modal
            $('#acceptTermsBtn').on('click', function () {
                $('#AgreeToTerms').prop('checked', true).removeClass('is-invalid');
                $('#termsModal').modal('hide');
                showToast('Terms and Conditions accepted!', 'success');
            });

            // Newsletter subscription info
            $('#SubscribeToNewsletter').on('change', function () {
                if ($(this).is(':checked')) {
                    showToast('You can unsubscribe at any time from your account settings.', 'info');
                }
            });
        },

        // Accessibility enhancements
        initAccessibility: function () {
            // Add ARIA labels and descriptions
            $('#FirstName').attr('aria-describedby', 'firstname-help');
            $('#LastName').attr('aria-describedby', 'lastname-help');
            $('#emailField').attr('aria-describedby', 'email-help emailAvailability');
            $('#passwordField').attr('aria-describedby', 'password-help passwordSuggestions');
            $('#confirmPasswordField').attr('aria-describedby', 'confirm-help');

            // Add hidden help text for screen readers
            this.addScreenReaderHelp();

            // Keyboard navigation for password toggle
            $('#togglePassword').on('keydown', function (e) {
                if (e.which === 13 || e.which === 32) {
                    e.preventDefault();
                    $(this).trigger('click');
                }
            });

            // Form progress announcements
            this.setupFormProgressAnnouncements();
        },

        // Add screen reader help text
        addScreenReaderHelp: function () {
            const helpTexts = {
                'firstname-help': 'Enter your legal first name',
                'lastname-help': 'Enter your legal last name',
                'email-help': 'Enter a valid email address that you have access to',
                'password-help': 'Create a strong password with at least 8 characters',
                'confirm-help': 'Re-enter your password to confirm it matches'
            };

            Object.entries(helpTexts).forEach(([id, text]) => {
                if (!$(`#${id}`).length) {
                    $(`<div id="${id}" class="visually-hidden">${text}</div>`).insertAfter(`[aria-describedby*="${id}"]`);
                }
            });
        },

        // Setup form progress announcements
        setupFormProgressAnnouncements: function () {
            let completedFields = 0;
            const totalRequiredFields = 4; // firstName, lastName, email, password

            $('input[required]').on('blur', function () {
                if ($(this).hasClass('is-valid')) {
                    completedFields++;
                    const progress = Math.round((completedFields / totalRequiredFields) * 100);

                    // Announce progress to screen readers
                    const announcement = $('<div class="visually-hidden" aria-live="polite"></div>');
                    announcement.text(`Form ${progress}% complete`);
                    $('body').append(announcement);

                    setTimeout(() => announcement.remove(), 1000);
                }
            });
        },

        // Initialize tooltips
        initTooltips: function () {
            $('[data-bs-toggle="tooltip"]').each(function () {
                new bootstrap.Tooltip(this, {
                    delay: { show: 500, hide: 100 }
                });
            });
        },

        // Handle existing validation errors
        handleExistingErrors: function () {
            // If there are server-side validation errors, show them as toasts
            $('.field-validation-error').each(function () {
                const errorText = $(this).text();
                if (errorText) {
                    showToast(errorText, 'error');
                }
            });

            // Auto-dismiss validation summary after 10 seconds
            $('.validation-summary').fadeOut(10000);
        },

        // Utility functions
        isValidEmail: function (email) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(email);
        },

        isAllowedEmailDomain: function (email) {
            const domain = email.split('@')[1]?.toLowerCase();
            if (!domain) return false;

            return RegisterPage.config.allowedEmailDomains.some(allowed =>
                domain === allowed || domain.endsWith('.' + allowed)
            );
        },

        setFieldError: function ($field, message) {
            $field.removeClass('is-valid').addClass('is-invalid');
            let feedback = $field.siblings('.invalid-feedback').first();
            if (feedback.length === 0) {
                feedback = $('<div class="invalid-feedback"></div>');
                $field.after(feedback);
            }
            feedback.text(message).show();
        },

        setFieldValid: function ($field) {
            $field.removeClass('is-invalid').addClass('is-valid');
            $field.siblings('.invalid-feedback').hide();
        }
    };

    // Initialize the registration page
    RegisterPage.init();

    // Global reference for external scripts
    window.RegisterPage = RegisterPage;

    // Auto-save form data to prevent loss (development feature)
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        RegisterPage.initAutoSave();
    }

    // Auto-save functionality (development only)
    RegisterPage.initAutoSave = function () {
        const formData = {};

        $('input[type="text"], input[type="email"]').on('blur', function () {
            const fieldName = $(this).attr('name') || $(this).attr('id');
            formData[fieldName] = $(this).val();
            sessionStorage.setItem('registerFormData', JSON.stringify(formData));
        });

        // Restore form data on page load
        const savedData = sessionStorage.getItem('registerFormData');
        if (savedData) {
            try {
                const data = JSON.parse(savedData);
                Object.entries(data).forEach(([key, value]) => {
                    const field = $(`input[name="${key}"], input[id="${key}"]`);
                    if (field.length && !field.val()) {
                        field.val(value);
                    }
                });
            } catch (e) {
                console.warn('Could not restore form data:', e);
            }
        }

        // Clear saved data on successful submission
        $('#registerForm').on('submit', function () {
            sessionStorage.removeItem('registerFormData');
        });
    };

    // Add CSS animations
    $('.card').addClass('fade-in');

    // Handle browser autofill detection
    setTimeout(() => {
        $('input:-webkit-autofill').each(function () {
            $(this).trigger('blur');
        });
    }, 1000);
});