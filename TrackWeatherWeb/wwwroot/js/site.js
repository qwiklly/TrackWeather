// Behaviour shared by all pages. Pages declare what they need with data attributes,
// so no user data ever ends up inside an inline event handler.
(function () {
    'use strict';

    // <button data-toggle-password> next to a password input
    document.addEventListener('click', function (event) {
        const button = event.target.closest('[data-toggle-password]');
        if (!button) {
            return;
        }
        const input = button.parentElement.querySelector('input');
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        button.querySelector('i').className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
    });

    document.addEventListener('submit', function (event) {
        const form = event.target;

        // <form data-confirm="Question?">
        if (form.dataset.confirm && !window.confirm(form.dataset.confirm)) {
            event.preventDefault();
            return;
        }

        // <form data-loading-form>: blocks double submits once client validation has passed
        if ('loadingForm' in form.dataset) {
            const isInvalid = window.jQuery && jQuery(form).valid && !jQuery(form).valid();
            if (!isInvalid) {
                const submit = form.querySelector('[type=submit]');
                submit.disabled = true;
                submit.insertAdjacentHTML('afterbegin', '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>');
            }
        }
    });

    // <time data-local-time datetime="...">: UTC on the server, the visitor's time zone on screen
    function localizeTimes(root) {
        root.querySelectorAll('time[data-local-time]').forEach(function (element) {
            const date = new Date(element.dateTime);
            if (!isNaN(date)) {
                element.textContent = date.toLocaleString(document.documentElement.lang);
            }
        });
    }

    localizeTimes(document);
    window.TrackWeatherSite = { localizeTimes };
})();
