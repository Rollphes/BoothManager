using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PuppeteerSharp;
using io.github.rollphes.boothManager.config;
using io.github.rollphes.boothManager.types.api;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO.Compression;
using System.Text;

namespace io.github.rollphes.boothManager.client {
    internal class Client {
        private static readonly string _browserPath = Path.Combine(ConfigLoader.RoamingDirectoryPath, "Browser");
        private static readonly string _packagesDirectoryPath = Path.Combine(ConfigLoader.RoamingDirectoryPath, "Packages");
        private static readonly string _userAgent = "\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36\"";

        private readonly HashSet<string> _itemIds = new();
        private readonly Dictionary<string, ItemInfo> _itemInfoList = new();

        internal Action AfterDeploy;
        internal bool IsDeployed { get; private set; } = false;
        internal bool IsLoggedIn { get; private set; } = false;
        internal string NickName { get; private set; }

        private IBrowser _browser = null;
        private readonly ConfigLoader _config;

        internal Client() {
            this._config = new ConfigLoader();
        }

        internal async Task Deploy() {
            this._config.Deploy();

            Debug.Log("Deploy Client");

            var browserFecher = new BrowserFetcher(new BrowserFetcherOptions { Path = _browserPath });
            var fetchedBrowser = await browserFecher.DownloadAsync();

            this._browser = await Puppeteer.LaunchAsync(new LaunchOptions {
                ExecutablePath = fetchedBrowser.GetExecutablePath(),
                HeadlessMode = HeadlessMode.True,
                Args = new[] {
                $"--user-agent={_userAgent}"
            }
            });
            ;
            await this.CheckAutoLogin();
            this.IsDeployed = true;
            this.AfterDeploy?.Invoke();
        }

        internal async Task Destroy() {
            if (this._browser != null) {
                await this._browser.CloseAsync();
            }

        }

        internal async Task SignIn(string email, string password) {
            this.ValidateBrowserInitialized();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
                throw new ArgumentException("Email and password are required.");
            }

            var page = await this._browser.NewPageAsync();
            await page.GoToAsync(this._config.GetEndpointUrl("auth", "signIn"));

            var emailInputSelector = this._config.GetSelector("auth", "emailInput");
            var passwordInputSelector = this._config.GetSelector("auth", "passwordInput");

            await page.WaitForSelectorAsync(emailInputSelector);
            await page.TypeAsync(emailInputSelector, email);
            await page.TypeAsync(passwordInputSelector, password);
            await page.Keyboard.PressAsync("Enter");

            try {
                await page.WaitForNavigationAsync(new NavigationOptions { Timeout = 10000 });
            } catch (Exception) {
                await page.CloseAsync();
                throw new Exception("Login Failed");
            }

            this.IsLoggedIn = true;
            this.NickName = await this.GetConfigElementPropertyAsync(page, "auth", "nickName", "innerText");
            var cookies = await page.GetCookiesAsync();
            this._config.SaveCookieParams(cookies);

            await page.CloseAsync();
            Debug.Log("Login Success");
        }

        internal void SignUp() {
            System.Diagnostics.Process.Start(this._config.GetEndpointUrl("auth", "signUp"));
        }

        internal void SignOut() {
            this._config.DeleteCookieParams();
            this.IsLoggedIn = false;
            this.NickName = null;
        }

        private async Task CheckAutoLogin() {
            var cookieParams = this._config.GetCookieParams();
            var page = await this._browser.NewPageAsync();

            if (cookieParams == null) {
                return;
            }

            await page.SetCookieAsync(cookieParams);
            await page.GoToAsync(this._config.GetEndpointUrl("home", "home"));

            this.IsLoggedIn = await page.EvaluateExpressionAsync<bool>(this._config.GetScript("auth", "checkLoggedIn"));
            if (this.IsLoggedIn) {
                this.NickName = await this.GetConfigElementPropertyAsync(page, "auth", "nickName", "innerText");
            }

            await page.CloseAsync();
        }

        /* This Test Method */
        internal async Task FetchItemIds() {
            this.ValidateBrowserInitialized();

            if (!this.IsLoggedIn) {
                throw new InvalidOperationException("You are not logged in.");
            }

            var page = await this._browser.NewPageAsync();
            await page.SetCookieAsync(this._config.GetCookieParams());

            try {
                for (int i = 1; i <= 999; i++) {
                    var urlParams = new Dictionary<string, string> { { "libraryPageNumber", i.ToString() } };
                    //TODO:ギフトページにしか表示されないアイテムもあるが、ギフトをもらったことがないため未実装
                    await page.GoToAsync(this._config.GetEndpointUrl("library", "library", urlParams));

                    var orders = await page.QuerySelectorAllAsync(this._config.GetSelector("library", "orders"));
                    if (orders.Length == 0)
                        break;

                    var orderTasks = orders.Select(async order => {
                        var itemUrlLink = await this.GetConfigElementPropertyAsync(order, "library", "itemLink", "href");

                        var itemId = itemUrlLink.Split("/").Last();
                        if (this._itemIds.Contains(itemId))
                            return;
                        this._itemIds.Add(itemId);
                    });

                    await Task.WhenAll(orderTasks);

                    if (orders.Length < 10)
                        break;
                }
            } finally {
                await page.CloseAsync();
            }
        }

