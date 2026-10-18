/* HealthTrack trainer sidebar: collapse (remembered), tablet auto-collapse, mobile slide-out menu. */
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

        function match(q) { return window.matchMedia(q).matches; }
        function isMobile() { return match("(max-width: 767.98px)"); }
        function isTablet() { return match("(min-width: 768px) and (max-width: 1099.98px)"); }

        var saved = null;
        try { saved = localStorage.getItem(KEY); } catch (e) { }

        function labelButton(collapsed) {
            if (!collapseButton) { return; }
            var text = collapsed ? "Expand sidebar" : "Collapse sidebar";
            collapseButton.setAttribute("aria-label", text);
            collapseButton.setAttribute("title", text);
        }

        function setCollapsed(collapsed) {
            shell.classList.toggle("sidebar-collapsed", collapsed);
            labelButton(collapsed);
        }

        // default: icons-only on tablets, full on desktop, unless the user chose
        function applyDefault() {
            if (isMobile()) { return; }
            if (saved !== null) { setCollapsed(saved === "1"); }
            else { setCollapsed(isTablet()); }
        }

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
                if (isMobile()) { closeMobile(true); return; }
                var collapsed = !shell.classList.contains("sidebar-collapsed");
                setCollapsed(collapsed);
                saved = collapsed ? "1" : "0";
                try { localStorage.setItem(KEY, saved); } catch (e) { }
            });
        }

        if (mobileButton) {
            mobileButton.addEventListener("click", function () {
                if (shell.classList.contains("mobile-sidebar-open")) { closeMobile(false); }
                else { openMobile(); }
            });
        }

        if (overlay) { overlay.addEventListener("click", function () { closeMobile(true); }); }

        if (sidebar) {
            sidebar.addEventListener("click", function (e) {
                var link = e.target.closest ? e.target.closest("a") : null;
                if (link && isMobile()) { closeMobile(false); }
            });
        }

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") { closeMobile(true); }
        });

        var resizeTimer = null;
        window.addEventListener("resize", function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                if (!isMobile()) { closeMobile(false); }
                applyDefault();
            }, 120);
        });

        applyDefault();
    });
})();
