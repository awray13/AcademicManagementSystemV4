/*
 * Password Strength Utilities
 * Comprehensive password strength calculation and validation
 * File: /wwwroot/js/shared/password-strength.js
 */

(function (window) {
    'use strict';

    // Namespace for password utilities
    window.PasswordStrength = window.PasswordStrength || {};

    const PasswordStrength = {

        // Password strength levels
        levels: {
            NONE: { value: 0, name: 'none', text: 'No password', color: 'secondary' },
            WEAK: { value: 1, name: 'weak', text: 'Weak ❌', color: 'danger' },
            FAIR: { value: 2, name: 'fair', text: 'Fair ⚠️', color: 'warning' },
            GOOD: { value: 3, name: 'good', text: 'Good ✓', color: 'info' },
            STRONG: { value: 4, name: 'strong', text: 'Strong 💪', color: 'success' }
        },

        // Password requirements configuration
        requirements: {
            minLength: 8,
            maxLength: 100,
            requireLowercase: true,
            requireUppercase: true,
            requireNumbers: true,
            requireSpecialChars: true,
            forbidRepeating: true,
            forbidCommonPatterns: true
        },

        // Common weak patterns to avoid
        weakPatterns: [
            /123/i, /abc/i, /qwe/i, /asd/i, /zxc/i,
            /password/i, /admin/i, /user/i, /login/i,
            /welcome/i, /hello/i, /test/i, /sample/i
        ],

        // Calculate comprehensive password strength
        calculate: function (password, options = {}) {
            const config = { ...this.requirements, ...options };

            if (!password || typeof password !== 'string') {
                return {
                    score: 0,
                    level: this.levels.NONE,
                    suggestions: ['Password is required'],
                    requirements: this.checkRequirements('', config)
                };
            }

            let score = 0;
            const suggestions = [];
            const penalties = [];

            // Length scoring (0-30 points)
            const lengthScore = this.calculateLengthScore(password, config);
            score += lengthScore.score;
            suggestions.push(...lengthScore.suggestions);

            // Character variety scoring (0-40 points)
            const varietyScore = this.calculateVarietyScore(password, config);
            score += varietyScore.score;
            suggestions.push(...varietyScore.suggestions);

            // Complexity bonus (0-20 points)
            const complexityScore = this.calculateComplexityScore(password, config);
            score += complexityScore.score;
            suggestions.push(...complexityScore.suggestions);

            // Security penalties (up to -10 points)
            const securityScore = this.calculateSecurityPenalties(password, config);
            score += securityScore.score; // This will be negative
            penalties.push(...securityScore.penalties);

            // Ensure score is within bounds
            score = Math.max(0, Math.min(100, score));

            // Determine strength level
            const level = this.getStrengthLevel(score);

            // Combine suggestions and penalties
            const allSuggestions = [...suggestions, ...penalties].filter(s => s);

            // If password is strong, show positive feedback
            if (level.value >= this.levels.STRONG.value && allSuggestions.length === 0) {
                allSuggestions.push('Excellent password strength! 🔒');
            }

            return {
                score: Math.round(score),
                level: level,
                suggestions: allSuggestions,
                requirements: this.checkRequirements(password, config),
                details: {
                    length: lengthScore,
                    variety: varietyScore,
                    complexity: complexityScore,
                    security: securityScore
                }
            };
        },

        // Calculate length-based score
        calculateLengthScore: function (password, config) {
            const length = password.length;
            const suggestions = [];
            let score = 0;

            if (length < config.minLength) {
                suggestions.push(`Use at least ${config.minLength} characters`);
            } else {
                // Score based on length
                score += Math.min(25, length * 2);

                if (length >= 12) {
                    score += 5; // Bonus for longer passwords
                } else if (length < 10) {
                    suggestions.push('Consider using 12+ characters for better security');
                }
            }

            if (length > config.maxLength) {
                suggestions.push(`Password cannot exceed ${config.maxLength} characters`);
                score = 0; // Invalid if too long
            }

            return { score, suggestions };
        },

        // Calculate character variety score
        calculateVarietyScore: function (password, config) {
            const suggestions = [];
            let score = 0;

            // Lowercase letters (0-10 points)
            if (/[a-z]/.test(password)) {
                score += 10;
            } else if (config.requireLowercase) {
                suggestions.push('Include lowercase letters (a-z)');
            }

            // Uppercase letters (0-10 points)
            if (/[A-Z]/.test(password)) {
                score += 10;
            } else if (config.requireUppercase) {
                suggestions.push('Include uppercase letters (A-Z)');
            }

            // Numbers (0-10 points)
            if (/\d/.test(password)) {
                score += 10;
            } else if (config.requireNumbers) {
                suggestions.push('Include numbers (0-9)');
            }

            // Special characters (0-10 points)
            if (/[^\da-zA-Z]/.test(password)) {
                score += 10;
            } else if (config.requireSpecialChars) {
                suggestions.push('Include special characters (!@#$%^&*)');
            }

            return { score, suggestions };
        },

        // Calculate complexity bonus score
        calculateComplexityScore: function (password, config) {
            const suggestions = [];
            let score = 0;

            // Bonus for length >= 12
            if (password.length >= 12) {
                score += 5;
            }

            // Bonus for diverse special characters
            if (/[!@#$%^&*()_+=\[\]{}|;:,.<>?]/.test(password)) {
                score += 5;
            }

            // Bonus for mixed case throughout the password
            if (/^(?=.*[a-z])(?=.*[A-Z]).+$/.test(password) &&
                password !== password.toLowerCase() &&
                password !== password.toUpperCase()) {
                score += 5;
            }

            // Bonus for good entropy (not just numbers or letters)
            if (!/^\d+$/.test(password) && !/^[a-zA-Z]+$/.test(password)) {
                score += 5;
            }

            return { score, suggestions };
        },

        // Calculate security penalties
        calculateSecurityPenalties: function (password, config) {
            const penalties = [];
            let score = 0;

            // Penalty for repeating characters
            if (config.forbidRepeating && /(.)\1{2,}/.test(password)) {
                penalties.push('Avoid repeating the same character multiple times');
                score -= 5;
            }

            // Penalty for common patterns
            if (config.forbidCommonPatterns) {
                for (const pattern of this.weakPatterns) {
                    if (pattern.test(password)) {
                        penalties.push('Avoid common words and patterns');
                        score -= 3;
                        break; // Only penalize once for patterns
                    }
                }
            }

            // Penalty for sequential characters
            if (/(?:abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz|123|234|345|456|567|678|789|890)/i.test(password)) {
                penalties.push('Avoid sequential characters');
                score -= 3;
            }

            // Penalty for keyboard patterns
            if (/(?:qwer|wert|erty|rtyu|tyui|yuio|uiop|asdf|sdfg|dfgh|fghj|ghjk|hjkl|zxcv|xcvb|cvbn|vbnm)/i.test(password)) {
                penalties.push('Avoid keyboard patterns');
                score -= 3;
            }

            return { score, penalties };
        },

        // Check individual requirements
        checkRequirements: function (password, config) {
            return {
                minLength: password.length >= config.minLength,
                maxLength: password.length <= config.maxLength,
                hasLowercase: /[a-z]/.test(password),
                hasUppercase: /[A-Z]/.test(password),
                hasNumbers: /\d/.test(password),
                hasSpecialChars: /[^\da-zA-Z]/.test(password),
                noRepeating: !config.forbidRepeating || !/(.)\1{2,}/.test(password),
                noCommonPatterns: !config.forbidCommonPatterns || !this.weakPatterns.some(pattern => pattern.test(password))
            };
        },

        // Determine strength level from score
        getStrengthLevel: function (score) {
            if (score >= 85) return this.levels.STRONG;
            if (score >= 65) return this.levels.GOOD;
            if (score >= 40) return this.levels.FAIR;
            if (score > 0) return this.levels.WEAK;
            return this.levels.NONE;
        },

        // Check if password contains personal information
        containsPersonalInfo: function (password, personalInfo = {}) {
            if (!password) return false;

            const passwordLower = password.toLowerCase();
            const checks = [];

            // Check email username
            if (personalInfo.email) {
                const emailUsername = personalInfo.email.split('@')[0].toLowerCase();
                if (emailUsername.length >= 3 && passwordLower.includes(emailUsername)) {
                    checks.push('email username');
                }
            }

            // Check first name
            if (personalInfo.firstName && personalInfo.firstName.length >= 3) {
                if (passwordLower.includes(personalInfo.firstName.toLowerCase())) {
                    checks.push('first name');
                }
            }

            // Check last name
            if (personalInfo.lastName && personalInfo.lastName.length >= 3) {
                if (passwordLower.includes(personalInfo.lastName.toLowerCase())) {
                    checks.push('last name');
                }
            }

            // Check birth year
            if (personalInfo.birthYear) {
                if (passwordLower.includes(personalInfo.birthYear.toString())) {
                    checks.push('birth year');
                }
            }

            return {
                contains: checks.length > 0,
                info: checks
            };
        },

        // Validate password against another password (for confirmation)
        validateConfirmation: function (password, confirmPassword) {
            if (!password && !confirmPassword) {
                return { isValid: true, message: '' };
            }

            if (!confirmPassword) {
                return { isValid: false, message: 'Please confirm your password' };
            }

            if (password !== confirmPassword) {
                return { isValid: false, message: 'Passwords do not match' };
            }

            return { isValid: true, message: 'Passwords match' };
        },

        // Generate a strong password suggestion
        generateSuggestion: function (length = 12) {
            const lowercase = 'abcdefghijklmnopqrstuvwxyz';
            const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
            const numbers = '0123456789';
            const specials = '!@#$%^&*()_+-=[]{}|;:,.<>?';

            let password = '';
            const allChars = lowercase + uppercase + numbers + specials;

            // Ensure at least one character from each category
            password += lowercase.charAt(Math.floor(Math.random() * lowercase.length));
            password += uppercase.charAt(Math.floor(Math.random() * uppercase.length));
            password += numbers.charAt(Math.floor(Math.random() * numbers.length));
            password += specials.charAt(Math.floor(Math.random() * specials.length));

            // Fill the rest randomly
            for (let i = password.length; i < length; i++) {
                password += allChars.charAt(Math.floor(Math.random() * allChars.length));
            }

            // Shuffle the password
            return password.split('').sort(() => Math.random() - 0.5).join('');
        },

        // Real-time password input handler
        createInputHandler: function (inputSelector, options = {}) {
            const config = {
                strengthSelector: options.strengthSelector || inputSelector + '-strength',
                suggestionsSelector: options.suggestionsSelector || inputSelector + '-suggestions',
                requirementsSelector: options.requirementsSelector || inputSelector + '-requirements',
                debounceMs: options.debounceMs || 300,
                personalInfo: options.personalInfo || {},
                ...options
            };

            let debounceTimer;

            const updateStrength = () => {
                const input = document.querySelector(inputSelector);
                if (!input) return;

                const password = input.value;
                const result = this.calculate(password, config);
                const personalCheck = this.containsPersonalInfo(password, config.personalInfo);

                // Update strength display
                this.updateStrengthDisplay(result, config);

                // Update suggestions
                this.updateSuggestions(result, personalCheck, config);

                // Update requirements checklist
                this.updateRequirements(result.requirements, config);

                // Trigger custom event
                input.dispatchEvent(new CustomEvent('passwordStrengthUpdate', {
                    detail: { result, personalCheck, config }
                }));
            };

            // Attach event listener
            const input = document.querySelector(inputSelector);
            if (input) {
                input.addEventListener('input', () => {
                    clearTimeout(debounceTimer);
                    debounceTimer = setTimeout(updateStrength, config.debounceMs);
                });

                // Initial calculation
                updateStrength();
            }

            return updateStrength;
        },

        // Update strength display elements
        updateStrengthDisplay: function (result, config) {
            const strengthElement = document.querySelector(config.strengthSelector);
            if (!strengthElement) return;

            // Update progress bar
            const progressBar = strengthElement.querySelector('.progress-bar');
            if (progressBar) {
                progressBar.style.width = `${result.score}%`;
                progressBar.className = `progress-bar bg-${result.level.color}`;
            }

            // Update text
            const textElement = strengthElement.querySelector('.strength-text');
            if (textElement) {
                textElement.textContent = result.level.text;
                textElement.className = `strength-text text-${result.level.color}`;
            }

            // Update score
            const scoreElement = strengthElement.querySelector('.strength-score');
            if (scoreElement) {
                scoreElement.textContent = `${result.score}/100`;
                scoreElement.className = `strength-score text-${result.level.color}`;
            }
        },

        // Update suggestions display
        updateSuggestions: function (result, personalCheck, config) {
            const suggestionsElement = document.querySelector(config.suggestionsSelector);
            if (!suggestionsElement) return;

            let suggestions = [...result.suggestions];

            // Add personal info warning
            if (personalCheck.contains) {
                suggestions.unshift(`Avoid using your ${personalCheck.info.join(', ')} in your password`);
            }

            if (suggestions.length === 0) {
                suggestionsElement.style.display = 'none';
                return;
            }

            suggestionsElement.style.display = 'block';
            suggestionsElement.innerHTML = suggestions
                .map(suggestion => `<small class="text-muted d-block"><i class="fas fa-lightbulb me-1"></i>${suggestion}</small>`)
                .join('');
        },

        // Update requirements checklist
        updateRequirements: function (requirements, config) {
            const requirementsElement = document.querySelector(config.requirementsSelector);
            if (!requirementsElement) return;

            const reqMap = {
                'req-length': requirements.minLength,
                'req-lowercase': requirements.hasLowercase,
                'req-uppercase': requirements.hasUppercase,
                'req-numbers': requirements.hasNumbers,
                'req-special': requirements.hasSpecialChars,
                'req-no-repeat': requirements.noRepeating,
                'req-no-patterns': requirements.noCommonPatterns
            };

            Object.entries(reqMap).forEach(([id, isValid]) => {
                const element = requirementsElement.querySelector(`#${id}`);
                if (element) {
                    if (isValid) {
                        element.className = 'fas fa-check text-success';
                    } else {
                        element.className = 'fas fa-times text-danger';
                    }
                }
            });
        }
    };

    // Expose to global scope
    window.PasswordStrength = PasswordStrength;

})(window);