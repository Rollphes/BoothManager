using System;

using io.github.rollphes.epmanager.booth;
using io.github.rollphes.epmanager.library;

using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.epmanager.popups {
    internal class TagSelectPopup : PopupBase {
        private static readonly VisualTreeAsset _tagListLineUxml = Resources.Load<VisualTreeAsset>("UI/Components/TagListLine");

        internal Action<Tag> OnChangeSelectedTag;
        internal Tag _selectedTag;

        protected override VisualTreeAsset InitTagUxml => Resources.Load<VisualTreeAsset>("UI/Popups/TagSelectPopupContent");

        private ScrollView _tagListView;
        private string _searchText = "";

        public override Vector2 GetWindowSize() {
            return new Vector2(200, 500);
        }

        public override void OnOpen() {
            base.OnOpen();
            var root = this.editorWindow.rootVisualElement;

            var tagFilterField = root.Q<ToolbarSearchField>("TagFilterField");
            tagFilterField.value = this._searchText;
            tagFilterField.RegisterValueChangedCallback((evt) => {
                this._searchText = evt.newValue;
                this.ShowTags();
            });

            this._tagListView = root.Q<ScrollView>("TagListView");

            this.ShowTags();
        }

        private void ShowTags() {
            this._tagListView.Clear();

            var tags = Library.GetTags(this._searchText);

            foreach (var tag in tags) {
                var root = new VisualElement();
                _tagListLineUxml.CloneTree(root);
                root.RegisterCallback<MouseOverEvent>((evt) => {
                    root.AddToClassList("MouseOver");
                });
                root.RegisterCallback<MouseLeaveEvent>((evt) => {
                    root.RemoveFromClassList("MouseOver");
                });
                root.RegisterCallback<ClickEvent>((evt) => {
                    this._selectedTag = this._selectedTag == tag ? null : tag;
                    this.OnChangeSelectedTag?.Invoke(tag);
                    this.ShowTags();
                });

                var tagName = root.Q<Label>("TagName");
                tagName.text = tag.Name;

                var isSelected = root.Q<Label>("IsSelected");
                isSelected.text = tag == this._selectedTag ? "✔" : "";

                this._tagListView.Add(root);
            }
        }
    }
}
