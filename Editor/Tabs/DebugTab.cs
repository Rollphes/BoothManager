using UnityEngine;
using UnityEngine.UIElements;
using io.github.rollphes.boothManager.client;

namespace io.github.rollphes.boothManager.tabs {
    internal class DebugTab : TabBase {
        internal override string Tooltip => "デバック";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Default");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/DebugTabContent");

        internal DebugTab(Client client, VisualElement tabContent) : base(client, tabContent) { }

        internal override void Show() {
            base.Show();
            var elementButton = this._tabContent.Q<VisualElement>("ElementButton");
            elementButton.RegisterCallback<ClickEvent>(async evt => {
                Debug.Log("Element clicked!");
                await this._client.FetchItemIds();
                await this._client.FetchItemInfoList();
                await this._client.DownloadAllFile();
                Debug.Log("Library fetched!");
            });
        }
    }
}