function googleTranslateElementInit() {
    new google.translate.TranslateElement(
        { pageLanguage: 'ru', includedLanguages: 'en,ru', autoDisplay: false },
        'google_translate_element'
    );
}

function translatePage() {
    var googleTranslate = document.querySelector('.goog-te-combo');
    if (googleTranslate) {
        googleTranslate.value = 'en';
        googleTranslate.dispatchEvent(new Event('change'));
    }
}