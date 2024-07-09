using UnityEngine;
using UnityEngine.UIElements;
using io.github.rollphes.boothManager.client;

namespace io.github.rollphes.boothManager.tabs {
    internal abstract class TabBase {
        internal abstract Texture2D TabIcon { get; }
        internal abstract string Tooltip { get; }

        protected Client _client;
        protected VisualElement _tabContent;
        protected abstract VisualTreeAsset InitTabUxml { get; }

        internal TabBase(Client client, VisualElement tabContent) {
            this._client = client;
            this._tabContent = tabContent;
        }

        internal virtual void Show() {
            this._tabContent.Clear();
            this.InitTabUxml.CloneTree(this._tabContent);
        }
    }
}