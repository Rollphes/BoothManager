using UnityEngine;
using UnityEngine.UIElements;
using io.github.rollphes.boothManager.client;

namespace io.github.rollphes.boothManager.tabs {
    internal class DebugTab : TabBase {
        internal override string Tooltip => "�f�o�b�N";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Default");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/DebugTabContent");

        public DebugTab(Client client, TabController tabController, VisualElement tabContent) : base(client, tabController, tabContent) { }

        internal override void Show() {
            base.Show();
            var elementButton = this._tabContent.Q<VisualElement>("ElementButton");
            elementButton.RegisterCallback<ClickEvent>(async evt => {
                Debug.Log("Element clicked!");
                await this._client.FetchItemInfos();
                Debug.Log("Library fetched!");
            });
        }
    }
}