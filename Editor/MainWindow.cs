using io.github.rollphes.epmanager.client;
using io.github.rollphes.epmanager.tabs;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.epmanager {
    public class MainWindow : EditorWindow {
        private static Client _client;
        private static readonly string _githubLink = "https://github.com/Rollphes/EPManager";
        private static readonly string _changeLogLink = $"{_githubLink}/releases";

        private TabController _tabController;

        [MenuItem("EPManager/MainWindow")]
        public static void ShowWindow() {
            var wnd = GetWindow<MainWindow>("EPManager");
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
            var mainUXML = Resources.Load<VisualTreeAsset>("UI/MainWindow");
            mainUXML.CloneTree(root);

            var changeLogLink = root.Q<ToolbarButton>("ChangeLogLink");
            var githubLink = root.Q<ToolbarButton>("GithubLink");

            changeLogLink.clicked += () => System.Diagnostics.Process.Start(_changeLogLink);
            githubLink.clicked += () => System.Diagnostics.Process.Start(_githubLink);

            this._tabController = new TabController(_client, this);
        }
    }
}