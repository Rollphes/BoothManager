using System;
using UnityEngine;
using UnityEngine.UIElements;
using io.github.rollphes.boothManager.client;

namespace io.github.rollphes.boothManager.tabs {

    internal class TabController {
        private static readonly VisualTreeAsset _tabIconActiveUxml = Resources.Load<VisualTreeAsset>("UI/Components/TabIconActive");
        private static readonly VisualTreeAsset _tabIconPassiveUxml = Resources.Load<VisualTreeAsset>("UI/Components/TabIconPassive");

        private readonly VisualElement _tabBar;

        private readonly TabBase[] _tabs;
        private int _activeTabIndex = 0;
        internal bool _IsLock = false;

        internal TabController(Client client, VisualElement tabContent, VisualElement tabBar) {
            this._tabBar = tabBar;

            this._tabs = new TabBase[] {
            new AuthTab(client, this, tabContent),
            new LibraryTab(client, this, tabContent),
            new DebugTab(client, this, tabContent)
            };
            this._tabs[this._activeTabIndex].Show();
            this.ShowTabBar();
        }

        private void ShowTabBar() {
            this._tabBar.Clear();
            int index = -1;
            foreach (var tab in this._tabs) {
                index++;
                var root = new VisualElement();
                if (index == this._activeTabIndex) {
                    _tabIconActiveUxml.CloneTree(root);
                } else {
                    _tabIconPassiveUxml.CloneTree(root);
                }
                var tabIconElement = root.Q<VisualElement>("TabIcon");
                tabIconElement.style.backgroundImage = new Background { texture = tab.TabIcon };
                tabIconElement.tooltip = tab.Tooltip;
                tabIconElement.RegisterCallback<ClickEvent>(evt => {
                    if (!this._IsLock) {
                        this._activeTabIndex = Array.FindIndex(this._tabs, t => t.GetType() == tab.GetType());
                        this.ShowTabBar();
                        tab.Show();
                    }
                });
                this._tabBar.Add(root);
            }
        }
    }
}