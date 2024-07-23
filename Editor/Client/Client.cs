using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using io.github.rollphes.boothManager.config;
using io.github.rollphes.boothManager.types.api;

using PuppeteerSharp;

using UnityEditor;

namespace io.github.rollphes.boothManager.client {
    internal enum DeployStatusType {
        BrowserDownloading,
        BrowserActivating,
        AutoLoginInProgress,
        Complete
    }

    internal enum FetchItemInfoStatusType {
        ItemIdFetchingInLibrary,
        ItemIdFetchingInGift,
        ItemInfoFetching
    }

    internal class Client {
        private static readonly string _browserPath = Path.Combine(ConfigLoader.RoamingDirectoryPath, "Browser");
        private static readonly string _packagesDirectoryPath = Path.Combine(ConfigLoader.RoamingDirectoryPath, "Packages");
        private static readonly string _userAgent = "\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36\"";
        private static readonly string _fileLinkPattern = @"(?<=<a class=""nav-reverse"" href="")https://booth.pm/downloadables/.*?(?="">)";
        private static readonly string _sevenZipExePath = "Packages/io.github.rollphes.booth-manager/Runtime/7-Zip/7z.exe";

        internal Action<DeployStatusType> onDeployProgressing;
        internal bool IsDeployed { get; private set; } = false;
        internal bool IsLoggedIn { get; private set; } = false;
        internal string NickName { get; private set; }

        private ItemInfo[] _itemInfos = null;
        private IBrowser _browser = null;
        private readonly ConfigLoader _config;

        internal Client() {
            this._config = new ConfigLoader();
        }

        internal async Task Deploy() {
            this._config.Deploy();

            this.onDeployProgressing?.Invoke(DeployStatusType.BrowserDownloading);
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = _browserPath });
            var fetchedBrowser = await browserFetcher.DownloadAsync();

            this.onDeployProgressing?.Invoke(DeployStatusType.BrowserActivating);
            this._browser = await Puppeteer.LaunchAsync(new LaunchOptions {
                ExecutablePath = fetchedBrowser.GetExecutablePath(),
                HeadlessMode = HeadlessMode.True,
                Args = new[] {
                    $"--user-agent={_userAgent}"
                }
            });

            this.onDeployProgressing?.Invoke(DeployStatusType.AutoLoginInProgress);
            await this.CheckAutoLogin();

