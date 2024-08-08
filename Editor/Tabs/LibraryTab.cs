using System;
using System.Collections.Generic;

using io.github.rollphes.epmanager.booth;
using io.github.rollphes.epmanager.library;
using io.github.rollphes.epmanager.popups;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

using PopupWindow = UnityEditor.PopupWindow;

namespace io.github.rollphes.epmanager.tabs {
    internal class LibraryTab : TabBase {
        private static readonly VisualTreeAsset _itemPanelUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemPanel");
        private static readonly VisualTreeAsset _itemListLineUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemListLine");
        private static readonly VisualTreeAsset _centerTextBoxUxml = Resources.Load<VisualTreeAsset>("UI/Components/CenterTextBox");
        private static readonly VisualTreeAsset _itemDetailUxml = Resources.Load<VisualTreeAsset>("UI/Components/ItemDetail");

        internal override string Tooltip => "ライブラリ";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Package");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/LibraryTabContent");

        private readonly Dictionary<string, PopupWindowContent> _classNameToPopupDictionary;
        private readonly Dictionary<string, Texture2D> _imageCache = new();
        private float _imageSize = 100f;
        private string _searchText = "";
        private float _itemDetailContentWidth = 200f;
        private Item _selectedItem = null;

        private readonly TagSelectPopup _tagSelectPopup;
        private readonly ShowSelectPopup _showSelectPopup;

        private VisualElement _itemSelectorContent;
        private VisualElement _itemDetailContent;

        internal LibraryTab(MainWindow window) : base(window) {
            this._tagSelectPopup = new();
            this._showSelectPopup = new();
            this._classNameToPopupDictionary = new() {
                { "TagSelectPopupButton", this._tagSelectPopup },
                { "ShowSelectPopupButton", this._showSelectPopup }
            };
        }

        internal override void Show() {
            base.Show();

            this._itemDetailContent = this._tabContent.Q<VisualElement>("ItemDetailContent");
            this._itemDetailContent.style.width = this._itemDetailContentWidth;
            this._itemDetailContent.style.maxWidth = this._itemDetailContentWidth;
            this._itemDetailContent.style.minWidth = this._itemDetailContentWidth;

            float itemDetailInitWidth = 0;
            var isDragging = false;
            var initMousePosition = Vector2.zero;
            var mainContent = this._tabContent.Q<VisualElement>("MainContent");
            this._itemSelectorContent = this._tabContent.Q<VisualElement>("ItemSelectorContent");

            var handle = this._tabContent.Q<VisualElement>("Handle");
            handle.RegisterCallback<MouseDownEvent>(evt => {
                isDragging = true;
                itemDetailInitWidth = this._itemDetailContent.resolvedStyle.width;
                initMousePosition = evt.mousePosition;
                handle.CaptureMouse();
            });
            handle.RegisterCallback<MouseMoveEvent>(evt => {
                if (isDragging) {
                    var delta = initMousePosition.x - evt.mousePosition.x;
                    var newWidth = itemDetailInitWidth + delta;
                    if (newWidth < 50) {
                        newWidth = 50;
                    }
                    if (newWidth > (mainContent.resolvedStyle.width - 50)) {
                        newWidth = mainContent.resolvedStyle.width - 50;
                    }
                    this._itemDetailContent.style.width = newWidth;
                    this._itemDetailContent.style.maxWidth = newWidth;
                    this._itemDetailContent.style.minWidth = newWidth;
                    this._itemDetailContentWidth = newWidth;
                }
            });
            handle.RegisterCallback<MouseUpEvent>(evt => {
                isDragging = false;
                handle.ReleaseMouse();
            });

            mainContent.RegisterCallback<GeometryChangedEvent>(evt => {
                if (this._itemSelectorContent.resolvedStyle.width < 50) {
                    var newWidth = mainContent.resolvedStyle.width - 50;
                    if (newWidth < 50) {
                        newWidth = 50;
                    }
                    this._itemDetailContent.style.width = newWidth;
                    this._itemDetailContent.style.maxWidth = newWidth;
                    this._itemDetailContent.style.minWidth = newWidth;
                    this._itemDetailContentWidth = newWidth;
                }
            });

            var textFilterField = this._tabContent.Q<ToolbarSearchField>("TextFilterField");
            textFilterField.value = this._searchText;
            textFilterField.RegisterValueChangedCallback((evt) => {
                this._searchText = evt.newValue;
                this.ShowItems();
            });

            var imageSizeSlider = this._tabContent.Q<Slider>("ImageSizeSlider");
            imageSizeSlider.SetValueWithoutNotify(this._imageSize);
            imageSizeSlider.RegisterValueChangedCallback((evt) => {
                this._imageSize = evt.newValue;
                this.ShowItems();
            });

            foreach (var (className, popupContent) in this._classNameToPopupDictionary) {
                var toolbarButton = this._tabContent.Q<ToolbarButton>(className);
                toolbarButton.clicked += () => {
                    var buttonWorldBound = toolbarButton.worldBound;
                    var popupRect = new Rect(buttonWorldBound.xMin, buttonWorldBound.yMax, 1, 1);
                    PopupWindow.Show(popupRect, popupContent);
                };
            }

            this._showSelectPopup.OnChangeArgLimitType += (_) => this.ShowItems();
            this._tagSelectPopup.OnChangeSelectedTag += (_) => this.ShowItems();

            this.ShowItems();
            this.ShowSelectItemDetail();
        }

