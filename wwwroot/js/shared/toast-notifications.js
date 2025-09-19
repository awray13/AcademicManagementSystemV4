/*
 * Toast Notifications Utility
 * Reusable toast notification system for authentication pages
 * File: /wwwroot/js/shared/toast-notifications.js
 */

(function (window, $) {
    'use strict';

    // Namespace for toast notifications
    window.ToastNotifications = window.ToastNotifications || {};

    const ToastNotifications = {

        // Default configuration
        defaults: {
            delay: 5000,
            animation: true,
            autohide: true,
            position: 'bottom-end' // top-start, top-end, bottom-start, bottom-end
        },

        // Toast types with their configurations
        types: {
            success: {
                icon: 'fas fa-check-circle',
                bgClass: 'text-bg-success',
                iconColor: 'text-white'
            },
            error: {
                icon: 'fas fa-exclamation-circle',
                bgClass: 'text-bg-danger',
                iconColor: 'text-white'
            },
            warning: {
                icon: 'fas fa-exclamation-triangle',
                bgClass: 'text-bg-warning',
                iconColor: 'text-dark'
            },
            info: {
                icon: 'fas fa-info-circle',
                bgClass: 'text-bg-info',
                iconColor: 'text-dark'
            },
            loading: {
                icon: 'fas fa-spinner fa-spin',
                bgClass: 'text-bg-primary',
                iconColor: 'text-white'
            }
        },

        // Initialize toast container if it doesn't exist
        initContainer: function (position = this.defaults.position) {
            let container = document.querySelector('.toast-container');

            if (!container) {
                container = document.createElement('div');
                container.className = `toast-container position-fixed ${this.getPositionClasses(position)} p-3`;
                container.style.zIndex = '1055';
                document.body.appendChild(container);
            }

            return container;
        },

        // Get Bootstrap classes for positioning
        getPositionClasses: function (position) {
            const positions = {
                'top-start': 'top-0 start-0',
                'top-end': 'top-0 end-0',
                'bottom-start': 'bottom-0 start-0',
                'bottom-end': 'bottom-0 end-0'
            };

            return positions[position] || positions['bottom-end'];
        },

        // Create toast element
        createToastElement: function (message, type, options = {}) {
            const config = { ...this.defaults, ...options };
            const typeConfig = this.types[type] || this.types.info;
            const toastId = `toast-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;

            const toastHtml = `
                <div id="${toastId}" 
                     class="toast ${typeConfig.bgClass} border-0" 
                     role="alert" 
                     aria-live="assertive" 
                     aria-atomic="true"
                     data-bs-delay="${config.delay}"
                     data-bs-animation="${config.animation}"
                     data-bs-autohide="${config.autohide}">
                    <div class="d-flex align-items-center">
                        <div class="toast-body d-flex align-items-center">
                            <i class="${typeConfig.icon} ${typeConfig.iconColor} me-2" aria-hidden="true"></i>
                            <span class="flex-grow-1">${this.sanitizeMessage(message)}</span>
                        </div>
                        <button type="button" 
                                class="btn-close ${typeConfig.iconColor === 'text-white' ? 'btn-close-white' : ''} me-2" 
                                data-bs-dismiss="toast" 
                                aria-label="Close">
                        </button>
                    </div>
                </div>
            `;

            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = toastHtml;
            return tempDiv.firstElementChild;
        },

        // Sanitize message to prevent XSS
        sanitizeMessage: function (message) {
            const div = document.createElement('div');
            div.textContent = message;
            return div.innerHTML;
        },

        // Show toast notification
        show: function (message, type = 'info', options = {}) {
            try {
                const container = this.initContainer(options.position);
                const toastElement = this.createToastElement(message, type, options);

                // Add toast to container
                container.appendChild(toastElement);

                // Initialize Bootstrap toast
                const bsToast = new bootstrap.Toast(toastElement, {
                    delay: options.delay || this.defaults.delay,
                    animation: options.animation !== false,
                    autohide: options.autohide !== false
                });

                // Show the toast
                bsToast.show();

                // Auto-remove from DOM after hiding
                toastElement.addEventListener('hidden.bs.toast', function () {
                    if (toastElement.parentNode) {
                        toastElement.parentNode.removeChild(toastElement);
                    }
                });

                // Return toast instance for external control
                return {
                    element: toastElement,
                    toast: bsToast,
                    hide: () => bsToast.hide(),
                    dispose: () => bsToast.dispose()
                };

            } catch (error) {
                console.error('Error showing toast notification:', error);
                // Fallback to alert if toast fails
                alert(`${type.toUpperCase()}: ${message}`);
                return null;
            }
        },

        // Convenience methods for different toast types
        success: function (message, options = {}) {
            return this.show(message, 'success', options);
        },

        error: function (message, options = {}) {
            return this.show(message, 'error', { ...options, delay: 8000 });
        },

        warning: function (message, options = {}) {
            return this.show(message, 'warning', { ...options, delay: 6000 });
        },

        info: function (message, options = {}) {
            return this.show(message, 'info', options);
        },

        loading: function (message, options = {}) {
            return this.show(message, 'loading', {
                ...options,
                autohide: false // Loading toasts should not auto-hide
            });
        },

        // Clear all toasts
        clearAll: function () {
            const container = document.querySelector('.toast-container');
            if (container) {
                const toasts = container.querySelectorAll('.toast');
                toasts.forEach(toast => {
                    const bsToast = bootstrap.Toast.getInstance(toast);
                    if (bsToast) {
                        bsToast.hide();
                    }
                });
            }
        },

        // Show multiple toasts with delay between them
        showQueue: function (toasts, delayBetween = 500) {
            toasts.forEach((toast, index) => {
                setTimeout(() => {
                    this.show(toast.message, toast.type, toast.options);
                }, index * delayBetween);
            });
        },

        // Show toast with action button
        showWithAction: function (message, actionText, actionCallback, type = 'info', options = {}) {
            const toastId = `toast-${Date.now()}`;
            const typeConfig = this.types[type] || this.types.info;

            const toastHtml = `
                <div id="${toastId}" 
                     class="toast ${typeConfig.bgClass} border-0" 
                     role="alert" 
                     aria-live="assertive" 
                     aria-atomic="true"
                     data-bs-autohide="false">
                    <div class="toast-body">
                        <div class="d-flex align-items-center mb-2">
                            <i class="${typeConfig.icon} ${typeConfig.iconColor} me-2"></i>
                            <span>${this.sanitizeMessage(message)}</span>
                        </div>
                        <div class="d-flex justify-content-end">
                            <button type="button" 
                                    class="btn btn-sm btn-outline-secondary me-2" 
                                    data-bs-dismiss="toast">
                                Dismiss
                            </button>
                            <button type="button" 
                                    class="btn btn-sm ${type === 'success' ? 'btn-success' : type === 'error' ? 'btn-danger' : 'btn-primary'}" 
                                    data-action="confirm">
                                ${actionText}
                            </button>
                        </div>
                    </div>
                </div>
            `;

            const container = this.initContainer(options.position);
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = toastHtml;
            const toastElement = tempDiv.firstElementChild;

            container.appendChild(toastElement);

            // Add action button event listener
            const actionBtn = toastElement.querySelector('[data-action="confirm"]');
            actionBtn.addEventListener('click', function () {
                if (typeof actionCallback === 'function') {
                    actionCallback();
                }
                bootstrap.Toast.getInstance(toastElement).hide();
            });

            const bsToast = new bootstrap.Toast(toastElement, {
                animation: options.animation !== false,
                autohide: options.autohide !== false
            });

            bsToast.show();

            return {
                element: toastElement,
                toast: bsToast,
                hide: () => bsToast.hide(),
                dispose: () => bsToast.dispose()
            };
        }
    };

    // Expose ToastNotifications to the global object
    window.ToastNotifications = ToastNotifications;

})(window, jQuery);