        /* This Test Method */
        internal async Task FetchItemInfoList() {
            if (!this.IsLoggedIn) {
                throw new InvalidOperationException("You are not logged in.");
            }

            var httpClient = this.GetHttpClient();

            var tasks = new List<Task>();

            foreach (string itemId in this._itemIds) {
                tasks.Add(this.FetchItemInfoAsync(httpClient, itemId));
            }

            await Task.WhenAll(tasks);
        }

        private async Task FetchItemInfoAsync(HttpClient httpClient, string itemId) {
            var urlParams = new Dictionary<string, string> { { "lang", "ja" }, { "itemId", itemId } };
            string url = this._config.GetEndpointUrl("api", "itemInfo", urlParams);
            Uri uri = new(url);
            httpClient.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority));

            HttpResponseMessage response = await httpClient.GetAsync(uri.PathAndQuery);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            this._itemInfoList[itemId] = ItemInfo.FromJson(responseBody);
        }

        /* This Test Method */
        internal async Task DownloadAllFile() {
            this.ValidateBrowserInitialized();

            if (!this.IsLoggedIn) {
                throw new InvalidOperationException("You are not logged in.");
            }

            var page = await this._browser.NewPageAsync();
            await page.SetCookieAsync(this._config.GetCookieParams());

            try {
                foreach ((string itemId, ItemInfo itemInfo) in this._itemInfoList) {
                    var itemDirectoryPath = Path.Combine(_packagesDirectoryPath, itemId);
                    if (!Directory.Exists(itemDirectoryPath)) {
                        Directory.CreateDirectory(itemDirectoryPath);
                    }
                    foreach (Variation variation in itemInfo.Variations) {
                        if (variation.OrderUrl == null)
                            continue;
                        var variationDirectoryPath = Path.Combine(itemDirectoryPath, variation.Id.ToString());
                        if (!Directory.Exists(variationDirectoryPath)) {
                            Directory.CreateDirectory(variationDirectoryPath);
                        }

                        await page.GoToAsync(variation.OrderUrl.ToString());
                        var files = await page.QuerySelectorAllAsync(this._config.GetSelector("order", "files"));
                        foreach (var file in files) {
                            var fileUrlLink = await this.GetConfigElementPropertyAsync(file, "order", "fileLink", "href");

                            Uri fileLink = new (fileUrlLink);
                            string fileId = fileLink.Segments[^1];
                            string fileDirectoryPath = Path.Combine(variationDirectoryPath, fileId);

                            if (Directory.Exists(fileDirectoryPath))
                                continue;
                            Directory.CreateDirectory(fileDirectoryPath);

                            var httpClient = this.GetHttpClient();
                            await this.DownloadFileFromRedirectUrlAsync(httpClient, fileUrlLink, fileDirectoryPath);
                        }
                    }
                }
            } finally {
                await page.CloseAsync();
            }
        }

        private async Task DownloadFileFromRedirectUrlAsync(HttpClient httpClient, string url, string destinationDirectoryPath) {


            HttpResponseMessage response = await httpClient.GetAsync(url);
            Uri location = null;

            if (response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.MovedPermanently) {
                location = response.Headers.Location;
                response = await httpClient.GetAsync(location);
            }
            response.EnsureSuccessStatusCode();

            var zipFileName = Uri.UnescapeDataString(location.Segments[^1]);
            var zipFilePath = Path.Combine(destinationDirectoryPath, zipFileName);

            byte[] content = await response.Content.ReadAsByteArrayAsync();

            await File.WriteAllBytesAsync(zipFilePath, content);
            this.ExtractZipFile(zipFilePath, destinationDirectoryPath);
        }

        private void ExtractZipFile(string zipFilePath, string extractPath) {

            string pattern = @".zip$";
            if (!Regex.IsMatch(zipFilePath, pattern))
                return;

            if (!Directory.Exists(extractPath)) {
                Directory.CreateDirectory(extractPath);
            }
            ZipFile.ExtractToDirectory(zipFilePath, extractPath, Encoding.GetEncoding("shift_jis"));
            File.Delete(zipFilePath);
            
        }

        // utility methods
        private void ValidateBrowserInitialized() {
            if (this._browser == null) {
                throw new InvalidOperationException("Browser is not initialized.");
            }
        }

        private async Task<string> GetConfigElementPropertyAsync(IPage page, string section, string key, string property) {
            var selector = this._config.GetSelector(section, key);
            var element = await page.QuerySelectorAsync(selector);
            var value = (await element.GetPropertyAsync(property)).RemoteObject.Value.ToString();
            return value;
        }

        private async Task<string> GetConfigElementPropertyAsync(IElementHandle personElement, string section, string key, string property) {
            var selector = this._config.GetSelector(section, key);
            var element = await personElement.QuerySelectorAsync(selector);
            var value = (await element.GetPropertyAsync(property)).RemoteObject.Value.ToString();
            return value;
        }

        private HttpClient GetHttpClient() {
            var handler = new HttpClientHandler {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = false,
            };
            var cookies = this._config.GetCookieParams();
            foreach (var cookie in cookies) {
                handler.CookieContainer.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
            }

            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }
    }
}