            this.onDeployProgressing?.Invoke(DeployStatusType.Complete);
            this.IsDeployed = true;
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
                await page.WaitForNavigationAsync(new NavigationOptions { Timeout = 10 * 1000 });
            } catch (Exception) {
                await page.CloseAsync();
                throw new Exception("Login Failed");
            }

            this.IsLoggedIn = true;
            this.NickName = await this.GetConfigElementPropertyAsync(page, "auth", "nickName", "innerText");
            var cookies = await page.GetCookiesAsync();
            this._config.SaveCookieParams(cookies);

            await page.CloseAsync();
        }

        internal void SignUp() {
            Process.Start(this._config.GetEndpointUrl("auth", "signUp"));
        }

        internal void SignOut() {
            this._config.DeleteCookieParams();
            this.IsLoggedIn = false;
            this.NickName = null;
        }

        private async Task CheckAutoLogin() {
            var cookieParams = this._config.GetCookieParams();
            var page = await this._browser.NewPageAsync();
            if (cookieParams != null) {
                await page.SetCookieAsync(cookieParams);
                await page.GoToAsync(this._config.GetEndpointUrl("home", "home"));

                this.IsLoggedIn = await page.EvaluateExpressionAsync<bool>(this._config.GetScript("auth", "checkLoggedIn"));
                if (this.IsLoggedIn) {
                    this.NickName = await this.GetConfigElementPropertyAsync(page, "auth", "nickName", "innerText");
                }

                await page.CloseAsync();
            }
        }

        private async Task<List<string>> FetchItemIds(Action<FetchItemInfoStatusType, int, int> onProgressing) {
            this.ValidateBrowserInitialized();
            if (!this.IsLoggedIn) {
                throw new InvalidOperationException("You are not logged in.");
            }

            var page = await this._browser.NewPageAsync();
            await page.SetCookieAsync(this._config.GetCookieParams());

            var itemIds = new List<string>();
            var pageTypes = new string[] { "library", "gift" };
            var lastPageCounts = new Dictionary<string, int>();

            foreach (var pageType in pageTypes) {
                var urlParams = new Dictionary<string, string> { { "pageNumber", "1" } };
                try {
                    await page.GoToAsync(this._config.GetEndpointUrl("library", pageType, urlParams));
                    var lastPageUrlLink = await this.GetConfigElementPropertyAsync(page, "library", "lastPageLink", "href");
                    lastPageCounts[pageType] = int.Parse(Regex.Match(lastPageUrlLink, @"(?<=\?page=).*?(?=$|&)").ToString());
                } catch {
                    lastPageCounts[pageType] = 0;
                }
            }

            foreach (var pageType in pageTypes) {
                for (var i = 1; i <= lastPageCounts[pageType]; i++) {
                    var urlParams = new Dictionary<string, string> { { "pageNumber", i.ToString() } };
                    await page.GoToAsync(this._config.GetEndpointUrl("library", pageType, urlParams));

                    var orders = await page.QuerySelectorAllAsync(this._config.GetSelector("library", "orders"));
                    if (orders.Length == 0) {
                        break;
                    }

                    var orderTasks = orders.Select(async order => {
                        var itemUrlLink = await this.GetConfigElementPropertyAsync(order, "library", "itemLink", "href");
                        var itemId = itemUrlLink.Split("/").Last();
                        if (!itemIds.Contains(itemId)) {
                            itemIds.Add(itemId);
                        }
                    });
                    await Task.WhenAll(orderTasks);

                    onProgressing?.Invoke(pageType == "library" ? FetchItemInfoStatusType.ItemIdFetchingInLibrary : FetchItemInfoStatusType.ItemIdFetchingInGift, i, lastPageCounts[pageType]);

                    if (orders.Length < 10) {
                        break;
                    }
                }
            }

            await page.CloseAsync();

            return itemIds;
        }

        internal async Task<ItemInfo[]> FetchItemInfos(bool force = false, Action<FetchItemInfoStatusType, int, int> onProgressing = null) {
            if (this._itemInfos == null || force) {
                var itemIds = await this.FetchItemIds(onProgressing);
                if (!this.IsLoggedIn) {
                    throw new InvalidOperationException("You are not logged in.");
                }

                var httpClient = this.GetHttpClient();
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var taskCompletedCount = 0;

                var tasks = itemIds.Select(async (itemId) => {
                    var itemInfo = await this.FetchItemInfoAsync(httpClient, itemId);
                    taskCompletedCount++;
                    onProgressing?.Invoke(FetchItemInfoStatusType.ItemInfoFetching, taskCompletedCount, itemIds.Count);
                    return itemInfo;
                });
                var itemInfos = await Task.WhenAll(tasks);

                this._itemInfos = itemInfos;
                return itemInfos;
            } else {
                return this._itemInfos;
            }
        }

        private async Task<ItemInfo> FetchItemInfoAsync(HttpClient httpClient, string itemId) {
            var urlParams = new Dictionary<string, string> { { "lang", "ja" }, { "itemId", itemId } };
            var url = this._config.GetEndpointUrl("api", "itemInfo", urlParams);
            var uri = new Uri(url);
            httpClient.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority));

            var response = await httpClient.GetAsync(uri.PathAndQuery);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return ItemInfo.FromJson(responseBody);
        }

        /* This Test Method */
        //internal async Task DownloadAllFile() {
        //    Debug.Log("Start DownloadAllFile");
        //    this.ValidateBrowserInitialized();

        //    throw new Exception("Forbidden method.");

        //    if (!this.IsLoggedIn) {
        //        throw new InvalidOperationException("You are not logged in.");
        //    }

        //    HttpClient httpClient = this.GetHttpClient();

        //    var itemInfoTasks = this._itemInfoList.Select(async (itemInfoCurrent) => {

        //        string itemId = itemInfoCurrent.Key;
        //        ItemInfo itemInfo = itemInfoCurrent.Value;
        //        var itemDirectoryPath = Path.Combine(_packagesDirectoryPath, itemId);
        //        if (!Directory.Exists(itemDirectoryPath)) {
        //            Directory.CreateDirectory(itemDirectoryPath);
        //        }
        //        var variationTasks = itemInfo.Variations.Select(async (variation) => {
        //            if (variation.OrderUrl == null)
        //                return;
        //            var variationDirectoryPath = Path.Combine(itemDirectoryPath, variation.Id.ToString());
        //            if (!Directory.Exists(variationDirectoryPath)) {
        //                Directory.CreateDirectory(variationDirectoryPath);
        //            }
        //            string html = await httpClient.GetStringAsync(variation.OrderUrl.ToString()); // Puppeteer has a glitch with too many pages open at once.
        //            var LinkMatch = Regex.Matches(html, _fileLinkPattern, RegexOptions.IgnoreCase);
        //            var fileTasks = LinkMatch.Select(async (link) => {
        //                var fileUrlLink = link.ToString();
        //                var fileUri = new Uri(fileUrlLink);
        //                string fileId = fileUri.Segments[^1];
        //                string fileDirectoryPath = Path.Combine(variationDirectoryPath, fileId);

        //                if (Directory.Exists(fileDirectoryPath))
        //                    return;
        //                Directory.CreateDirectory(fileDirectoryPath);
        //                try {
        //                    await this.DownloadFileFromRedirectUrlAsync(httpClient, fileUrlLink, fileDirectoryPath);
        //                } catch (TaskCanceledException) {
        //                    Debug.LogWarning($"File download canceled for {fileUrlLink}");
        //                    Directory.Delete(fileDirectoryPath, true );
        //                } catch (Exception error) {
        //                    Debug.LogError(error);
        //                    Directory.Delete(fileDirectoryPath, true );
        //                }
        //            });
        //            await Task.WhenAll(fileTasks);
        //        });
        //        await Task.WhenAll(variationTasks);
        //    });
        //    await Task.WhenAll(itemInfoTasks);
        //}

        /* This Test Method */
        private async Task DownloadFileFromRedirectUrlAsync(HttpClient httpClient, string url, string destinationDirectoryPath) {
            var response = await httpClient.GetAsync(url);
            Uri location = null;

            if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently) {
                location = response.Headers.Location;
                var uriBuilder = new UriBuilder(location) {
                    Query = string.Empty
                };

                var urlWithoutQuery = uriBuilder.ToString();
                var redirectHttpClient = new HttpClient();
                response = await redirectHttpClient.GetAsync(urlWithoutQuery);
            }
            response.EnsureSuccessStatusCode();

            var zipFileName = Uri.UnescapeDataString(location.Segments[^1]);
            var zipFilePath = Path.Combine(destinationDirectoryPath, zipFileName);

            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(zipFilePath, content);

            this.ExtractZipFile(zipFilePath, destinationDirectoryPath);
        }

        private void ExtractZipFile(string zipFilePath, string extractPath) {
            try {
                if (!zipFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
                    throw new ArgumentException("The specified file is not a zip file.");
                }

                var processInfo = new ProcessStartInfo() {
                    FileName = _sevenZipExePath,
                    Arguments = $"x \"{zipFilePath}\" -o\"{extractPath}\" -sdel",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = new Process() { StartInfo = processInfo };
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0) {
                    throw new Exception($"7-Zip extraction failed with exit code {process.ExitCode}: {error}");
                }

                Console.WriteLine("Extraction succeeded.");
            } catch (Exception ex) {
                Console.WriteLine($"An error occurred during extraction: {ex.Message}");
            }
        }

        // utility methods
        private void ValidateBrowserInitialized() {
            if (this._browser == null) {
                throw new InvalidOperationException("Browser is not initialized.");
            }
        }

        private async Task<string> GetConfigElementPropertyAsync(IPage page, string section, string key, string property) {
            var selector = this._config.GetSelector(section, key);
            var element = await page.QuerySelectorAsync(selector) ?? throw new InvalidOperationException($"Element is not initialized.({selector})");
            var value = (await element.GetPropertyAsync(property)).RemoteObject.Value.ToString();
            return value;
        }

        private async Task<string> GetConfigElementPropertyAsync(IElementHandle personElement, string section, string key, string property) {
            var selector = this._config.GetSelector(section, key);
            var element = await personElement.QuerySelectorAsync(selector) ?? throw new InvalidOperationException($"Element is not initialized.({selector})");
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

            return new HttpClient(handler);
        }
    }
}