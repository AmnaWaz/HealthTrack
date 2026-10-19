/* HealthTrack - searchable multi-select with removable chips (Person A).
   Progressive enhancement of Views/Shared/_OptionPicker.cshtml:
   - the checkboxes stay the source of truth for what is selected;
   - on load they lose their "name" and hidden inputs are written instead, so the
     server receives the choices in the order the user picked them;
   - search filters the options, Enter selects a match or adds a custom option;
   - limits (max total, max new custom) and duplicates are checked here AND on the server.
   Exposes  element.htPicker.count()  for onboarding.js. No libraries needed. */
(function () {
    'use strict';

    // Same rule as Helpers/ProfileOptions.cs (2-50 characters, letters/numbers and & ' - / + . ( ) )
    var NAME_RE;
    try { NAME_RE = new RegExp("^[\\p{L}\\p{N}](?:[\\p{L}\\p{N} &'\\-/+.()]*[\\p{L}\\p{N})])?$", 'u'); }
    catch (e) { NAME_RE = /^[A-Za-z0-9](?:[A-Za-z0-9 &'\-\/+.()]*[A-Za-z0-9)])?$/; }

    function normalise(s) { return (s || '').replace(/\s+/g, ' ').trim(); }
    function key(s) { return normalise(s).toLowerCase(); }

    function init(root) {
        var field = root.getAttribute('data-field');
        var noun = root.getAttribute('data-noun') || 'option';
        var max = parseInt(root.getAttribute('data-max'), 10) || 15;
        var maxNew = parseInt(root.getAttribute('data-max-new'), 10) || 5;
        var grid = root.querySelector('[data-op-grid]');
        var chips = root.querySelector('[data-op-chips]');
        var hidden = root.querySelector('[data-op-hidden]');
        var search = root.querySelector('[data-op-search]');
        var addBtn = root.querySelector('[data-op-add]');
        var countEl = root.querySelector('[data-op-count]');
        var status = root.querySelector('[data-op-status]');
        var inlineErr = root.querySelector('[data-op-inline-error]');
        var noMatch = root.querySelector('[data-op-nomatch]');
        var serverErr = root.querySelector('[data-op-error]');

        root.querySelector('.op-enhanced').hidden = false;

        function boxes() { return Array.prototype.slice.call(grid.querySelectorAll('input.op-check')); }
        function boxFor(token) {
            var all = boxes();
            for (var i = 0; i < all.length; i++) if (all[i].value === token) return all[i];
            return null;
        }
        function boxByName(name) {
            var k = key(name), all = boxes();
            for (var i = 0; i < all.length; i++) if (key(all[i].getAttribute('data-name')) === k) return all[i];
            return null;
        }

        // ---- initial order: server order first, then anything else that is checked ----
        var order = [];
        var initial = [];
        try { initial = JSON.parse(root.getAttribute('data-selected') || '[]'); } catch (e) { initial = []; }
        initial.forEach(function (t) { var b = boxFor(t); if (b && b.checked && order.indexOf(t) < 0) order.push(t); });
        boxes().forEach(function (b) {
            if (b.checked && order.indexOf(b.value) < 0) order.push(b.value);
            b.removeAttribute('name');             // the hidden inputs are posted instead (keeps the order)
        });

        function say(msg) { status.textContent = ''; window.setTimeout(function () { status.textContent = msg; }, 30); }
        function showInlineError(msg) { inlineErr.textContent = msg; inlineErr.hidden = !msg; }
        function newCount() { return order.filter(function (t) { return t.indexOf('new:') === 0; }).length; }

        function render() {
            // hidden inputs, in picked order
            hidden.innerHTML = '';
            order.forEach(function (t) {
                var i = document.createElement('input');
                i.type = 'hidden'; i.name = field; i.value = t;
                hidden.appendChild(i);
            });

            // chips
            chips.innerHTML = '';
            order.forEach(function (t) {
                var b = boxFor(t); if (!b) return;
                var name = b.getAttribute('data-name');
                var li = document.createElement('li'); li.className = 'op-chip';
                var span = document.createElement('span'); span.className = 'op-chip-text'; span.textContent = name; // textContent: never HTML
                var x = document.createElement('button');
                x.type = 'button'; x.className = 'op-chip-remove'; x.setAttribute('aria-label', 'Remove ' + name);
                x.setAttribute('data-token', t); x.innerHTML = '&times;';
                li.appendChild(span); li.appendChild(x); chips.appendChild(li);
            });

            countEl.textContent = order.length;
            var full = order.length >= max;
            boxes().forEach(function (b) { b.disabled = full && !b.checked; });
            updateSearch();
            if (order.length > 0 && serverErr && !serverErr.hidden && root.classList.contains('op-has-error')) {
                root.classList.remove('op-has-error');
            }
        }

        function select(box, announce) {
            if (box.checked && order.indexOf(box.value) >= 0) return;
            if (order.length >= max) { showInlineError('You can choose up to ' + max + ' ' + noun + 's. Remove one to add another.'); return; }
            box.checked = true;
            order.push(box.value);
            showInlineError('');
            render();
            if (announce) say(box.getAttribute('data-name') + ' added. ' + order.length + ' of ' + max + ' selected.');
        }

        function unselect(box, announce) {
            box.checked = false;
            var i = order.indexOf(box.value);
            if (i >= 0) order.splice(i, 1);
            showInlineError('');
            render();
            if (announce) say(box.getAttribute('data-name') + ' removed. ' + order.length + ' of ' + max + ' selected.');
        }

        function addCustom(raw) {
            var name = normalise(raw);
            if (!name) return;
            var existing = boxByName(name);
            if (existing) {                                   // already an option: select it instead of duplicating
                if (!existing.checked) select(existing, true);
                else { showInlineError('\u201C' + existing.getAttribute('data-name') + '\u201D is already selected.'); }
                search.value = ''; updateSearch(); return;
            }
            if (name.length < 2 || name.length > 50) { showInlineError('Use 2 to 50 characters.'); return; }
            if (!NAME_RE.test(name)) { showInlineError("Use letters, numbers, spaces and & ' - / + . ( ) only."); return; }
            if (order.length >= max) { showInlineError('You can choose up to ' + max + ' ' + noun + 's.'); return; }
            if (newCount() >= maxNew) { showInlineError('You can add up to ' + maxNew + ' of your own ' + noun + 's at a time.'); return; }

            var label = document.createElement('label'); label.className = 'op-pill is-custom';
            var box = document.createElement('input');
            box.type = 'checkbox'; box.className = 'op-check'; box.value = 'new:' + name; box.setAttribute('data-name', name);
            var text = document.createElement('span'); text.className = 'op-pill-text'; text.textContent = name;
            var tag = document.createElement('span'); tag.className = 'op-tag'; tag.textContent = 'Custom';
            label.appendChild(box); label.appendChild(text); label.appendChild(tag);
            grid.insertBefore(label, noMatch);
            search.value = '';
            select(box, false);
            say(name + ' added as your own ' + noun + '. ' + order.length + ' of ' + max + ' selected.');
        }

        function updateSearch() {
            var q = key(search.value), shown = 0, exact = null;
            boxes().forEach(function (b) {
                var n = key(b.getAttribute('data-name'));
                var match = !q || n.indexOf(q) >= 0;
                b.parentNode.hidden = !match;
                if (match) shown++;
                if (q && n === q) exact = b;
            });
            noMatch.hidden = !(q && shown === 0);
            var canAdd = q && !exact;
            addBtn.hidden = !canAdd;
            if (canAdd) {
                addBtn.textContent = '+ Add \u201C' + normalise(search.value) + '\u201D';
                addBtn.disabled = order.length >= max || newCount() >= maxNew;
            }
        }

        // ---- events ----
        grid.addEventListener('change', function (e) {
            var b = e.target;
            if (!b.classList || !b.classList.contains('op-check')) return;
            if (b.checked) { b.checked = false; select(b, true); } else { unselect(b, true); }
        });

        chips.addEventListener('click', function (e) {
            var x = e.target.closest ? e.target.closest('.op-chip-remove') : null;
            if (!x) return;
            var all = Array.prototype.slice.call(chips.querySelectorAll('.op-chip-remove'));
            var idx = all.indexOf(x);
            var b = boxFor(x.getAttribute('data-token'));
            if (b) unselect(b, true);
            // keep keyboard focus somewhere sensible
            var left = chips.querySelectorAll('.op-chip-remove');
            if (left.length) left[Math.min(idx, left.length - 1)].focus(); else search.focus();
        });

        search.addEventListener('input', function () { showInlineError(''); updateSearch(); });

        search.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();                       // never submit the step from the search box
                var q = normalise(search.value);
                if (!q) return;
                var exact = boxByName(q);
                if (exact) {
                    if (!exact.checked) select(exact, true);
                    search.value = ''; updateSearch(); return;
                }
                var visible = boxes().filter(function (b) { return !b.parentNode.hidden; });
                if (visible.length === 1 && !visible[0].checked) { select(visible[0], true); search.value = ''; updateSearch(); return; }
                addCustom(q);
            } else if (e.key === 'Escape') {
                if (search.value) { e.preventDefault(); search.value = ''; showInlineError(''); updateSearch(); }
            } else if (e.key === 'Backspace' && !search.value && order.length) {
                var last = boxFor(order[order.length - 1]);
                if (last) unselect(last, true);
            }
        });

        addBtn.addEventListener('click', function () { addCustom(search.value); search.focus(); });

        root.htPicker = {
            count: function () { return order.length; },
            focus: function () { search.focus(); },
            min: parseInt(root.getAttribute('data-min'), 10) || 1,
            noun: noun
        };

        render();
    }

    document.querySelectorAll('[data-option-picker]').forEach(init);
})();
