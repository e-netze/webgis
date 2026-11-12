// Subscriber login / account pages: password visibility toggle, caps lock hint, submit protection, copy to clipboard
(function () {
    'use strict';

    var eyeSvg = '<svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true" focusable="false" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7S1 12 1 12z"/><circle cx="12" cy="12" r="3"/></svg>';
    var eyeOffSvg = '<svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true" focusable="false" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 19c-7 0-11-7-11-7a18.45 18.45 0 0 1 5.06-5.94"/><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 7 11 7a18.5 18.5 0 0 1-2.16 3.19"/><path d="M14.12 14.12a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>';

    function initPasswordToggles() {
        document.querySelectorAll('.webgis-auth-password').forEach(function (wrapper) {
            var input = wrapper.querySelector('input');
            if (!input || wrapper.querySelector('.webgis-auth-password-toggle')) {
                return;
            }

            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'webgis-auth-password-toggle';
            button.setAttribute('aria-label', wrapper.getAttribute('data-toggle-label') || 'Show password');
            button.setAttribute('title', wrapper.getAttribute('data-toggle-label') || 'Show password');
            button.setAttribute('aria-pressed', 'false');
            button.setAttribute('aria-controls', input.id);
            button.innerHTML = eyeSvg;

            button.addEventListener('click', function () {
                var show = input.type === 'password';
                input.type = show ? 'text' : 'password';
                button.setAttribute('aria-pressed', show ? 'true' : 'false');
                button.innerHTML = show ? eyeOffSvg : eyeSvg;
                input.focus();
            });

            wrapper.appendChild(button);
        });
    }

    function initCapsLockHints() {
        document.querySelectorAll('input[data-capslock-hint]').forEach(function (input) {
            var hint = document.getElementById(input.getAttribute('data-capslock-hint'));
            if (!hint) {
                return;
            }
            var handler = function (e) {
                if (e.getModifierState) {
                    hint.style.display = e.getModifierState('CapsLock') ? '' : 'none';
                }
            };
            input.addEventListener('keydown', handler);
            input.addEventListener('keyup', handler);
            input.addEventListener('blur', function () { hint.style.display = 'none'; });
        });
    }

    function initForms() {
        document.querySelectorAll('form.webgis-auth-form').forEach(function (form) {
            form.addEventListener('submit', function () {
                // never submit a visible (plain text) password field state to the browser history/autofill
                form.querySelectorAll('.webgis-auth-password input').forEach(function (input) {
                    input.type = 'password';
                });

                var submit = form.querySelector('button[type=submit]');
                if (submit) {
                    // disable after the current event loop, otherwise the button value would not be submitted
                    setTimeout(function () {
                        submit.disabled = true;
                        if (submit.getAttribute('data-submitting')) {
                            submit.textContent = submit.getAttribute('data-submitting');
                        }
                    }, 0);
                }
            });
        });
    }

    function initCopyButtons() {
        document.querySelectorAll('[data-copy-target]').forEach(function (button) {
            button.addEventListener('click', function () {
                var target = document.getElementById(button.getAttribute('data-copy-target'));
                if (!target || !navigator.clipboard) {
                    return;
                }
                var text = Array.prototype.map.call(target.querySelectorAll('li'), function (li) { return li.textContent.trim(); }).join('\n')
                    || target.textContent.trim();

                navigator.clipboard.writeText(text).then(function () {
                    var original = button.getAttribute('data-original-text') || button.textContent;
                    button.setAttribute('data-original-text', original);
                    button.textContent = '\u2714 ' + original;
                    setTimeout(function () { button.textContent = original; }, 2000);
                });
            });
        });
    }

    function initMatchValidation() {
        document.querySelectorAll('input[data-match]').forEach(function (input) {
            var other = document.getElementById(input.getAttribute('data-match'));
            if (!other) {
                return;
            }
            var validate = function () {
                input.setCustomValidity(input.value && input.value !== other.value
                    ? (input.getAttribute('data-match-message') || 'Values do not match')
                    : '');
            };
            input.addEventListener('input', validate);
            other.addEventListener('input', validate);
        });
    }

    function init() {
        initPasswordToggles();
        initCapsLockHints();
        initMatchValidation();
        initForms();
        initCopyButtons();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
