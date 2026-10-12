document.addEventListener('DOMContentLoaded', function () {

  // آیکن‌های Lucide
  if (window.lucide && typeof window.lucide.createIcons === 'function') {
    window.lucide.createIcons();
  }

  // منوی موبایل (Hamburger)
  var menuBtn = document.getElementById('mobile-menu-button');
  var menu = document.getElementById('mobile-menu');
  var openIcon = document.getElementById('menu-open-icon');
  var closeIcon = document.getElementById('menu-close-icon');

  if (menuBtn && menu) {
    menuBtn.addEventListener('click', function () {
      var isOpen = !menu.classList.contains('hidden');
      menu.classList.toggle('hidden', isOpen);
      menuBtn.setAttribute('aria-expanded', String(!isOpen));
      if (openIcon && closeIcon) {
        openIcon.classList.toggle('hidden', !isOpen);
        closeIcon.classList.toggle('hidden', isOpen);
      }
    });
  }

  // فیلتر موبایل (Drawer/Bottom Sheet)
  var filterOpenBtn = document.getElementById('mobile-filter-button');
  var filterDrawer = document.getElementById('mobile-filter-drawer');
  var filterBackdrop = document.getElementById('mobile-filter-backdrop');
  var filterCloseBtn = document.getElementById('mobile-filter-close');

  function openFilterDrawer() {
    if (!filterDrawer || !filterBackdrop) return;
    filterDrawer.classList.remove('translate-y-full');
    filterBackdrop.classList.remove('hidden');
    document.body.classList.add('overflow-hidden');
  }
  function closeFilterDrawer() {
    if (!filterDrawer || !filterBackdrop) return;
    filterDrawer.classList.add('translate-y-full');
    filterBackdrop.classList.add('hidden');
    document.body.classList.remove('overflow-hidden');
  }
  if (filterOpenBtn) filterOpenBtn.addEventListener('click', openFilterDrawer);
  if (filterCloseBtn) filterCloseBtn.addEventListener('click', closeFilterDrawer);
  if (filterBackdrop) filterBackdrop.addEventListener('click', closeFilterDrawer);

  // فقط اجازه ورود رقم در باکس جستجوی شماره
  var searchInputs = document.querySelectorAll('[data-numeric-search]');
  searchInputs.forEach(function (input) {
    input.addEventListener('input', function () {
      input.value = input.value.replace(/[^\d]/g, '');
    });
  });

  // FAQ آکاردئون
  document.querySelectorAll('[data-faq-trigger]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var panel = document.getElementById(btn.getAttribute('aria-controls'));
      var expanded = btn.getAttribute('aria-expanded') === 'true';
      btn.setAttribute('aria-expanded', String(!expanded));
      var icon = btn.querySelector('[data-faq-icon]');
      if (panel) panel.classList.toggle('hidden', expanded);
      if (icon) icon.classList.toggle('rotate-45', !expanded);
    });
  });

  // سایه سربرگ هنگام اسکرول
  var header = document.querySelector('header.sticky');
  if (header) {
    var onScroll = function () {
      header.classList.toggle('shadow-softer', window.scrollY > 4);
    };
    document.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
  }
});
