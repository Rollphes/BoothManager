using System;
using System.Collections.Generic;

using io.github.rollphes.boothManager.client;

using UnityEngine;
using UnityEngine.UIElements;

using VRC.PackageManagement.Core;

namespace io.github.rollphes.boothManager.popups {
    internal enum ArgLimitType {
        All,
        AllAgesOnly,
        R18Only
    }

    internal class ShowSelectPopup : PopupBase {
        private static readonly Dictionary<ArgLimitType, string> _argLimitDropDownItems = new() {
            {ArgLimitType.All ,"すべて"},
            {ArgLimitType.AllAgesOnly , "全年齢のみ" },
            {ArgLimitType.R18Only,"R18商品のみ" }
        };

        internal Action<ArgLimitType> OnChangeArgLimitType;
        internal ArgLimitType ArgLimitType;

        protected override VisualTreeAsset InitTagUxml => Resources.Load<VisualTreeAsset>("UI/Popups/ShowSelectPopupContent");

        public ShowSelectPopup(Client client) : base(client) { }

        public override void OnOpen() {
            base.OnOpen();
            var root = this.editorWindow.rootVisualElement;

            var argLimitDropDown = root.Q<DropdownField>("ArgLimitDropDown");
            argLimitDropDown.choices.Clear();

            argLimitDropDown.value = _argLimitDropDownItems[this.ArgLimitType];

            foreach (var item in _argLimitDropDownItems) {
                argLimitDropDown.choices.Add(item.Value);
            }

            argLimitDropDown.RegisterValueChangedCallback((evt) => {
                var newArgLimitType = _argLimitDropDownItems.FindFirstKeyByValue(evt.newValue);
                this.ArgLimitType = newArgLimitType;
                this.OnChangeArgLimitType?.Invoke(newArgLimitType);
            });
        }
    }
}
