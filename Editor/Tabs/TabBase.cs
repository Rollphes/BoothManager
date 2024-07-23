using io.github.rollphes.boothManager.client;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.boothManager.tabs {
    internal abstract class TabBase {
        internal abstract Texture2D TabIcon { get; }
        internal abstract string Tooltip { get; }

        protected abstract VisualTreeAsset InitTabUxml { get; }

        protected Client _client;
        protected VisualElement _tabContent;
        protected TabController _tabController;

        internal TabBase(Client client, TabController tabController, VisualElement tabContent) {
            this._client = client;
            this._tabController = tabController;
            this._tabContent = tabContent;
        }

        internal virtual void Show() {
            this._tabContent.Clear();
            this.InitTabUxml.CloneTree(this._tabContent);
        }
    }
}