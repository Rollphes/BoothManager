using io.github.rollphes.boothManager.client;
using io.github.rollphes.boothManager.tabs;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.boothManager {
    public class BoothManager : EditorWindow {

        private static Client _client;
        private static readonly string _githubLink = "https://github.com/Rollphes/BoothManager";
        private static readonly string _changeLogLink = $"{_githubLink}/releases";

        private TabController _tabController;

        [MenuItem("BoothManager/MainWindow")]
        public static void ShowWindow() {
            var wnd = GetWindow<BoothManager>("BoothManager");
            wnd.Show();
        }

        [InitializeOnLoadMethod]
        private static async void Initialize() {
            _client ??= new Client();
            if (_client.IsDeployed == false) {
                await _client.Deploy();
            }
        }

        public void CreateGUI() {
            var root = this.rootVisualElement;
            var mainUXML = Resources.Load<VisualTreeAsset>("UI/BoothManager");
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