        private void ShowItems() {
            this._itemSelectorContent.Clear();

            if (!BoothClient.IsLoggedIn) {
                _centerTextBoxUxml.CloneTree(this._itemSelectorContent);
                var centerText = this._itemSelectorContent.Q<Label>("CenterText");
                centerText.text = "ログイン後に使用可能です";
                return;
            }

            var items = Library.GetAll(new GetAllItemsOptions {
                SearchText = this._searchText,
                ArgLimitType = this._showSelectPopup.ArgLimitType,
                Tags = this._tagSelectPopup._selectedTag != null ? new Tag[] { this._tagSelectPopup._selectedTag } : new Tag[] { }
            });
            if (items == null || items.Length == 0) {
                _centerTextBoxUxml.CloneTree(this._itemSelectorContent);
                var centerText = this._itemSelectorContent.Q<Label>("CenterText");
                centerText.text = "アイテムがありません";
                return;
            }

            var selectedItemName = this._tabContent.Q<Label>("SelectedItemName");
            selectedItemName.text = this._selectedItem?.Name ?? "";

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            if (this._imageSize > 50) {
                scrollView.contentContainer.style.flexDirection = FlexDirection.Row;
                scrollView.contentContainer.style.flexWrap = Wrap.Wrap;
            }

            var container = scrollView.Q<VisualElement>("unity-content-and-vertical-scroll-container");
            scrollView.RegisterCallback<ClickEvent>((evt) => {
                if (evt.target == scrollView.contentContainer || evt.target == container) {
                    this._selectedItem = null;
                    selectedItemName.text = "";
                    foreach (var child in scrollView.Children()) {
                        child.RemoveFromClassList("MouseOver");
                    }
                    this.ShowSelectItemDetail();
                }
            });

            foreach (var item in items) {
                var root = new VisualElement();
                root.RegisterCallback<ClickEvent>((evt) => {
                    this._selectedItem = item;
                    selectedItemName.text = item.Name;
                    foreach (var child in scrollView.Children()) {
                        child.RemoveFromClassList("MouseOver");
                    }
                    root.AddToClassList("MouseOver");
                    this.ShowSelectItemDetail();
                });

                if (this._selectedItem?.Id == item.Id) {
                    root.AddToClassList("MouseOver");
                    this.ShowSelectItemDetail();
                }

                var itemUxml = (this._imageSize > 50) ? _itemPanelUxml : _itemListLineUxml;
                itemUxml.CloneTree(root);

                var itemImage = root.Q<VisualElement>("ItemImage");
                var itemName = root.Q<Label>("ItemName");
                itemName.text = item.Name;
                root.tooltip = item.Name;

                if (this._imageSize > 50) {
                    itemName.style.width = new StyleLength(new Length(this._imageSize, LengthUnit.Pixel));

                    itemImage.style.width = new StyleLength(new Length(this._imageSize - 10, LengthUnit.Pixel));
                    itemImage.style.height = new StyleLength(new Length(this._imageSize - 10, LengthUnit.Pixel));
                }

                this.LoadImageFromUrlAsync(item.Images[0].Original.ToString(), (texture) => {
                    if (texture != null) {
                        itemImage.style.backgroundImage = new StyleBackground(texture);
                    }
                });

                scrollView.Add(root);
            }

            this._itemSelectorContent.Add(scrollView);
        }

        private void ShowSelectItemDetail() {
            this._itemDetailContent.Clear();
            if (this._selectedItem == null) {
                _centerTextBoxUxml.CloneTree(this._itemDetailContent);
                var centerText = this._itemDetailContent.Q<Label>("CenterText");
                centerText.text = "アイテムが選択されていません";
                return;
            }
            _itemDetailUxml.CloneTree(this._itemDetailContent);

            var itemCategory = this._itemDetailContent.Q<Label>("ItemCategory");
            itemCategory.text = $"{this._selectedItem.Category.Parent.Name} > {this._selectedItem.Category.Name}";

            var shopIcon = this._itemDetailContent.Q<VisualElement>("ShopIcon");
            this.LoadImageFromUrlAsync(this._selectedItem.Shop.ThumbnailUrl.ToString(), (texture) => {
                if (texture != null) {
                    shopIcon.style.backgroundImage = new StyleBackground(texture);
                }
            });

            var shopName = this._itemDetailContent.Q<Label>("ShopName");
            shopName.text = this._selectedItem.Shop.Name;

            var itemName = this._itemDetailContent.Q<Label>("ItemName");
            itemName.text = this._selectedItem.Name;

            var wishListsCount = this._itemDetailContent.Q<Label>("WishListsCount");
            wishListsCount.text = this._selectedItem.WishListsCount.ToString();
        }

        private void LoadImageFromUrlAsync(string url, Action<Texture2D> onCompleted) {
            if (this._imageCache.ContainsKey(url)) {
                onCompleted?.Invoke(this._imageCache[url]);
            } else {
                var request = UnityWebRequestTexture.GetTexture(url);
                request.SendWebRequest().completed += (operation) => {
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