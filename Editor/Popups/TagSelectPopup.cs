using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using io.github.rollphes.boothManager.client;
using io.github.rollphes.boothManager.types.api;
using io.github.rollphes.boothManager.util;

using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.boothManager.popups {
    internal class TagSelectPopup : PopupBase {
        private static readonly VisualTreeAsset _tagListLineUxml = Resources.Load<VisualTreeAsset>("UI/Components/TagListLine");

        internal Action<Tag> OnChangeSelectedTag;
        internal string _selectedTagName = "";

        protected override VisualTreeAsset InitTagUxml => Resources.Load<VisualTreeAsset>("UI/Popups/TagSelectPopupContent");

        private ScrollView _tagListView;
        private string _searchText = "";

        public TagSelectPopup(Client client) : base(client) { }

        public override Vector2 GetWindowSize() {
            return new Vector2(200, 500);
        }

        public override void OnOpen() {
            base.OnOpen();
            var root = this.editorWindow.rootVisualElement;

            var tagFilterField = root.Q<ToolbarSearchField>("TagFilterField");
            tagFilterField.value = this._searchText;
            tagFilterField.RegisterValueChangedCallback(evt => {
                this._searchText = evt.newValue;
                this.ShowTags();
            });

            this._tagListView = root.Q<ScrollView>("TagListView");

            this.ShowTags();
        }

        private void ShowTags() {
            this._tagListView.Clear();

            var itemInfos = this._client.FetchItemInfos().Result;
            if (itemInfos == null || itemInfos.Length == 0) {
                return;
            }

            var filteredTags = this.GetFilteredTags(itemInfos);

            foreach (var tag in filteredTags) {
                var root = new VisualElement();
                _tagListLineUxml.CloneTree(root);
                root.RegisterCallback<MouseOverEvent>(evt => {
                    root.AddToClassList("MouseOver");
                });
                root.RegisterCallback<MouseLeaveEvent>(evt => {
                    root.RemoveFromClassList("MouseOver");
                });
                root.RegisterCallback<ClickEvent>(evt => {
                    this._selectedTagName = this._selectedTagName == tag.Name ? "" : tag.Name;
                    this.OnChangeSelectedTag?.Invoke(tag);
                    this.ShowTags();
                });

                var tagName = root.Q<Label>("TagName");
                tagName.text = tag.Name;

                var isSelected = root.Q<Label>("IsSelected");
                isSelected.text = tag.Name == this._selectedTagName ? "✔" : "";

                this._tagListView.Add(root);
            }
        }

        private Tag[] GetFilteredTags(ItemInfo[] itemInfos) {
            var tags = itemInfos.SelectMany(itemInfo => itemInfo.Tags).ToArray();
            var tagList = new List<Tag>();

            foreach (var tag in tags) {
                if (!tagList.Select((tag) => tag.Name).ToList().Contains(tag.Name)) {
                    tagList.Add(tag);
                }
            }

            return Array.FindAll(tagList.ToArray(), (tag) => {
                var normalizedItemName = Util.ConvertToSearchText(tag.Name);
                var normalizedFilter = Util.ConvertToSearchText(this._searchText);
                return Regex.IsMatch(normalizedItemName, normalizedFilter);
            });
        }
    }
}
