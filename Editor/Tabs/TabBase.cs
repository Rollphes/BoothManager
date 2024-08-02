using io.github.rollphes.boothManager.client;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.boothManager.tabs {
    internal abstract class TabBase {
        private static readonly StyleSheet _styleSheet = Resources.Load<StyleSheet>("USS/Util");

        internal abstract Texture2D TabIcon { get; }
        internal abstract string Tooltip { get; }

        protected abstract VisualTreeAsset InitTabUxml { get; }

        protected Client _client;
        protected VisualElement _tabContent;
        protected TabController _tabController;

        internal TabBase(Client client, EditorWindow window, TabController tabController) {
            this._client = client;
            this._tabController = tabController;
            this._tabContent = window.rootVisualElement.Q<VisualElement>("TabContent");
        }

        internal virtual void Show() {
            this._tabContent.Clear();
            this.InitTabUxml.CloneTree(this._tabContent);
            this._tabContent.styleSheets.Add(_styleSheet);
        }
    }
}