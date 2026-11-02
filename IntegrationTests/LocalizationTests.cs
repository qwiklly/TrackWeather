using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace IntegrationTests
{
    public class LocalizationTests(TestAppFixture fixture) : IClassFixture<TestAppFixture>
    {
        private readonly TestApp _app = fixture.App;

        private static string ProjectDir => Path.Combine(FindSolutionDir(), "TrackWeatherWeb");

        private static string FindSolutionDir()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "TrackWeather.sln")))
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? throw new DirectoryNotFoundException("TrackWeather.sln");
        }

        private static Dictionary<string, string> LoadResources(string fileName) =>
            XDocument.Load(Path.Combine(ProjectDir, "Resources", fileName))
                .Root!.Elements("data")
                .ToDictionary(d => d.Attribute("name")!.Value, d => d.Element("value")!.Value);

        [Fact]
        public async Task DefaultLanguage_IsRussian()
        {
            var page = await _app.CreateBrowser().GetStringAsync("/");

            Assert.Contains("<html lang=\"ru\">", page);
            Assert.Contains("КартаПогоды", page);
        }

        [Fact]
        public async Task BrowserLanguage_IsRespected()
        {
            var page = await _app.CreateBrowser("en-US").GetStringAsync("/");

            Assert.Contains("<html lang=\"en\">", page);
            Assert.Contains("TrackWeatherMap", page);
            Assert.DoesNotContain("КартаПогоды", page);
        }

        [Fact]
        public async Task Switcher_SetsLanguage_AndReturnsToTheSamePage()
        {
            var browser = _app.CreateBrowser();

            var response = await browser.PostFormAsync("/language", new()
            {
                ["culture"] = "en",
                ["returnUrl"] = "/Privacy?x=1",
            }, tokenPage: "/");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Privacy?x=1", response.Headers.Location!.OriginalString);
            Assert.Contains("<html lang=\"en\">", await browser.GetStringAsync("/Privacy"));
        }

        [Fact]
        public async Task ChosenLanguage_WinsOverBrowserLanguage()
        {
            var browser = _app.CreateBrowser("en-US");

            await browser.PostFormAsync("/language", new() { ["culture"] = "ru", ["returnUrl"] = "/" }, tokenPage: "/");

            Assert.Contains("<html lang=\"ru\">", await browser.GetStringAsync("/"));
        }

        [Theory]
        [InlineData("https://evil.example.com/")]
        [InlineData("//evil.example.com/")]
        public async Task Switcher_DoesNotRedirectToAnotherSite(string returnUrl)
        {
            var response = await _app.CreateBrowser().PostFormAsync("/language", new()
            {
                ["culture"] = "en",
                ["returnUrl"] = returnUrl,
            }, tokenPage: "/");

            Assert.DoesNotContain("evil.example.com", response.Headers.Location!.OriginalString);
        }

        [Fact]
        public async Task Switcher_IgnoresUnsupportedLanguage()
        {
            var browser = _app.CreateBrowser();

            await browser.PostFormAsync("/language", new() { ["culture"] = "de", ["returnUrl"] = "/" }, tokenPage: "/");

            Assert.Contains("<html lang=\"ru\">", await browser.GetStringAsync("/"));
        }

        [Fact]
        public async Task Switcher_RequiresAntiforgeryToken()
        {
            var response = await _app.CreateBrowser().PostAsync("/language",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["culture"] = "en" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Layout_MarksTheCurrentLanguage_AndHasNoGoogleTranslate()
        {
            var page = await _app.CreateBrowser("en").GetStringAsync("/");

            Assert.DoesNotContain("translate.google", page);
            Assert.Matches(new Regex("value=\"en\"[^>]*is-active[^>]*aria-pressed=\"true\""), page);
        }

        [Theory]
        [InlineData("ru", "ru_RU")]
        [InlineData("en", "en_RU")]
        public async Task Map_LoadsYandexInTheCurrentLanguage(string language, string yandexLanguage)
        {
            var browser = await _app.SignInNewUserAsync();
            await browser.PostFormAsync("/language", new() { ["culture"] = language, ["returnUrl"] = "/" }, tokenPage: "/");

            var page = await browser.GetStringAsync("/UsersMap");

            Assert.Contains($"lang={yandexLanguage}", page);
        }

        [Fact]
        public async Task WeatherApi_FollowsTheChosenLanguage()
        {
            var browser = await _app.SignInNewUserAsync();
            await browser.PostFormAsync("/language", new() { ["culture"] = "en", ["returnUrl"] = "/" }, tokenPage: "/");

            await browser.GetAsync("/api/weather?lat=51.672&lon=39.184");

            Assert.Equal("en", _app.Weather.LastLanguage);
        }

        [Fact]
        public async Task ValidationMessages_AreTranslated()
        {
            var form = new Dictionary<string, string>
            {
                ["Register.Name"] = "",
                ["Register.Email"] = "not-an-email",
                ["Register.Password"] = "weak",
                ["Register.ConfirmPassword"] = "different",
            };

            var english = await (await _app.CreateBrowser("en").PostFormAsync("/Registration", new(form))).Content.ReadAsStringAsync();
            var russian = await (await _app.CreateBrowser("ru").PostFormAsync("/Registration", new(form))).Content.ReadAsStringAsync();

            Assert.Contains("Enter a valid email address", english);
            Assert.Contains("Passwords do not match", english);
            Assert.Contains("Введите корректный email", russian);
            Assert.Contains("Пароли не совпадают", russian);
        }

        [Fact]
        public void Resources_HaveTheSameKeysInBothLanguages()
        {
            var russian = LoadResources("SharedResource.resx");
            var english = LoadResources("SharedResource.en.resx");

            Assert.Empty(russian.Keys.Except(english.Keys));
            Assert.Empty(english.Keys.Except(russian.Keys));
            Assert.All(russian.Values.Concat(english.Values), value => Assert.False(string.IsNullOrWhiteSpace(value)));
        }

        [Fact]
        public void EveryKeyUsedInCode_ExistsInResources()
        {
            var known = LoadResources("SharedResource.resx").Keys.ToHashSet();
            var missing = new List<string>();

            void Scan(string pattern, string searchPattern, string prefix = "")
            {
                foreach (var file in Directory.EnumerateFiles(ProjectDir, searchPattern, SearchOption.AllDirectories))
                {
                    if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        || file.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}"))
                    {
                        continue;
                    }

                    foreach (Match match in Regex.Matches(File.ReadAllText(file), pattern))
                    {
                        var key = prefix + match.Groups[1].Value;
                        if (!known.Contains(key))
                        {
                            missing.Add($"{Path.GetFileName(file)}: {key}");
                        }
                    }
                }
            }

            Scan(@"\bL\[""([^""]+)""", "*.cshtml");
            Scan(@"\blocalizer\[""([^""]+)""", "*.cs");
            Scan(@"ErrorMessage = ""((?:Validation)\.[^""]+)""", "*.cs");
            Scan(@"Display\(Name = ""([^""]+)""", "*.cs");
            Scan(@"\bt\('([^']+)'\)", "*.js", "MapJs.");
            Scan(@"\bstrings\.([A-Z]\w+)", "transportLive.js", "LiveJs.");

            Assert.Empty(missing);
        }

        [Fact]
        public void EveryStatusAndHazard_HasATranslation()
        {
            var known = LoadResources("SharedResource.resx").Keys.ToHashSet();

            Assert.All(Enum.GetNames<TrackWeatherWeb.Models.TransportStatus>(), name => Assert.Contains($"Status.{name}", known));
            Assert.All(Enum.GetNames<TrackWeatherWeb.Services.Hazard>(), name => Assert.Contains($"Hazard.{name}", known));
        }

        [Fact]
        public void Pages_HaveNoHardcodedRussianText()
        {
            var offenders = Directory.EnumerateFiles(Path.Combine(ProjectDir, "Pages"), "*.cshtml", SearchOption.AllDirectories)
                .Where(file => !file.EndsWith("_LanguageSwitcher.cshtml"))
                .Where(file => Regex.IsMatch(File.ReadAllText(file), "[А-Яа-яЁё]"))
                .Select(Path.GetFileName);

            Assert.Empty(offenders);
        }
    }
}
