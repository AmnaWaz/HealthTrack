
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var shell = document.getElementById("htTrainerShell");
        var collapseButton = document.getElementById("htSidebarCollapse");
        var mobileButton = document.getElementById("htMobileMenu");
        var overlay = document.getElementById("htSidebarOverlay");

        if (!shell) {
            return;
        }

        function isMobile() {
            return window.matchMedia("(max-width: 767.98px)").matches;
        }

        function closeMobileSidebar() {
            shell.classList.remove("mobile-sidebar-open");

            if (mobileButton) {
                mobileButton.setAttribute("aria-expanded", "false");
            }
        }

        if (collapseButton) {
            collapseButton.addEventListener("click", function () {
                if (isMobile()) {
                    closeMobileSidebar();
                    return;
                }

                shell.classList.toggle("sidebar-collapsed");

                var collapsed =
                    shell.classList.contains("sidebar-collapsed");

                collapseButton.setAttribute(
                    "aria-label",
                    collapsed ? "Expand sidebar" : "Collapse sidebar"
                );

                collapseButton.setAttribute(
                    "title",
                    collapsed ? "Expand sidebar" : "Collapse sidebar"
                );
            });
        }

        if (mobileButton) {
            mobileButton.addEventListener("click", function () {
                var isOpen =
                    shell.classList.toggle("mobile-sidebar-open");

                mobileButton.setAttribute(
                    "aria-expanded",
                    isOpen ? "true" : "false"
                );
            });
        }

        if (overlay) {
            overlay.addEventListener("click", closeMobileSidebar);
        }

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeMobileSidebar();
            }
        });

        window.addEventListener("resize", function () {
            if (!isMobile()) {
                closeMobileSidebar();
            }
        });
    });
})();