#pragma warning disable CS0618

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

using PuppeteerSharp;
using PuppeteerSharp.BrowserData;

using UnityEditor;

namespace io.github.rollphes.epmanager.booth {
    internal enum DeployStatusType {
        BrowserDownloading,
        AutoLoginInProgress,
        Complete
    }

    internal enum FetchItemInfoStatusType {
        ItemIdFetchingInLibrary,
        ItemIdFetchingInGift,
        ItemInfoFetching
    }

    internal static class BoothClient {
        internal static string NickName { get; private set; }
        internal static bool IsDeployed { get; private set; } = false;
        internal static bool IsLoggedIn { get; private set; } = false;

        private static readonly string _browserPath = Path.Combine(BoothConfig.RoamingDirectoryPath, "Browser");
        private static readonly string _packagesDirectoryPath = Path.Combine(BoothConfig.RoamingDirectoryPath, "Packages");
        private const string _userAgent = "\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36\"";
        private const string _fileLinkPattern = @"(?<=<a class=""nav-reverse"" href="")https://booth.pm/downloadables/.*?(?="">)";
        private const string _sevenZipExePath = "Packages/io.github.rollphes.epmanager/Runtime/7-Zip/7z.exe";

        internal static Action<DeployStatusType> OnDeployProgressing;

        private static ItemInfo[] _itemInfos;
        private static InstalledBrowser _installedBrowser;

        internal static async Task Deploy() {
            if (IsDeployed) {
                return;
            }

            OnDeployProgressing?.Invoke(DeployStatusType.BrowserDownloading);
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = _browserPath, Browser = SupportedBrowser.Chromium });
            _installedBrowser = await browserFetcher.DownloadAsync();

            OnDeployProgressing?.Invoke(DeployStatusType.AutoLoginInProgress);
            await CheckAutoLogin();

