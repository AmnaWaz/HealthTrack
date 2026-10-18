/* HealthTrack trainer sidebar
   - collapse / expand ONLY from the collapse button (state is remembered between pages)
   - icons-only by default on tablets, mobile slide-out menu with scroll lock
   - cogwheel / avatar opens the Profile + Settings menu */
(function () {
    "use strict";

    var KEY = "ht-sidebar-collapsed";

    document.addEventListener("DOMContentLoaded", function () {
        var shell = document.getElementById("htTrainerShell");
        if (!shell) { return; }

        var collapseButton = document.getElementById("htSidebarCollapse");
        var mobileButton = document.getElementById("htMobileMenu");
        var overlay = document.getElementById("htSidebarOverlay");
        var sidebar = document.getElementById("htSidebar");
        var menu = document.getElementById("htProfileMenu");
        var anchor = sidebar ? sidebar.querySelector(".ht-sidebar-profile") : null;
        var toggles = document.querySelectorAll("[data-profile-toggle]");

        function match(q) { return window.matchMedia(q).matches; }
        function isMobile() { return match("(max-width: 767.98px)"); }
        function isTablet() { return match("(min-width: 768px) and (max-width: 1099.98px)"); }

        var saved = null;
        try { saved = localStorage.getItem(KEY); } catch (e) { }

        function labelButton(collapsed) {
            if (!collapseButton) { return; }
            var text = collapsed ? "Open sidebar" : "Collapse sidebar";
            collapseButton.setAttribute("aria-label", text);
            collapseButton.setAttribute("title", text);
        }
        function setCollapsed(collapsed) {
            shell.classList.toggle("sidebar-collapsed", collapsed);
            labelButton(collapsed);
        }
        function applyDefault() {
            if (isMobile()) { return; }
            if (saved !== null) { setCollapsed(saved === "1"); }
            else { setCollapsed(isTablet()); }
        }

        /* ---------- profile / settings menu ---------- */
        function placeMenu() {
            if (!menu || !anchor) { return; }
            var r = anchor.getBoundingClientRect();
            var collapsed = shell.classList.contains("sidebar-collapsed") && !isMobile();
            if (collapsed) {
                menu.style.left = (sidebar.getBoundingClientRect().right + 10) + "px";
                menu.style.bottom = Math.max(10, window.innerHeight - r.bottom) + "px";
                menu.style.width = "250px";
            } else {
                menu.style.left = (r.left + 8) + "px";
                menu.style.bottom = (window.innerHeight - r.top + 8) + "px";
                menu.style.width = Math.max(220, r.width - 16) + "px";
            }
        }
        function setToggles(open) {
            for (var i = 0; i < toggles.length; i++) { toggles[i].setAttribute("aria-expanded", open ? "true" : "false"); }
        }
        function openMenu() { if (!menu) { return; } placeMenu(); menu.hidden = false; setToggles(true); }
        function closeMenu() { if (!menu || menu.hidden) { return; } menu.hidden = true; setToggles(false); }

        for (var t = 0; t < toggles.length; t++) {
            toggles[t].addEventListener("click", function (e) {
                e.stopPropagation();
                if (menu.hidden) { openMenu(); } else { closeMenu(); }
            });
        }
        document.addEventListener("click", function (e) {
            if (menu && !menu.hidden && !menu.contains(e.target)) { closeMenu(); }
        });

        /* ---------- mobile slide-out ---------- */
        function openMobile() {
            shell.classList.add("mobile-sidebar-open");
            document.body.classList.add("ht-no-scroll");
            if (mobileButton) { mobileButton.setAttribute("aria-expanded", "true"); }
            var first = sidebar ? sidebar.querySelector(".ht-side-link") : null;
            if (first) { first.focus(); }
        }
        function closeMobile(returnFocus) {
            if (!shell.classList.contains("mobile-sidebar-open")) { return; }
            shell.classList.remove("mobile-sidebar-open");
            document.body.classList.remove("ht-no-scroll");
            if (mobileButton) {
                mobileButton.setAttribute("aria-expanded", "false");
                if (returnFocus) { mobileButton.focus(); }
            }
        }

        if (collapseButton) {
            collapseButton.addEventListener("click", function () {
                closeMenu();
                if (isMobile()) { closeMobile(true); return; }
                var collapsed = !shell.classList.contains("sidebar-collapsed");
                setCollapsed(collapsed);
                saved = collapsed ? "1" : "0";
                try { localStorage.setItem(KEY, saved); } catch (e) { }
            });
        }
        if (mobileButton) {
            mobileButton.addEventListener("click", function () {
                closeMenu();
                if (shell.classList.contains("mobile-sidebar-open")) { closeMobile(false); } else { openMobile(); }
            });
        }
        if (overlay) { overlay.addEventListener("click", function () { closeMenu(); closeMobile(true); }); }

        if (sidebar) {
            sidebar.addEventListener("click", function (e) {
                var link = e.target.closest ? e.target.closest("a.ht-side-link") : null;
                if (link && isMobile()) { closeMobile(false); }   // links never change the collapsed state on desktop
            });
        }
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") { closeMenu(); closeMobile(true); }
        });

        var resizeTimer = null;
        window.addEventListener("resize", function () {
            closeMenu();
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                if (!isMobile()) { closeMobile(false); }
                applyDefault();
            }, 120);
        });

        applyDefault();
    });
})();
