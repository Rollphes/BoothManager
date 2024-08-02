using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using io.github.rollphes.boothManager.client;
using io.github.rollphes.boothManager.popups;
using io.github.rollphes.boothManager.types.api;
using io.github.rollphes.boothManager.util;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

using PopupWindow = UnityEditor.PopupWindow;

namespace io.github.rollphes.boothManager.tabs {
    internal class LibraryTab : TabBase {
        private static readonly VisualTreeAsset _itemPanelUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemPanel");
        private static readonly VisualTreeAsset _itemListLineUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemListLine");

        internal override string Tooltip => "ライブラリ";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Package");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/LibraryTabContent");

        private readonly Dictionary<string, PopupWindowContent> _classNameToPopupDictionary;
        private readonly Dictionary<string, Texture2D> _imageCache = new();
        private float _imageSize = 100f;
        private string _searchText = "";

        private readonly TagSelectPopup _tagSelectPopup;
        private readonly ShowSelectPopup _showSelectPopup;

        private VisualElement _libraryContent;

        public LibraryTab(Client client, TabController tabController, VisualElement tabContent) : base(client, tabController, tabContent) {
            this._tagSelectPopup = new TagSelectPopup(client);
            this._showSelectPopup = new ShowSelectPopup(client);
            this._classNameToPopupDictionary = new() {
                { "TagSelectPopupButton", this._tagSelectPopup },
                { "ShowSelectPopupButton", this._showSelectPopup }
            };
        }

        internal override void Show() {
            base.Show();

            this._libraryContent = this._tabContent.Q<VisualElement>("LibraryContent");

            var textFilterField = this._tabContent.Q<ToolbarSearchField>("TextFilterField");
            textFilterField.value = this._searchText;
            textFilterField.RegisterValueChangedCallback(evt => {
                this._searchText = evt.newValue;
                this.ShowItemInfos();
            });

            var imageSizeSlider = this._tabContent.Q<Slider>("ImageSizeSlider");
            imageSizeSlider.SetValueWithoutNotify(this._imageSize);
            imageSizeSlider.RegisterValueChangedCallback(evt => {
                this._imageSize = evt.newValue;
                this.ShowItemInfos();
            });

            foreach (var (className, popupContent) in this._classNameToPopupDictionary) {
                var toolbarButton = this._tabContent.Q<ToolbarButton>(className);
                toolbarButton.clicked += () => {
                    var buttonWorldBound = toolbarButton.worldBound;
                    var popupRect = new Rect(buttonWorldBound.xMin, buttonWorldBound.yMax, 1, 1);
                    PopupWindow.Show(popupRect, popupContent);
                };
            }

            this._showSelectPopup.OnChangeArgLimitType += (_) => this.ShowItemInfos();
            this._tagSelectPopup.OnChangeSelectedTag += (_) => this.ShowItemInfos();

            this.ShowItemInfos();
        }

        private void ShowItemInfos() {
            if (!this._client.IsLoggedIn) {
                var nonItemText = this._libraryContent.Q<Label>("NonItemText");
                nonItemText.text = "ログイン後に使用可能です";
                return;
            }
            var itemInfos = this._client.FetchItemInfos().Result;
            if (itemInfos == null || itemInfos.Length == 0) {
                var nonItemText = this._libraryContent.Q<Label>("NonItemText");
                nonItemText.text = "アイテムがありません";
            }

            var filteredItemInfos = this.GetFilteredItemInfos(itemInfos);

            var scrollView = new ScrollView();
            scrollView.contentContainer.style.flexGrow = 0;
            if (this._imageSize > 50) {
                scrollView.contentContainer.style.flexDirection = FlexDirection.Row;
                scrollView.contentContainer.style.flexWrap = Wrap.Wrap;
            }

            foreach (var itemInfo in filteredItemInfos) {
                var root = new VisualElement();

                var itemUxml = (this._imageSize > 50) ? _itemPanelUxml : _itemListLineUxml;
                itemUxml.CloneTree(root);

                var itemImage = root.Q<VisualElement>("ItemImage");
                var itemName = root.Q<Label>("ItemName");
                itemName.text = itemInfo.Name;
                root.tooltip = itemInfo.Name;

                if (this._imageSize > 50) {
                    itemName.style.width = new StyleLength(new Length(this._imageSize, LengthUnit.Pixel));

                    itemImage.style.width = new StyleLength(new Length(this._imageSize - 10, LengthUnit.Pixel));
                    itemImage.style.height = new StyleLength(new Length(this._imageSize - 10, LengthUnit.Pixel));
                }

                this.LoadImageFromUrlAsync(itemInfo.Images[0].Original.ToString(), (texture) => {
                    if (texture != null) {
                        itemImage.style.backgroundImage = new StyleBackground(texture);
                    }
                });

                scrollView.Add(root);
            }

            this._libraryContent.Clear();
            this._libraryContent.Add(scrollView);
        }

        private ItemInfo[] GetFilteredItemInfos(ItemInfo[] itemInfos) {
            var textFiltered = Array.FindAll(itemInfos, (itemInfo) => {
                var normalizedItemName = Util.ConvertToSearchText(itemInfo.Name);
                var normalizedFilter = Util.ConvertToSearchText(this._searchText);
                return Regex.IsMatch(normalizedItemName, normalizedFilter);
            });

            var argLimitFiltered = this._showSelectPopup.ArgLimitType == ArgLimitType.All
                ? textFiltered
                : Array.FindAll(textFiltered, (itemInfo) => {
                    var isR18 = itemInfo.Tags.Any((tag) => tag.Name == "R18");
                    return this._showSelectPopup.ArgLimitType == ArgLimitType.AllAgesOnly != isR18;
                });

            var tagFiltered = Array.FindAll(argLimitFiltered, (itemInfo) => itemInfo.Tags.Any((tag) => {
                return this._tagSelectPopup._selectedTagName == "" || tag.Name == this._tagSelectPopup._selectedTagName;
            }));

            return tagFiltered;
        }

        private void LoadImageFromUrlAsync(string url, Action<Texture2D> onCompleted) {
            if (this._imageCache.ContainsKey(url)) {
                onCompleted?.Invoke(this._imageCache[url]);
            } else {
                var request = UnityWebRequestTexture.GetTexture(url);
                request.SendWebRequest().completed += operation => {
                    if (request.result == UnityWebRequest.Result.Success) {
                        var texture = DownloadHandlerTexture.GetContent(request);
                        onCompleted?.Invoke(texture);

                        if (!this._imageCache.ContainsKey(url)) {
                            this._imageCache.Add(url, texture);
                        }
                    } else {
                        Debug.LogError($"Failed to load image: {request.error}");
                    }
                };
            }
        }
    }
}