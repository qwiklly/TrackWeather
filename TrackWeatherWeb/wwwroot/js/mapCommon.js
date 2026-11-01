// Helpers shared by the map pages.
window.TrackWeatherMap = (function () {
    'use strict';

    const container = document.getElementById('map');
    const strings = JSON.parse(document.getElementById('map-strings').textContent);
    const statusBox = document.getElementById('map-status');
    const statusText = document.getElementById('map-status-text');
    const clearButton = document.getElementById('map-status-clear');
    const language = container.dataset.language;

    function t(key) {
        return strings[key] ?? key;
    }

    // Balloon content is rendered as HTML, so everything that comes from data goes through here
    function escapeHtml(value) {
        const replacements = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
        return String(value ?? '').replace(/[&<>"']/g, character => replacements[character]);
    }

    function formatCoords(coords) {
        return coords.map(value => Number(value).toFixed(4)).join(', ');
    }

    function showStatus(message, onClear) {
        statusText.textContent = message;
        statusBox.hidden = !message;
        clearButton.hidden = !onClear;
        clearButton.onclick = onClear ?? null;
    }

    function showFatal(message) {
        const alert = document.createElement('div');
        alert.className = 'alert alert-warning';
        alert.setAttribute('role', 'alert');
        alert.textContent = message;
        container.replaceWith(alert);
    }

    // Runs the callback once the Yandex API is usable, otherwise explains what went wrong
    function ready(callback) {
        if (container.dataset.hasKey !== 'true') {
            showFatal(t('MissingKey'));
        } else if (!window.ymaps) {
            showFatal(t('MapUnavailable'));
        } else {
            ymaps.ready(callback, () => showFatal(t('MapUnavailable')));
        }
    }

    return { t, escapeHtml, formatCoords, showStatus, ready, language };
})();
