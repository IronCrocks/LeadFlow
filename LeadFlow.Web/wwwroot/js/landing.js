(() => {
  const toggle = document.querySelector('.menu-toggle');
  const mobileNav = document.querySelector('#mobile-nav');
  if (toggle && mobileNav) {
    const closeMenu = () => { mobileNav.hidden = true; toggle.setAttribute('aria-expanded','false'); document.body.classList.remove('menu-open'); };
    toggle.addEventListener('click', () => { const opening = mobileNav.hidden; mobileNav.hidden = !opening; toggle.setAttribute('aria-expanded',String(opening)); document.body.classList.toggle('menu-open',opening); });
    mobileNav.querySelectorAll('a').forEach(a => a.addEventListener('click',closeMenu));
    window.addEventListener('keydown', e => { if (e.key === 'Escape') closeMenu(); });
  }
  const form = document.querySelector('.lead-form');
  if (!form) return;
  const isDemo = document.body.dataset.demo === 'true';
  const button = form.querySelector('[type="submit"]');
  if (isDemo) {
    form.addEventListener('submit', e => {
      e.preventDefault();
      if (!form.reportValidity()) return;
      const status = form.querySelector('.demo-message');
      status.hidden = false;
      status.textContent = 'Демонстрация: форма заполнена. Для реальной отправки подключите ASP.NET Core endpoint POST /leads.';
      status.scrollIntoView({behavior:'smooth',block:'nearest'});
    });
  } else {
    form.addEventListener('submit', e => {
      if (button && !button.disabled) {
        // Do not disable synchronously: disabled submit buttons may be excluded from native submission in some environments.
        button.setAttribute('aria-busy','true');
      }
    });
    const invalid = document.querySelector('.field-error:not(:empty)');
    if (invalid) {
      const control = invalid.closest('.field')?.querySelector('input,select,textarea');
      if (control) { requestAnimationFrame(() => { control.scrollIntoView({block:'center'});control.focus({preventScroll:true}); }); }
    }
  }
})();
