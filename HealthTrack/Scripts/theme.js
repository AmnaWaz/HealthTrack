/* HealthTrack theme switcher. Loaded on every page via Views/Shared/_ThemeScripts.cshtml.
   Any element with the attribute  data-theme-toggle  becomes a light/dark switch.
   From your own JS:  HealthTheme.set('light' | 'dark'),  HealthTheme.toggle(),  HealthTheme.get()  */
(function () {
  var KEY = 'ht-theme';
  var root = document.documentElement;

  function apply(t) {
    root.setAttribute('data-theme', t);
    root.setAttribute('data-bs-theme', t);  
  }
  function get() { return root.getAttribute('data-theme') === 'light' ? 'light' : 'dark'; }
  function set(t) {
    try { localStorage.setItem(KEY, t); } catch (e) { }
    apply(t);
    document.dispatchEvent(new CustomEvent('ht-theme-change', { detail: t }));
  }
  function toggle() { set(get() === 'dark' ? 'light' : 'dark'); }

  document.addEventListener('click', function (e) {
    var b = e.target.closest ? e.target.closest('[data-theme-toggle]') : null;
    if (b) { e.preventDefault(); toggle(); }
  });

  window.HealthTheme = { set: set, get: get, toggle: toggle };
})();
