using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using io.github.rollphes.boothManager.client;

using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace io.github.rollphes.boothManager.tabs {
    internal class LibraryTab : TabBase {
        private static readonly VisualTreeAsset _itemPanelUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemPanel");
        private static readonly VisualTreeAsset _itemListLineUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemListLine");

        internal override string Tooltip => "ライブラリ";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Package");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/LibraryTabContent");

        private readonly Dictionary<string, Texture2D> _imageCache = new();
        private float _imageSize = 100f;

        private VisualElement _libraryContent;
        private Slider _imageSizeSlider;
        private ToolbarSearchField _textFilterField;

        public LibraryTab(Client client, TabController tabController, VisualElement tabContent) : base(client, tabController, tabContent) { }

        internal override void Show() {
            base.Show();

            this._libraryContent = this._tabContent.Q<VisualElement>("LibraryContent");

            this._textFilterField = this._tabContent.Q<ToolbarSearchField>("TextFilterField");
            this._textFilterField.RegisterValueChangedCallback(evt => this.ShowItemInfos());

            this._imageSizeSlider = this._tabContent.Q<Slider>("ImageSizeSlider");
            this._imageSizeSlider.SetValueWithoutNotify(this._imageSize);
            this._imageSizeSlider.RegisterValueChangedCallback(evt => {
                this._imageSize = evt.newValue;
                this.ShowItemInfos();
            });

            this.ShowItemInfos();
        }

        private void ShowItemInfos() {
            var itemInfos = this._client.FetchItemInfos().Result;

            var textFilteredItemInfos = Array.FindAll(itemInfos, (itemInfo) => {
                var normalizedItemName = this.ConvertToSearchText(itemInfo.Name);
                var normalizedFilter = this.ConvertToSearchText(this._textFilterField.value);
                return Regex.IsMatch(normalizedItemName, normalizedFilter);
            });

            var scrollView = new ScrollView();
            scrollView.contentContainer.style.flexGrow = 0;
            if (this._imageSize > 50) {
                scrollView.contentContainer.style.flexDirection = FlexDirection.Row;
                scrollView.contentContainer.style.flexWrap = Wrap.Wrap;
            }

            foreach (var itemInfo in textFilteredItemInfos) {
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

        private string ConvertToSearchText(string input) {
            // Convert to NFKD & Lower
            var s = input.Normalize(NormalizationForm.FormKD).ToLower();

            // Convert to Kana
            var sb = new StringBuilder();
            var target = s.ToCharArray();
            char c;
            for (var i = 0; i < target.Length; i++) {
                c = target[i];
                if (c is >= 'ぁ' and <= 'ヴ') {
                    c = (char)(c + 0x0060);
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}