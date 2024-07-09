using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PuppeteerSharp;
using UnityEngine;

internal class Client {
    private static readonly string _browserPath = Path.Combine(ConfigLoader.RoamingDirectoryPath, "Browser");
    private static readonly string _userAgent = "\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36\"";

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
                $"--user-agent={_userAgent}"//test
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

    internal async Task FetchLibrary() {
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
                    var itemUrlTask = this.GetConfigElementPropertyAsync(order, "library", "itemLink", "href");
                    var imageUrlTask = this.GetConfigElementPropertyAsync(order, "library", "itemImageLink", "src");
                    var shopUrlTask = this.GetConfigElementPropertyAsync(order, "library", "shopLink", "href");
                    var variationNameTask = this.GetConfigElementPropertyAsync(order, "library", "variationName", "innerText");

                    var fileList = await order.QuerySelectorAllAsync(this._config.GetSelector("library", "fileList"));

                    var fileTasks = fileList.Select(async file => {
                        var name = await this.GetConfigElementPropertyAsync(file, "library", "fileName", "innerText");
                        var link = await this.GetConfigElementPropertyAsync(file, "library", "fileLink", "href");
                        return (name, link);
                    });

                    var fileResults = await Task.WhenAll(fileTasks);
                    await Task.WhenAll(itemUrlTask, imageUrlTask, shopUrlTask, variationNameTask);

                    var itemUrl = await itemUrlTask;
                    var imageUrl = await imageUrlTask;
                    var shopUrl = await shopUrlTask;
                    var variationName = await variationNameTask;

                    foreach (var (name, link) in fileResults) {
                        Debug.Log($"Item: {variationName} - {name} - {link}");
                    }
                });

                await Task.WhenAll(orderTasks);

                if (orders.Length < 10)
                    break;
            }
        } finally {
            await page.CloseAsync();
        }
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
}