            OnDeployProgressing?.Invoke(DeployStatusType.Complete);
            IsDeployed = true;
        }

        internal static async Task SignIn(string email, string password) {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
                throw new ArgumentException("Email and password are required.");
            }

            var browser = await FetchBrowserAsync();
            var page = (await browser.PagesAsync())[0];
            await page.GoToAsync(BoothConfig.GetEndpointUrl("auth", "signIn"));

            var emailInputSelector = BoothConfig.GetSelector("auth", "emailInput");
            var passwordInputSelector = BoothConfig.GetSelector("auth", "passwordInput");

            await page.WaitForSelectorAsync(emailInputSelector);
            await page.TypeAsync(emailInputSelector, email);
            await page.TypeAsync(passwordInputSelector, password);
            await page.Keyboard.PressAsync("Enter");
            try {
                await page.WaitForNavigationAsync(new NavigationOptions { Timeout = 10 * 1000 });
            } catch (Exception) {
                await page.CloseAsync();
                await browser.DisposeAsync();
                throw new Exception("Login Failed");
            }

            IsLoggedIn = true;
            NickName = await GetConfigElementPropertyAsync(page, "auth", "nickName", "innerText");

            var client = await page.Target.CreateCDPSessionAsync(); // GetCookiesAsync does not work properly in Chromium
            var response = await client.SendAsync("Network.getAllCookies");
            BoothConfig.SaveCookieParams(response);

            await page.DisposeAsync();
            await browser.DisposeAsync();
        }

        internal static void SignUp() {
            Process.Start(BoothConfig.GetEndpointUrl("auth", "signUp"));
        }

        internal static void SignOut() {
            BoothConfig.DeleteCookieParams();
            IsLoggedIn = false;
            NickName = null;
            _itemInfos = null;
        }

        private static async Task CheckAutoLogin() {
            var cookieParams = BoothConfig.GetCookieParams();
            if (cookieParams != null) {
                var browser = await FetchBrowserAsync();
                var page = (await browser.PagesAsync())[0];
                await page.SetCookieAsync(cookieParams);
                await page.GoToAsync(BoothConfig.GetEndpointUrl("home", "home"));

                var selector = BoothConfig.GetSelector("home", "checkLoggedIn");
                var element = await page.QuerySelectorAsync(selector);

                IsLoggedIn = element == null;
                if (IsLoggedIn) {
                    NickName = await GetConfigElementPropertyAsync(page, "auth", "nickName", "innerText");
                }
                await page.DisposeAsync();
                await browser.DisposeAsync();
            }
        }

        private static async Task<List<string>> FetchItemIds(Action<FetchItemInfoStatusType, int, int> onProgressing) {
            if (!IsLoggedIn) {
                throw new InvalidOperationException("You are not logged in.");
            }

            var browser = await FetchBrowserAsync();
            var page = (await browser.PagesAsync())[0];
            await page.SetCookieAsync(BoothConfig.GetCookieParams());

            var itemIds = new List<string>();
            var pageTypes = new string[] { "library", "gift" };
            var lastPageCounts = new Dictionary<string, int>();

            foreach (var pageType in pageTypes) {
                var urlParams = new Dictionary<string, string> { { "pageNumber", "1" } };
                try {
                    await page.GoToAsync(BoothConfig.GetEndpointUrl("library", pageType, urlParams));
                    var lastPageUrlLink = await GetConfigElementPropertyAsync(page, "library", "lastPageLink", "href");
                    lastPageCounts[pageType] = int.Parse(Regex.Match(lastPageUrlLink, @"(?<=\?page=).*?(?=$|&)").ToString());
                } catch {
                    lastPageCounts[pageType] = 0;
                }
            }

            foreach (var pageType in pageTypes) {
                for (var i = 1; i <= lastPageCounts[pageType]; i++) {
                    onProgressing?.Invoke(pageType == "library" ? FetchItemInfoStatusType.ItemIdFetchingInLibrary : FetchItemInfoStatusType.ItemIdFetchingInGift, i, lastPageCounts[pageType]);

                    var urlParams = new Dictionary<string, string> { { "pageNumber", i.ToString() } };
                    await page.GoToAsync(BoothConfig.GetEndpointUrl("library", pageType, urlParams));

                    var orders = await page.QuerySelectorAllAsync(BoothConfig.GetSelector("library", "orders"));
                    if (orders.Length == 0) {
                        break;
                    }

                    var orderTasks = orders.Select(async (order) => {
                        var itemUrlLink = await GetConfigElementPropertyAsync(order, "library", "itemLink", "href");
                        var itemId = itemUrlLink.Split("/").Last();
                        if (!itemIds.Contains(itemId)) {
                            itemIds.Add(itemId);
                        }
                    });
                    await Task.WhenAll(orderTasks);

                    if (orders.Length < 10) {
                        break;
                    }
                }
            }

            await page.DisposeAsync();
            await browser.DisposeAsync();

            return itemIds;
        }

        internal static async Task<ItemInfo[]> FetchItemInfos(bool force = false, Action<FetchItemInfoStatusType, int, int> onProgressing = null) {
            if (_itemInfos == null || force) {
                var itemIds = await FetchItemIds(onProgressing);
                if (!IsLoggedIn) {
                    throw new InvalidOperationException("You are not logged in.");
                }

                var httpClient = GetHttpClient();
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var taskCompletedCount = 0;

                var tasks = itemIds.Select(async (itemId) => {
                    var itemInfo = await FetchItemInfoAsync(httpClient, itemId);
                    taskCompletedCount++;
                    onProgressing?.Invoke(FetchItemInfoStatusType.ItemInfoFetching, taskCompletedCount, itemIds.Count);
                    return itemInfo;
                });
                _itemInfos = await Task.WhenAll(tasks);
            }
            return _itemInfos;
        }

        private static async Task<ItemInfo> FetchItemInfoAsync(HttpClient httpClient, string itemId) {
            var urlParams = new Dictionary<string, string> { { "lang", "ja" }, { "itemId", itemId } };
            var url = BoothConfig.GetEndpointUrl("api", "itemInfo", urlParams);
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
        private static async Task DownloadFileFromRedirectUrlAsync(HttpClient httpClient, string url, string destinationDirectoryPath) {
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

            ExtractZipFile(zipFilePath, destinationDirectoryPath);
        }

        private static void ExtractZipFile(string zipFilePath, string extractPath) {
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
        private static async Task<string> GetConfigElementPropertyAsync(IPage page, string section, string key, string property) {
            var selector = BoothConfig.GetSelector(section, key);
            var element = await page.QuerySelectorAsync(selector) ?? throw new InvalidOperationException($"Element is not initialized.({selector})");
            var value = (await element.GetPropertyAsync(property)).RemoteObject.Value.ToString();
            return value;
        }

        private static async Task<string> GetConfigElementPropertyAsync(IElementHandle personElement, string section, string key, string property) {
            var selector = BoothConfig.GetSelector(section, key);
            var element = await personElement.QuerySelectorAsync(selector) ?? throw new InvalidOperationException($"Element is not initialized.({selector})");
            var value = (await element.GetPropertyAsync(property)).RemoteObject.Value.ToString();
            return value;
        }

        private static HttpClient GetHttpClient() {
            var handler = new HttpClientHandler {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = false,
            };

            var cookies = BoothConfig.GetCookieParams();
            foreach (var cookie in cookies) {
                handler.CookieContainer.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
            }

            return new HttpClient(handler);
        }

        private static async Task<IBrowser> FetchBrowserAsync() {
            if (_installedBrowser == null) {
                throw new InvalidOperationException("Browser is not installed");
            };
            return await Puppeteer.LaunchAsync(new LaunchOptions {
                ExecutablePath = _installedBrowser.GetExecutablePath(),
                HeadlessMode = HeadlessMode.True,
                Args = new[] {
                    "--window-size=1,1",
                    "--window-position=-10000,-10000",
                    $"--user-agent={_userAgent}",
                }
            });
        }
    }
}