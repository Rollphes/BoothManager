using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PuppeteerSharp;

using UnityEngine;

namespace io.github.rollphes.epmanager.config {

    internal class ConfigLoader {
        internal static readonly string RoamingDirectoryPath = "\\\\?\\" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BoothManager");

        private static readonly string _cookieDirectoryPath = Path.Combine(RoamingDirectoryPath, "Cookies");
        private static readonly string _cookieJsonPath = Path.Combine(_cookieDirectoryPath, "cookies.json");
        private static readonly JObject _configJson = JObject.Parse(Resources.Load<TextAsset>("Conf/booth.config").text);

        private List<JObject> _cookiesJson;

        internal void Deploy() {
            if (!Directory.Exists(_cookieDirectoryPath)) {
                Directory.CreateDirectory(_cookieDirectoryPath);
            }
            this.SetCookiesJson();
        }

        internal string GetEndpointUrl(string section, string key, Dictionary<string, string> args = null) {
            var sectionObj = _configJson[section] ?? throw new Exception($"Section '{section}' not found in config file");
            var endpoints = sectionObj["endpoints"] ?? throw new Exception($"Endpoints not found in section '{section}'");
            var endpointInfo = endpoints[key] ?? throw new Exception($"Endpoint '{key}' not found in section '{section}'");

            var protocol = endpointInfo["protocol"]?.ToString() ?? _configJson["default"]["endpoint"]["protocol"].ToString();
            var domain = endpointInfo["domain"]?.ToString() ?? _configJson["default"]["endpoint"]["domain"].ToString();
            var path = endpointInfo["path"]?.ToString() ?? _configJson["default"]["endpoint"]["path"].ToString();
            var subDomain = endpointInfo["subDomain"]?.ToString() ?? _configJson["default"]["endpoint"]["subDomain"]?.ToString();

            var url = $"{protocol}://{(string.IsNullOrEmpty(subDomain) ? "" : subDomain + ".")}{domain}{path}";

            if (args != null) {
                foreach (var arg in args) {
                    url = url.Replace($":{arg.Key}", arg.Value);
                }
            }

            if (endpointInfo["queryParams"] != null) {
                var queryParams = endpointInfo["queryParams"].ToObject<Dictionary<string, string>>();
                var queryString = string.Join("&", queryParams.Select((queryParam) => {
                    var value = queryParam.Value;
                    if (args != null && value.Contains(":")) {
                        foreach (var arg in args) {
                            value = value.Replace($":{arg.Key}", arg.Value);
                        }
                    }
                    return $"{queryParam.Key}={value}";
                }));
                url += $"?{queryString}";
            }

            return url;
        }

        internal string GetSelector(string section, string key) {
            var sectionObj = _configJson[section] ?? throw new Exception($"Section '{section}' not found in config file");
            var selectors = sectionObj["selectors"] ?? throw new Exception($"Selectors not found in section '{section}'");
            var selector = selectors[key] ?? throw new Exception($"Selector '{key}' not found in section '{section}'");
            return selector.ToString();
        }

        internal CookieParam[] GetCookieParams() {
            return this._cookiesJson?.Select((cookieJson) =>
                new CookieParam {
                    Name = cookieJson["name"].ToString(),
                    Value = cookieJson["value"].ToString(),
                    Domain = cookieJson["domain"].ToString(),
                    Path = cookieJson["path"].ToString(),
                    Secure = Convert.ToBoolean(cookieJson["secure"]),
                    HttpOnly = Convert.ToBoolean(cookieJson["httpOnly"])
                }).ToArray();
        }

        internal void SaveCookieParams(JObject response) {
            var jsonObject = JObject.Parse(response.ToString());
            var cookies = jsonObject["cookies"].ToObject<JArray>();
            var json = cookies.ToString();
            File.WriteAllText(_cookieJsonPath, json);

            this.SetCookiesJson();
        }

        internal void DeleteCookieParams() {
            if (File.Exists(_cookieJsonPath)) {
                File.Delete(_cookieJsonPath);
            }
            this._cookiesJson = null;
        }

        private void SetCookiesJson() {
            if (File.Exists(_cookieJsonPath)) {
                var json = File.ReadAllText(_cookieJsonPath);
                this._cookiesJson = JsonConvert.DeserializeObject<List<JObject>>(json);
            }
        }
    }
}