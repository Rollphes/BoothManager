using UnityEngine;
using UnityEngine.UIElements;
using io.github.rollphes.boothManager.client;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using UnityEditor.UIElements;
using System.Text.RegularExpressions;
using System.Text;

namespace io.github.rollphes.boothManager.tabs {
    internal class ByOrderTab : TabBase {
        private static readonly VisualTreeAsset _itemPanelUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemPanel");
        private static readonly VisualTreeAsset _itemListLineUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemListLine");
        internal override string Tooltip => "�w������";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Default");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/ByOrderTabContent");

        private VisualElement _byOrderConetnt;
        private Slider _imageSizeSlider;
        private ToolbarSearchField _textFilterField;

        private float _imageSize = 100f;

        private readonly Dictionary<string, Texture2D> _imageCache = new();

        public ByOrderTab(Client client, TabController tabController, VisualElement tabContent) : base(client, tabController, tabContent) { }

        internal override void Show() {
            base.Show();
            this._byOrderConetnt = this._tabContent.Q<VisualElement>("ByOrderContent");
            this._textFilterField = this._tabContent.Q<ToolbarSearchField>("TextFilterField");
            this._textFilterField.RegisterValueChangedCallback(evt => this.ShowItemInfos());

            this._imageSizeSlider = this._tabContent.Q<Slider>("ImageSizeSlider");
            this._imageSizeSlider.RegisterValueChangedCallback(evt => {
                this._imageSize = evt.newValue;
                this.ShowItemInfos();
            });
            this._imageSizeSlider.SetValueWithoutNotify(this._imageSize);

            this.ShowItemInfos();
        }

        private void ShowItemInfos() {
            var itemInfos = this._client.FetchItemInfos().Result;
            var textFilteredItemInfos = Array.FindAll(itemInfos, (itemInfo) => {
                var normalizedItemName = itemInfo.Name.Normalize(NormalizationForm.FormKD);
                var normalizedFilter = this._textFilterField.value.Normalize(NormalizationForm.FormKD);
                return Regex.IsMatch(normalizedItemName, normalizedFilter);
            });


            var scrollView = new ScrollView();
            if (this._imageSize > 50) {
                scrollView.contentContainer.style.flexDirection = FlexDirection.Row;
                scrollView.contentContainer.style.flexWrap = Wrap.Wrap;
            }
            scrollView.contentContainer.style.flexGrow = 0;
            foreach (var itemInfo in textFilteredItemInfos) {
                var root = new VisualElement();
                if (this._imageSize > 50) {
                    _itemPanelUxml.CloneTree(root);
                } else {
                    _itemListLineUxml.CloneTree(root);
                }
                var itemImage = root.Q<VisualElement>("ItemImage");
                var itemName = root.Q<Label>("ItemName");
                root.tooltip = itemInfo.Name;
                itemName.text = itemInfo.Name;

                if (this._imageSize > 50) {
                    itemName.style.width = new StyleLength(new Length(this._imageSize, LengthUnit.Pixel));

                    itemImage.style.width = new StyleLength(new Length(this._imageSize - 10, LengthUnit.Pixel));
                    itemImage.style.height = new StyleLength(new Length(this._imageSize - 10, LengthUnit.Pixel));
                }

                this.LoadImageFromUrlAsync(itemInfo.Images[0].Original.ToString(), (texture) => {
                    if (texture != null) {
                        var backgroundImage = new StyleBackground(texture);
                        itemImage.style.backgroundImage = backgroundImage;
                    }
                });
                scrollView.Add(root);
            }
            this._byOrderConetnt.Clear();
            this._byOrderConetnt.Add(scrollView);
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