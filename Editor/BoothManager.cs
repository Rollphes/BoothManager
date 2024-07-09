using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using io.github.rollphes.boothManager.client;
using io.github.rollphes.boothManager.tabs;
using System.Security.Policy;
using UnityEditor.UIElements;
using System;

namespace io.github.rollphes.boothManager {
    public class BoothManager : EditorWindow {

        private static Client _client;
        private static string _githubLink = "https://github.com/Rollphes/BoothManager";
        private static string _changeLogLink = $"{_githubLink}/releases";

        private TabController _tabController;

        [MenuItem("BoothManager/MainWindow")]
        public static void ShowWindow() {
            BoothManager wnd = GetWindow<BoothManager>("BoothManager");
            wnd.Show();
        }

        [InitializeOnLoadMethod]
        private static async void Initialize() {
            Debug.Log("Initialize Client");

            _client ??= new Client();
            if (_client.IsDeployed == false)
                await _client.Deploy();
            EditorApplication.quitting += DestroyClient;
            AssemblyReloadEvents.beforeAssemblyReload += DestroyClient;
        }

        private static async void DestroyClient() {
            Debug.Log("Destroy Client");

            if (_client != null) {
                await _client.Destroy();
            }
        }

        public void CreateGUI() {
            VisualElement root = this.rootVisualElement;
            VisualTreeAsset mainUXML = Resources.Load<VisualTreeAsset>("UI/BoothManager");
            mainUXML.CloneTree(root);

            var tabContent = root.Q<VisualElement>("TabContent");
            var tabBar = root.Q<VisualElement>("TabBar");
            var changeLogLink = root.Q<ToolbarButton>("ChangeLogLink");
            var githubLink = root.Q<ToolbarButton>("GithubLink");

            changeLogLink.clicked += () => System.Diagnostics.Process.Start(_changeLogLink);
            githubLink.clicked += () => System.Diagnostics.Process.Start(_githubLink);

            this._tabController = new TabController(_client, tabContent, tabBar);
        }
    }
}