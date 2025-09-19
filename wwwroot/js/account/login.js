/*
 * Login Page JavaScript
 * Handles login form interactions, validation, and user experience
 * File: /wwwroot/js/account/login.js
 */

$(function () {
    'use strict';

    // Initialize login page functionality
    const LoginPage = {

        // Configuration
        config: {
            maxFailedAttempts: 3,
            lockoutDuration: 300000, // 5 minutes in milliseconds
            tooltipDelay: { show: 500, hide: 100 }
        },

        // Initialize all login page features
        init: function () {
            this.initTooltips();
            this.initPasswordToggle();
            this.initFormValidation();
            this.initFormSubmission();
            this.initEmailValidation();
            this.initForgotPassword();
            this.initAccessibility();
            this.initAutoFocus();
            this.initRememberMeTooltip();
            this.initDemoAccountHelper();
            this.handleExistingAlerts();
        },

        // Initialize Bootstrap tooltips
        initTooltips: function () {
            const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl, {
                    delay: LoginPage.config.tooltipDelay
                });
            });
        },

        // Password visibility toggle functionality
        initPasswordToggle: function () {
            $('#togglePassword').on('click', function () {
                const passwordField = $('#Password');
                const passwordToggleIcon = $('#passwordToggleIcon');

                if (passwordField.attr('type') === 'password') {
                    passwordField.attr('type', 'text');
                    passwordToggleIcon.removeClass('fa-eye').addClass('fa-eye-slash');
                    $(this).attr('aria-label', 'Hide password');
                } else {
                    passwordField.attr('type', 'password');
                    passwordToggleIcon.removeClass('fa-eye-slash').addClass('fa-eye');
                    $(this).attr('aria-label', 'Show password');
                }
            });

            // Hide password when clicking outside
            $(document).on('click', function (e) {
                if (!$(e.target).closest('.input-group').length) {
                    const passwordField = $('#Password');
                    const passwordToggleIcon = $('#passwordToggleIcon');

                    if (passwordField.attr('type') === 'text') {
                        passwordField.attr('type', 'password');
                        passwordToggleIcon.removeClass('fa-eye-slash').addClass('fa-eye');
                        $('#togglePassword').attr('aria-label', 'Show password');
                    }
                }
            });
        },

        // Form validation setup
        initFormValidation: function () {
            const form = $('#loginForm')[0];

            // Real-time validation
            $('#Email').on('blur', function () {
                LoginPage.validateEmailField($(this));
            });

            $('#Password').on('input', function () {
                LoginPage.validatePasswordField($(this));
            });

            // Clear validation on input
            $('input').on('input', function () {
                if ($(this).hasClass('is-invalid')) {
                    $(this).removeClass('is-invalid');
                    $(this).next('.invalid-feedback').hide();
                }
            });

            // Form submission validation
            $('#loginForm').on('submit', function (e) {
                if (!form.checkValidity() || !LoginPage.validateForm()) {
                    e.preventDefault();
                    e.stopPropagation();
                    $(this).addClass('was-validated');
                    return false;
                }
            });
        },

        // Email field validation
        validateEmailField: function ($field) {
            const email = $field.val();
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            if (!email) {
                this.setFieldError($field, 'Email address is required.');
                return false;
            }

            if (!emailRegex.test(email)) {
                this.setFieldError($field, 'Please enter a valid email address.');
                return false;
            }

            this.setFieldValid($field);
            return true;
        },

        // Password field validation
        validatePasswordField: function ($field) {
            const password = $field.val();

            if (!password) {
                this.setFieldError($field, 'Password is required.');
                return false;
            }

            if (password.length < 6) {
                this.setFieldError($field, 'Password must be at least 6 characters long.');
                return false;
            }

            this.setFieldValid($field);
            return true;
        },

        // Complete form validation
        validateForm: function () {
            let isValid = true;

            if (!this.validateEmailField($('#Email'))) {
                isValid = false;
            }

            if (!this.validatePasswordField($('#Password'))) {
                isValid = false;
            }

            return isValid;
        },

        // Set field error state
        setFieldError: function ($field, message) {
            $field.removeClass('is-valid').addClass('is-invalid');
            let feedback = $field.siblings('.invalid-feedback');
            if (feedback.length === 0) {
                feedback = $('<div class="invalid-feedback"></div>');
                $field.after(feedback);
            }
            feedback.text(message).show();
        },

        // Set field valid state
        setFieldValid: function ($field) {
            $field.removeClass('is-invalid').addClass('is-valid');
            $field.siblings('.invalid-feedback').hide();
        },

        // Form submission handling
        initFormSubmission: function () {
            $('#loginForm').on('submit', function (e) {
                const $form = $(this);
                const $submitBtn = $('#loginButton');
                const $spinner = $('#loginSpinner');

                // Show loading state
                $submitBtn.prop('disabled', true);
                $spinner.removeClass('d-none');

                // Update button text
                const originalText = $submitBtn.html();
                $submitBtn.find('span').first().text('Signing in...');

                // If form validation fails, restore button state
                setTimeout(function () {
                    if ($form.hasClass('was-validated') && !$form[0].checkValidity()) {
                        $submitBtn.prop('disabled', false);
                        $spinner.addClass('d-none');
                        $submitBtn.html(originalText);
                    }
                }, 100);
            });
        },

        // Enhanced email validation with suggestions
        initEmailValidation: function () {
            $('#Email').on('blur', function () {
                const email = $(this).val().trim();
                if (email) {
                    // Auto-correct common email domain typos
                    const correctedEmail = LoginPage.suggestEmailCorrection(email);
                    if (correctedEmail !== email) {
                        $(this).val(correctedEmail);
                        showToast(`Email corrected to: ${correctedEmail}`, 'info');
                    }
                }
            });
        },

        // Suggest email corrections for common typos
        suggestEmailCorrection: function (email) {
            const commonDomainCorrections = {
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
                if (commonDomainCorrections[domain]) {
                    return `${emailParts[0]}@${commonDomainCorrections[domain]}`;
                }
            }
            return email;
        },

        // Forgot password functionality
        initForgotPassword: function () {
            const $modal = $('#forgotPasswordModal');
            const $emailField = $('#resetEmail');
            const $sendBtn = $('#sendResetBtn');

            // Pre-populate email field when modal opens
            $modal.on('show.bs.modal', function () {
                const currentEmail = $('#Email').val();
                if (currentEmail) {
                    $emailField.val(currentEmail);
                }
                $emailField.removeClass('is-invalid');
            });

            // Validate and send reset email
            $sendBtn.on('click', function () {
                const email = $emailField.val().trim();

                if (!email) {
                    LoginPage.setFieldError($emailField, 'Email address is required.');
                    return;
                }

                const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                if (!emailRegex.test(email)) {
                    LoginPage.setFieldError($emailField, 'Please enter a valid email address.');
                    return;
                }

                // Show loading state
                const originalText = $sendBtn.html();
                $sendBtn.prop('disabled', true)
                    .html('<i class="fas fa-spinner fa-spin me-1"></i>Sending...');

                // Simulate API call (replace with actual implementation)
                setTimeout(() => {
                    $modal.modal('hide');
                    showToast('Password reset instructions have been sent to your email address.', 'success');
                    $sendBtn.prop('disabled', false).html(originalText);
                    $emailField.val('');
                }, 2000);
            });

            // Enter key support in modal
            $emailField.on('keypress', function (e) {
                if (e.which === 13) {
                    $sendBtn.trigger('click');
                }
            });
        },

        // Accessibility enhancements
        initAccessibility: function () {
            // Add ARIA labels
            $('#Email').attr('aria-describedby', 'email-help');
            $('#Password').attr('aria-describedby', 'password-help');

            // Add hidden help text
            if (!$('#email-help').length) {
                $('#Email').after('<div id="email-help" class="visually-hidden">Enter the email address associated with your account</div>');
            }

            if (!$('#password-help').length) {
                $('#Password').after('<div id="password-help" class="visually-hidden">Enter your account password</div>');
            }

            // Keyboard navigation for password toggle
            $('#togglePassword').on('keydown', function (e) {
                if (e.which === 13 || e.which === 32) { // Enter or Space
                    e.preventDefault();
                    $(this).trigger("click");
                }
            });

            // Skip link for screen readers
            if (!$('.skip-link').length) {
                $('body').prepend('<a href="#loginForm" class="skip-link visually-hidden-focusable">Skip to main content</a>');
            }
        },

        // Auto-focus management
        initAutoFocus: function () {
            // Focus email field if empty, otherwise focus password field
            setTimeout(() => {
                const $emailField = $('#Email');
                const $passwordField = $('#Password');

                if (!$emailField.val()) {
                    $emailField[0].focus();
                } else if (!$passwordField.val()) {
                    $passwordField[0].focus();
                }
            }, 100);

            // Auto-advance from email to password on Tab or Enter
            $('#Email').on('keydown', function (e) {
                if (e.which === 13 && $(this).val() && LoginPage.validateEmailField($(this))) {
                    e.preventDefault();
                    $('#Password')[0].focus();
                }
            });
        },

        // Remember Me tooltip with security information
        initRememberMeTooltip: function () {
            const $rememberMe = $('#RememberMe');
            if ($rememberMe.length) {
                $rememberMe.attr('data-bs-toggle', 'tooltip');
                $rememberMe.attr('title', 'Keep me signed in on this device (not recommended for public computers)');
                new bootstrap.Tooltip($rememberMe[0]);
            }
        },

        // Demo account helper (development only)
        initDemoAccountHelper: function () {
            const $demoSection = $('.demo-accounts');
            if ($demoSection.length) {
                // Add click handlers to demo account info
                $demoSection.on('click', 'code', function () {
                    const text = $(this).text();

                    // Copy to clipboard
                    if (navigator.clipboard) {
                        navigator.clipboard.writeText(text).then(() => {
                            showToast('Copied to clipboard: ' + text, 'info');
                        });
                    }

                    // Auto-fill form fields
                    if (text.includes('@')) {
                        $('#Email').val(text);
                        LoginPage.validateEmailField($('#Email'));
                    } else if (text.includes('Password')) {
                        $('#Password').val(text);
                        LoginPage.validatePasswordField($('#Password'));
                    }
                });

                // Quick fill button
                if (!$('.quick-fill-btn').length) {
                    $demoSection.append(`
                        <div class="mt-2 text-center">
                            <button type="button" class="btn btn-outline-primary btn-sm quick-fill-btn">
                                <i class="fas fa-magic me-1"></i>Quick Fill Demo Account
                            </button>
                        </div>
                    `);

                    $('.quick-fill-btn').on('click', function () {
                        $('#Email').val('student@wgu.edu');
                        $('#Password').val('Password123!');
                        LoginPage.validateEmailField($('#Email'));
                        LoginPage.validatePasswordField($('#Password'));
                        showToast('Demo account credentials filled', 'success');
                    });
                }
            }
        },

        // Handle existing alerts
        handleExistingAlerts: function () {
            // Auto-dismiss alerts after 10 seconds
            $('.alert-dismissible').each(function () {
                const $alert = $(this);
                setTimeout(() => {
                    $alert.alert('close');
                }, 10000);
            });

            // Handle lockout countdown
            const $lockoutAlert = $('.alert:contains("locked out")');
            if ($lockoutAlert.length) {
                this.startLockoutCountdown($lockoutAlert);
            }
        },

        // Start lockout countdown timer
        startLockoutCountdown: function ($alert) {
            const timeRegex = /(\d+):(\d+)/;
            const match = $alert.text().match(timeRegex);

            if (match) {
                let totalSeconds = parseInt(match[1]) * 60 + parseInt(match[2]);

                const countdown = setInterval(() => {
                    totalSeconds--;

                    if (totalSeconds <= 0) {
                        clearInterval(countdown);
                        $alert.fadeOut();
                        $('#loginForm input, #loginForm button').prop('disabled', false);
                        showToast('Account unlocked. You may now try to sign in again.', 'success');
                        return;
                    }

                    const minutes = Math.floor(totalSeconds / 60);
                    const seconds = totalSeconds % 60;
                    const timeString = `${minutes}:${seconds.toString().padStart(2, '0')}`;

                    $alert.html($alert.html().replace(/\d+:\d+/, timeString));
                }, 1000);
            }
        },

        // Utility function to handle failed login attempts
        handleFailedLogin: function (attempts, maxAttempts) {
            if (attempts >= maxAttempts) {
                $('#loginForm input, #loginForm button').prop('disabled', true);
                showToast('Account temporarily locked due to multiple failed attempts.', 'error');
            } else {
                const remaining = maxAttempts - attempts;
                showToast(`Login failed. ${remaining} attempt(s) remaining before account lockout.`, 'warning');
            }
        },

        // Security warning for public computers
        showPublicComputerWarning: function () {
            if ($('#RememberMe').is(':checked')) {
                showToast('Remember me is selected. Ensure this is a private device.', 'warning');
            }
        }
    };

    // Initialize the login page
    LoginPage.init();

    // Global reference for external scripts
    window.LoginPage = LoginPage;

    // Handle form submission security checks
    $('#loginForm').on('submit', function () {
        LoginPage.showPublicComputerWarning();
    });

    // Add CSS classes for animations
    $('.card, .alert').addClass('fade-in');

    // Handle browser autofill
    setTimeout(() => {
        $('input:-webkit-autofill').each(function () {
            const $this = $(this);
            if ($this.val()) {
                $this.trigger('blur');
            }
        });
    }, 500);

    // Prevent form resubmission on browser back
    if (window.history.replaceState) {
        window.history.replaceState(null, null, window.location.href);
    }

    // Handle connection issues
    window.addEventListener('online', () => {
        showToast('Connection restored', 'success');
        $('#loginButton').prop('disabled', false);
    });

    window.addEventListener('offline', () => {
        showToast('Connection lost. Please check your internet connection.', 'warning');
        $('#loginButton').prop('disabled', true);
    });
});