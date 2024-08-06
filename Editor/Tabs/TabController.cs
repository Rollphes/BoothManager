using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.epmanager.tabs {

    internal class TabController {
        internal static bool IsLock = false;

        private static readonly VisualTreeAsset _tabIconActiveUxml = Resources.Load<VisualTreeAsset>("UI/Components/TabIconActive");
        private static readonly VisualTreeAsset _tabIconPassiveUxml = Resources.Load<VisualTreeAsset>("UI/Components/TabIconPassive");

        private static int _activeTabIndex = 0;

        private readonly VisualElement _tabBar;
        private readonly TabBase[] _tabs;

        internal TabController(MainWindow window) {
            this._tabBar = window.rootVisualElement.Q<VisualElement>("TabBar");
            this._tabs = new TabBase[] {
                new AuthTab(window),
                new LibraryTab(window),
                new DebugTab(window)
            };

            this._tabs[_activeTabIndex].Show();
            this.ShowTabBar();
        }

        private void ShowTabBar() {
            this._tabBar.Clear();
            var index = -1;

            foreach (var tab in this._tabs) {
                var root = new VisualElement();

                var tabIconUxml = (++index == _activeTabIndex) ? _tabIconActiveUxml : _tabIconPassiveUxml;
                tabIconUxml.CloneTree(root);

                var tabIconElement = root.Q<VisualElement>("TabIcon");
                tabIconElement.style.backgroundImage = new Background { texture = tab.TabIcon };
                tabIconElement.tooltip = tab.Tooltip;
                tabIconElement.RegisterCallback<ClickEvent>((evt) => {
                    if (!IsLock) {
                        _activeTabIndex = Array.FindIndex(this._tabs, (t) => t.GetType() == tab.GetType());

                        this.ShowTabBar();
                        tab.Show();
                    }
                });

                this._tabBar.Add(root);
            }
        }
    }
}