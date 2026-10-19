/* HealthTrack - onboarding page behaviour (Person A).
   Client-side checks are only for convenience; OnboardingController validates everything again.
   - focus: error summary if there is one, otherwise the step heading
   - field-level messages on Continue (Back skips validation)
   - bio character counter, double-submit guard, countdown on the success page */
(function () {
    'use strict';

    // ---------- focus on load ----------
    var summary = document.querySelector('[data-ob-errors]');
    var heading = document.querySelector('[data-ob-heading]');
    try {
        if (summary) summary.focus();
        else if (heading) heading.focus({ preventScroll: true });
    } catch (e) { /* older browsers: ignore */ }

    // server-side errors -> aria-invalid so screen readers announce them on the field
    Array.prototype.forEach.call(document.querySelectorAll('.input-validation-error'), function (el) {
        el.setAttribute('aria-invalid', 'true');
    });

    // ---------- bio counter ----------
    Array.prototype.forEach.call(document.querySelectorAll('[data-ob-counter]'), function (area) {
        var box = document.getElementById(area.getAttribute('data-ob-counter'));
        if (!box) return;
        var out = box.querySelector('[data-ob-count]');
        var max = parseInt(area.getAttribute('maxlength'), 10) || 500;
        function update() {
            out.textContent = area.value.length;
            box.classList.toggle('is-warn', area.value.length > max - 40);
        }
        area.addEventListener('input', update);
        update();
    });

    // ---------- validation ----------
    var form = document.querySelector('[data-ob-form]');
    if (form) {
        var submitter = null;
        form.addEventListener('click', function (e) {
            var b = e.target.closest ? e.target.closest('button[type=submit]') : null;
            if (b) submitter = b;
        });

        function messageSpan(name) { return form.querySelector('[data-ob-error="' + name + '"]'); }

        function setError(name, input, msg) {
            var span = messageSpan(name);
            if (span) {
                span.textContent = msg || '';
                span.className = msg ? 'field-validation-error' : 'field-validation-valid';
            }
            if (input) {
                if (msg) input.setAttribute('aria-invalid', 'true'); else input.removeAttribute('aria-invalid');
            }
        }

        function checkInput(input) {
            var name = input.name, v = (input.value || '').trim(), msg = '';
            if (input.hasAttribute('required') && !v) msg = input.getAttribute('data-msg-required') || 'This field is required.';
            else if (v && input.type === 'number') {
                var n = Number(v), min = Number(input.min), max = Number(input.max);
                if (isNaN(n) || n < min || n > max) msg = input.getAttribute('data-msg-range') || 'Enter a value between ' + min + ' and ' + max + '.';
            }
            else if (v && input.type === 'date') {
                if ((input.min && v < input.min) || (input.max && v > input.max)) msg = input.getAttribute('data-msg-range') || 'Check this date.';
            }
            else if (v && input.hasAttribute('minlength') && v.length < parseInt(input.getAttribute('minlength'), 10)) {
                msg = input.getAttribute('data-msg-minlength') || 'Too short.';
            }
            setError(name, input, msg);
            return msg ? input : null;
        }

        function validate() {
            var firstBad = null;

            Array.prototype.forEach.call(form.querySelectorAll('input[required]:not([type=radio]), textarea[required]'), function (input) {
                var bad = checkInput(input);
                if (bad && !firstBad) firstBad = bad;
            });

            Array.prototype.forEach.call(form.querySelectorAll('[data-ob-radio-group]'), function (group) {
                var name = group.getAttribute('data-ob-radio-group');
                var checked = group.querySelector('input[name="' + name + '"]:checked');
                var msg = checked ? '' : (group.getAttribute('data-msg-required') || 'Choose an option.');
                setError(name, null, msg);
                group.classList.toggle('is-invalid', !!msg);
                if (msg && !firstBad) firstBad = group.querySelector('input[type=radio]');
            });

            Array.prototype.forEach.call(form.querySelectorAll('[data-option-picker]'), function (picker) {
                if (!picker.htPicker) return;          // script not loaded: server validates
                var err = picker.querySelector('[data-op-error]');
                var ok = picker.htPicker.count() >= picker.htPicker.min;
                if (err) {
                    err.hidden = ok;
                    err.textContent = ok ? '' : 'Choose at least ' + picker.htPicker.min + ' ' + picker.htPicker.noun + '.';
                }
                picker.classList.toggle('op-has-error', !ok);
                if (!ok && !firstBad) firstBad = { focus: function () { picker.htPicker.focus(); } };
            });

            return firstBad;
        }

        // re-check a field when the user leaves it (only once they have tried to continue)
        var attempted = false;
        form.addEventListener('focusout', function (e) {
            if (!attempted) return;
            var t = e.target;
            if (t.matches && t.matches('input[required]:not([type=radio]), textarea[required]')) checkInput(t);
        });

        form.addEventListener('submit', function (e) {
            var goingBack = submitter && submitter.value === 'back';
            if (!goingBack) {
                attempted = true;
                var bad = validate();
                if (bad) {
                    e.preventDefault();
                    bad.focus();
                    submitter = null;
                    return;
                }
            }
            // double-submit guard (the server is idempotent anyway)
            if (form.getAttribute('aria-busy') === 'true') { e.preventDefault(); return; }
            // (buttons are not disabled, only made unclickable by CSS, so the clicked Nav value is still posted)
            form.setAttribute('aria-busy', 'true');
        });
    }

    // ---------- success page countdown ----------
    var cd = document.querySelector('[data-ob-countdown]');
    if (cd) {
        var url = cd.getAttribute('data-url');
        var left = parseInt(cd.getAttribute('data-seconds'), 10) || 6;
        var text = cd.querySelector('[data-ob-countdown-text]');
        var stop = cd.querySelector('[data-ob-countdown-stop]');
        var timer = window.setInterval(function () {
            left--;
            if (left <= 0) { window.clearInterval(timer); window.location.href = url; return; }
            if (left <= 3) text.textContent = 'Taking you there in ' + left + '…';
        }, 1000);
        stop.addEventListener('click', function () {
            window.clearInterval(timer);
            text.textContent = 'Automatic redirect stopped.';
            stop.hidden = true;
        });
    }
})();
