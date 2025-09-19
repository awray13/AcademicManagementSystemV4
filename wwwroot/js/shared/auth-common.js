/*
 * Authentication Common JavaScript
 * Shared utilities and functions used across all authentication pages
 * File: /wwwroot/js/shared/auth-common.js
 */

(function (window, $) {
    'use strict';

    // Create namespace for authentication utilities
    window.AuthCommon = window.AuthCommon || {};

    const AuthCommon = {

        // Configuration constants
        config: {
            // Password visibility settings
            passwordHideDelay: 30000, // Auto-hide password after 30 seconds

            // Form settings
            formSubmissionDelay: 500, // Prevent double submission
            validationDelay: 300, // Debounce validation

            // Security settings
            maxFailedAttempts: 3,
            lockoutDuration: 300000, // 5 minutes

            // UI settings
            toastDuration: 5000,
            animationDuration: 300,

            // Accessibility settings
            focusDelay: 100,
            ariaLiveRegionId: 'auth-aria-live'
        },

        // State management
        state: {
            formSubmitting: false,
            passwordVisible: false,
            validationTimeout: null,
            passwordHideTimeout: null,
            currentFocus: null
        },

        // Initialize common authentication functionality
        init: function () {
            this.initAriaLiveRegion();
            this.initGlobalEventHandlers();
            this.initFormSecurity();
            this.initAccessibilityFeatures();
            this.initConnectionMonitoring();
            this.initBrowserCompatibility();
            this.setupErrorHandling();
        },

        // Create ARIA live region for screen reader announcements
        initAriaLiveRegion: function () {
            if (!document.getElementById(this.config.ariaLiveRegionId)) {
                const liveRegion = document.createElement('div');
                liveRegion.id = this.config.ariaLiveRegionId;
                liveRegion.className = 'visually-hidden';
                liveRegion.setAttribute('aria-live', 'polite');
                liveRegion.setAttribute('aria-atomic', 'true');
                document.body.appendChild(liveRegion);
            }
        },

        // Announce message to screen readers
        announceToScreenReader: function (message) {
            const liveRegion = document.getElementById(this.config.ariaLiveRegionId);
            if (liveRegion) {
                liveRegion.textContent = message;
                // Clear after announcement
                setTimeout(() => {
                    liveRegion.textContent = '';
                }, 1000);
            }
        },

        // Initialize global event handlers
        initGlobalEventHandlers: function () {
            // Handle browser back button
            window.addEventListener('popstate', this.handleBrowserNavigation.bind(this));

            // Handle page visibility changes
            document.addEventListener('visibilitychange', this.handleVisibilityChange.bind(this));

            // Handle keyboard shortcuts
            document.addEventListener('keydown', this.handleGlobalKeydown.bind(this));

            // Prevent form resubmission on refresh
            if (window.history.replaceState) {
                window.history.replaceState(null, null, window.location.href);
            }
        },

        // Form security enhancements
        initFormSecurity: function () {
            // Prevent multiple form submissions
            $('form').on('submit', function (e) {
                const $form = $(this);

                if (AuthCommon.state.formSubmitting) {
                    e.preventDefault();
                    return false;
                }

                if ($form[0].checkValidity()) {
                    AuthCommon.state.formSubmitting = true;

                    // Re-enable after delay to handle errors
                    setTimeout(() => {
                        AuthCommon.state.formSubmitting = false;
                    }, AuthCommon.config.formSubmissionDelay);
                }
            });

            // Add CSRF token to all AJAX requests
            $.ajaxSetup({
                beforeSend: function (xhr, settings) {
                    if (!/^(GET|HEAD|OPTIONS|TRACE)$/i.test(settings.type) && !this.crossDomain) {
                        const token = $('input[name="__RequestVerificationToken"]').val();
                        if (token) {
                            xhr.setRequestHeader('RequestVerificationToken', token);
                        }
                    }
                }
            });

            // Password field security enhancements
            this.enhancePasswordSecurity();
        },

        // Enhanced password field security
        enhancePasswordSecurity: function () {
            // Auto-hide passwords after delay
            $('input[type="password"]').on('focus', function () {
                const $field = $(this);

                // Clear any existing timeout
                if (AuthCommon.state.passwordHideTimeout) {
                    clearTimeout(AuthCommon.state.passwordHideTimeout);
                }

                // Set new timeout to hide password
                AuthCommon.state.passwordHideTimeout = setTimeout(() => {
                    if ($field.attr('type') === 'text') {
                        AuthCommon.hidePassword($field);
                    }
                }, AuthCommon.config.passwordHideDelay);
            });

            // Prevent password autocomplete on sensitive forms
            $('input[autocomplete="new-password"]').attr('autocomplete', 'new-password');
        },

        // Password visibility utilities
        togglePassword: function (fieldSelector, iconSelector) {
            const $field = $(fieldSelector);
            const $icon = $(iconSelector);

            if ($field.attr('type') === 'password') {
                this.showPassword($field, $icon);
            } else {
                this.hidePassword($field, $icon);
            }
        },

        showPassword: function ($field, $icon) {
            $field.attr('type', 'text');
            if ($icon) {
                $icon.removeClass('fa-eye').addClass('fa-eye-slash');
            }

            AuthCommon.state.passwordVisible = true;
            this.announceToScreenReader('Password is now visible');

            // Auto-hide after delay for security
            if (AuthCommon.state.passwordHideTimeout) {
                clearTimeout(AuthCommon.state.passwordHideTimeout);
            }

            AuthCommon.state.passwordHideTimeout = setTimeout(() => {
                this.hidePassword($field, $icon);
            }, AuthCommon.config.passwordHideDelay);
        },

        hidePassword: function ($field, $icon) {
            $field.attr('type', 'password');
            if ($icon) {
                $icon.removeClass('fa-eye-slash').addClass('fa-eye');
            }

            AuthCommon.state.passwordVisible = false;
            this.announceToScreenReader('Password is now hidden');

            if (AuthCommon.state.passwordHideTimeout) {
                clearTimeout(AuthCommon.state.passwordHideTimeout);
                AuthCommon.state.passwordHideTimeout = null;
            }
        },

        // Form validation utilities
        validateField: function ($field, validationType, options = {}) {
            const fieldValue = $field.val().trim();
            let isValid = true;
            let message = '';

            switch (validationType) {
                case 'required':
                    isValid = fieldValue.length > 0;
                    message = options.message || `${this.getFieldLabel($field)} is required.`;
                    break;

                case 'email':
                    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                    isValid = emailRegex.test(fieldValue);
                    message = options.message || 'Please enter a valid email address.';
                    break;

                case 'minLength':
                    isValid = fieldValue.length >= (options.minLength || 6);
                    message = options.message || `Must be at least ${options.minLength || 6} characters long.`;
                    break;

                case 'maxLength':
                    isValid = fieldValue.length <= (options.maxLength || 255);
                    message = options.message || `Cannot exceed ${options.maxLength || 255} characters.`;
                    break;

                case 'pattern':
                    const regex = new RegExp(options.pattern);
                    isValid = regex.test(fieldValue);
                    message = options.message || 'Invalid format.';
                    break;

                case 'match':
                    const compareValue = $(options.compareField).val();
                    isValid = fieldValue === compareValue;
                    message = options.message || 'Fields do not match.';
                    break;

                default:
                    console.warn(`Unknown validation type: ${validationType}`);
                    return true;
            }

            this.setFieldValidation($field, isValid, message);
            return isValid;
        },

        // Set field validation state
        setFieldValidation: function ($field, isValid, message = '') {
            $field.removeClass('is-valid is-invalid');

            if (isValid) {
                $field.addClass('is-valid');
                this.hideFeedback($field);
            } else {
                $field.addClass('is-invalid');
                this.showFeedback($field, message, 'invalid-feedback');
            }

            // Announce validation result to screen readers
            if (!isValid) {
                this.announceToScreenReader(`${this.getFieldLabel($field)}: ${message}`);
            }
        },

        // Show field feedback
        showFeedback: function ($field, message, className = 'invalid-feedback') {
            let $feedback = $field.siblings(`.${className}`);

            if ($feedback.length === 0) {
                $feedback = $(`<div class="${className}"></div>`);
                $field.after($feedback);
            }

            $feedback.text(message).show();
        },

        // Hide field feedback
        hideFeedback: function ($field) {
            $field.siblings('.invalid-feedback, .valid-feedback').hide();
        },

        // Get field label for messages
        getFieldLabel: function ($field) {
            const label = $(`label[for="${$field.attr('id')}"]`).text() ||
                $field.attr('placeholder') ||
                $field.attr('name') ||
                'Field';
            return label.replace(':', '').trim();
        },

        // Loading state management
        setLoadingState: function ($button, isLoading, loadingText = 'Loading...') {
            if (isLoading) {
                const originalContent = $button.data('original-content') || $button.html();
                $button.data('original-content', originalContent);

                $button.prop('disabled', true)
                    .html(`<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>${loadingText}`);

                this.announceToScreenReader(`${loadingText}`);
            } else {
                const originalContent = $button.data('original-content');
                if (originalContent) {
                    $button.html(originalContent);
                }
                $button.prop('disabled', false);
            }
        },

        // Connection monitoring
        initConnectionMonitoring: function () {
            window.addEventListener('online', () => {
                this.handleConnectionChange(true);
            });

            window.addEventListener('offline', () => {
                this.handleConnectionChange(false);
            });
        },

        handleConnectionChange: function (isOnline) {
            if (isOnline) {
                showToast('Connection restored', 'success');
                $('form input[type="submit"], form button[type="submit"]').prop('disabled', false);
                this.announceToScreenReader('Internet connection restored');
            } else {
                showToast('Connection lost. Please check your internet connection.', 'warning');
                $('form input[type="submit"], form button[type="submit"]').prop('disabled', true);
                this.announceToScreenReader('Internet connection lost');
            }
        },

        // Browser compatibility checks
        initBrowserCompatibility: function () {
            // Check for required features
            const requiredFeatures = {
                localStorage: typeof Storage !== 'undefined',
                fetch: typeof fetch !== 'undefined',
                promises: typeof Promise !== 'undefined',
                arrow: (() => { try { eval('()=>{}'); return true; } catch (e) { return false; } })()
            };

            const missingFeatures = Object.entries(requiredFeatures)
                .filter(([feature, supported]) => !supported)
                .map(([feature]) => feature);

            if (missingFeatures.length > 0) {
                console.warn('Unsupported browser features:', missingFeatures);
                showToast('Your browser may not support all features. Please update your browser.', 'warning');
            }

            // Polyfills for older browsers
            this.addPolyfills();
        },

        // Add necessary polyfills
        addPolyfills: function () {
            // Polyfill for closest() method
            if (!Element.prototype.closest) {
                Element.prototype.closest = function (s) {
                    var el = this;
                    do {
                        if (el.matches(s)) return el;
                        el = el.parentElement || el.parentNode;
                    } while (el !== null && el.nodeType === 1);
                    return null;
                };
            }

            // Polyfill for matches() method
            if (!Element.prototype.matches) {
                Element.prototype.matches = Element.prototype.msMatchesSelector ||
                    Element.prototype.mozMatchesSelector;
            }
        },

        // Accessibility features
        initAccessibilityFeatures: function () {
            // Skip link functionality
            this.addSkipLinks();

            // Focus management
            this.initFocusManagement();

            // Keyboard navigation
            this.initKeyboardNavigation();

            // High contrast mode detection
            this.detectHighContrastMode();
        },

        // Add skip links for screen readers
        addSkipLinks: function () {
            if (!$('.skip-links').length) {
                const skipLinks = $(`
                    <div class="skip-links">
                        <a href="#main-content" class="visually-hidden-focusable">Skip to main content</a>
                        <a href="#navigation" class="visually-hidden-focusable">Skip to navigation</a>
                    </div>
                `);
                $('body').prepend(skipLinks);
            }
        },

        // Focus management
        initFocusManagement: function () {
            // Track focus for restoration
            $('input, button, select, textarea').on('focus', function () {
                AuthCommon.state.currentFocus = this;
            });

            // Focus first error field
            this.focusFirstError();
        },

        // Focus first field with error
        focusFirstError: function () {
            setTimeout(() => {
                const $firstError = $('.is-invalid').first();
                if ($firstError.length) {
                    $firstError.focus();
                    this.announceToScreenReader(`Please correct the error in ${this.getFieldLabel($firstError)}`);
                }
            }, AuthCommon.config.focusDelay);
        },

        // Keyboard navigation
        initKeyboardNavigation: function () {
            // Enable keyboard navigation for custom elements
            $('.btn-password-toggle').attr('tabindex', '0');

            // Add keyboard event handlers
            $(document).on('keydown', '.btn-password-toggle', function (e) {
                if (e.which === 13 || e.which === 32) { // Enter or Space
                    e.preventDefault();
                    $(this).click();
                }
            });
        },

        // Detect high contrast mode
        detectHighContrastMode: function () {
            const testDiv = $('<div>').css({
                'border': '1px solid',
                'border-color': 'red green',
                'position': 'absolute',
                'height': '5px',
                'top': '-999px',
                'background-image': 'url("data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7")'
            });

            $('body').append(testDiv);

            const cs = window.getComputedStyle(testDiv[0]);
            const highContrast = cs.backgroundImage === 'none' ||
                cs.borderTopColor === cs.borderRightColor;

            testDiv.remove();

            if (highContrast) {
                $('body').addClass('high-contrast');
                this.announceToScreenReader('High contrast mode detected');
            }
        },

        // Global keyboard shortcuts
        handleGlobalKeydown: function (e) {
            // Escape key to close modals/dropdowns
            if (e.which === 27) {
                $('.modal.show .btn-close').click();
                $('.dropdown-menu.show').removeClass('show');
            }

            // Ctrl+Enter to submit forms (accessibility feature)
            if (e.ctrlKey && e.which === 13) {
                const $form = $(e.target).closest('form');
                if ($form.length) {
                    e.preventDefault();
                    $form.find('button[type="submit"], input[type="submit"]').first().click();
                }
            }
        },

        // Handle browser navigation
        handleBrowserNavigation: function (e) {
            // Clear form submission state on navigation
            this.state.formSubmitting = false;

            // Clear any timeouts
            if (this.state.passwordHideTimeout) {
                clearTimeout(this.state.passwordHideTimeout);
            }
        },

        // Handle page visibility changes
        handleVisibilityChange: function () {
            if (document.visibilityState === 'hidden') {
                // Hide passwords when page becomes hidden
                $('input[type="text"][data-password="true"]').each(function () {
                    AuthCommon.hidePassword($(this));
                });
            }
        },

        // Error handling setup
        setupErrorHandling: function () {
            // Global error handler
            window.addEventListener('error', function (e) {
                console.error('Global error:', e.error);

                // Don't show user-facing errors in production
                if (window.location.hostname !== 'localhost' && !window.location.hostname.includes('127.0.0.1')) {
                    return;
                }

                showToast('An unexpected error occurred. Please refresh the page.', 'error');
            });

            // Handle unhandled promise rejections
            window.addEventListener('unhandledrejection', function (e) {
                console.error('Unhandled promise rejection:', e.reason);
                e.preventDefault(); // Prevent default browser behavior
            });

            // AJAX error handling
            $(document).ajaxError(function (event, jqXHR, ajaxSettings, thrownError) {
                console.error('AJAX error:', thrownError);

                if (jqXHR.status === 401) {
                    showToast('Session expired. Please sign in again.', 'warning');
                    setTimeout(() => {
                        window.location.href = '/Account/Login';
                    }, 2000);
                } else if (jqXHR.status >= 500) {
                    showToast('Server error. Please try again later.', 'error');
                } else if (jqXHR.status === 0) {
                    showToast('Network error. Please check your connection.', 'warning');
                }
            });
        },

        // Utility functions
        utils: {
            // Debounce function
            debounce: function (func, wait, immediate) {
                let timeout;
                return function () {
                    const context = this, args = arguments;
                    const later = function () {
                        timeout = null;
                        if (!immediate) func.apply(context, args);
                    };
                    const callNow = immediate && !timeout;
                    clearTimeout(timeout);
                    timeout = setTimeout(later, wait);
                    if (callNow) func.apply(context, args);
                };
            },

            // Throttle function
            throttle: function (func, limit) {
                let inThrottle;
                return function () {
                    const args = arguments;
                    const context = this;
                    if (!inThrottle) {
                        func.apply(context, args);
                        inThrottle = true;
                        setTimeout(() => inThrottle = false, limit);
                    }
                };
            },

            // Format phone number
            formatPhoneNumber: function (phoneNumber) {
                const cleaned = phoneNumber.replace(/\D/g, '');
                const match = cleaned.match(/^(\d{3})(\d{3})(\d{4})$/);
                if (match) {
                    return `(${match[1]}) ${match[2]}-${match[3]}`;
                }
                return phoneNumber;
            },

            // Validate email format
            isValidEmail: function (email) {
                const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                return re.test(email);
            },

            // Generate random ID
            generateId: function (prefix = 'auth') {
                return `${prefix}-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
            },

            // Copy text to clipboard
            copyToClipboard: function (text) {
                if (navigator.clipboard && window.isSecureContext) {
                    return navigator.clipboard.writeText(text);
                } else {
                    // Return a rejected promise for unsupported browsers
                    return Promise.reject(new Error('Clipboard API not supported in this browser'));
                }
            }
        }
    };

    // Expose to global scope
    window.AuthCommon = AuthCommon;

    // Auto-initialize when DOM is ready
    $(document).ready(function () {
        AuthCommon.init();
    });

    // Expose utilities globally for convenience
    window.debounce = AuthCommon.utils.debounce;
    window.throttle = AuthCommon.utils.throttle;

})(window, window.jQuery);