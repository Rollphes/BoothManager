using UnityEngine;
using UnityEngine.UIElements;

internal class DebugTab : TabBase {
    internal override string Tooltip => "�f�o�b�N";
    internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Default");

    protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/DebugTabContent");

    internal DebugTab(Client client, VisualElement tabContent) : base(client, tabContent) { }

    internal override void Show() {
        base.Show();
        var elementButton = this._tabContent.Q<VisualElement>("ElementButton");
        elementButton.RegisterCallback<ClickEvent>(async evt => {
            Debug.Log("Element clicked!");
            await this._client.FetchLibrary();
            Debug.Log("Library fetched!");
        });
    }
}