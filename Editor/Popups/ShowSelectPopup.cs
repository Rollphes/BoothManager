using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

using VRC.PackageManagement.Core;

namespace io.github.rollphes.boothManager.popups {
    internal enum ArgLimitType {
        All,
        AllAgesOnly,
        R18Only
    }

    internal class ShowSelectPopup : PopupWindowContent {
        internal Action<ArgLimitType> OnChangeArgLimit;
        internal ArgLimitType ArgLimitType;

        private static readonly VisualTreeAsset _initPopupUxml = Resources.Load<VisualTreeAsset>("UI/Popups/ShowSelectPopupContent");
        private static readonly Dictionary<ArgLimitType, string> _argLimitDropDownItems = new() {
            {ArgLimitType.All ,"すべて"},
            {ArgLimitType.AllAgesOnly , "全年齢のみ" },
            {ArgLimitType.R18Only,"R18商品のみ" }
        };

        public ShowSelectPopup() { }

        public override Vector2 GetWindowSize() {
            return new Vector2(200, 100);
        }

        public override void OnGUI(Rect rect) { }

        public override void OnOpen() {
            var root = this.editorWindow.rootVisualElement;
            _initPopupUxml.CloneTree(root);

            var argLimitDropDown = root.Q<DropdownField>("ArgLimitDropDown");
            argLimitDropDown.choices.Clear();

            argLimitDropDown.value = _argLimitDropDownItems[this.ArgLimitType];

            foreach (var item in _argLimitDropDownItems) {
                argLimitDropDown.choices.Add(item.Value);
            }

            argLimitDropDown.RegisterValueChangedCallback(evt => {
                var newArgLimitType = _argLimitDropDownItems.FindFirstKeyByValue(evt.newValue);
                this.ArgLimitType = newArgLimitType;
                this.OnChangeArgLimit?.Invoke(newArgLimitType);
            });
        }

        public override void OnClose() { }
    }
}
