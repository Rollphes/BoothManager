using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.epmanager.tabs {
    internal abstract class TabBase {
        private static readonly StyleSheet _styleSheet = Resources.Load<StyleSheet>("USS/Util");

        internal abstract Texture2D TabIcon { get; }
        internal abstract string Tooltip { get; }

        protected abstract VisualTreeAsset InitTabUxml { get; }

        protected MainWindow _window;
        protected VisualElement _tabContent;

        internal TabBase(MainWindow window) {
            this._tabContent = window.rootVisualElement.Q<VisualElement>("TabContent");
        }

        internal virtual void Show() {
            this._tabContent.Clear();
            this.InitTabUxml.CloneTree(this._tabContent);
            this._tabContent.styleSheets.Add(_styleSheet);
        }
    }
}