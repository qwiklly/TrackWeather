namespace TrackWeatherWeb
{
    public static class SupportedLanguages
    {
        public const string Russian = "ru";
        public const string English = "en";
        public const string Default = Russian;

        public static readonly IReadOnlyList<string> All = [Russian, English];

        public static bool IsSupported(string? language) =>
            language is not null && All.Contains(language, StringComparer.OrdinalIgnoreCase);

        // Language of the current request, reduced to a value we support
        public static string Current =>
            Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == English ? English : Russian;
    }
}
