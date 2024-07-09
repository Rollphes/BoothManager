using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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