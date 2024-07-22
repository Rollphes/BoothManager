

namespace io.github.rollphes.boothManager.types.api {
    using System;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class ItemInfo {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("factory_description")]
        public object FactoryDescription { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_adult")]
        public bool IsAdult { get; set; }

        [JsonProperty("is_buyee_possible")]
        public bool IsBuyeePossible { get; set; }

        [JsonProperty("is_end_of_sale")]
        public bool IsEndOfSale { get; set; }

        [JsonProperty("is_placeholder")]
        public bool IsPlaceholder { get; set; }

        [JsonProperty("is_sold_out")]
        public bool IsSoldOut { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("purchase_limit")]
        public object PurchaseLimit { get; set; }

        [JsonProperty("shipping_info")]
        public string ShippingInfo { get; set; }

        [JsonProperty("small_stock")]
        public object SmallStock { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("wish_list_url")]
        public Uri WishListUrl { get; set; }

        [JsonProperty("wish_lists_count")]
        public long WishListsCount { get; set; }

        [JsonProperty("wished")]
        public bool Wished { get; set; }

        [JsonProperty("buyee_variations")]
        public object[] BuyeeVariations { get; set; }

        [JsonProperty("category")]
        public Category Category { get; set; }

        [JsonProperty("embeds")]
        public string[] Embeds { get; set; }

        [JsonProperty("images")]
        public Image[] Images { get; set; }

        [JsonProperty("order")]
        public Order Order { get; set; }

        [JsonProperty("gift")]
        public object Gift { get; set; }

        [JsonProperty("report_url")]
        public Uri ReportUrl { get; set; }

        [JsonProperty("share")]
        public Share Share { get; set; }

        [JsonProperty("shop")]
        public Shop Shop { get; set; }

        [JsonProperty("sound")]
        public object Sound { get; set; }

        [JsonProperty("tags")]
        public Parent[] Tags { get; set; }

        [JsonProperty("tag_banners")]
        public TagBanner[] TagBanners { get; set; }

        [JsonProperty("tag_combination")]
        public TagCombination TagCombination { get; set; }

        [JsonProperty("tracks")]
        public object Tracks { get; set; }

        [JsonProperty("variations")]
        public Variation[] Variations { get; set; }
    }

    public partial class Category {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parent")]
        public Parent Parent { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Parent {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Image {
        [JsonProperty("caption")]
        public object Caption { get; set; }

        [JsonProperty("original")]
        public Uri Original { get; set; }

        [JsonProperty("resized")]
        public Uri Resized { get; set; }
    }

    public partial class Order {
        [JsonProperty("purchased_at")]
        public string PurchasedAt { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Share {
        [JsonProperty("hashtags")]
        public string[] Hashtags { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public partial class Shop {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("subdomain")]
        public string Subdomain { get; set; }

        [JsonProperty("thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }
    }

    public partial class TagBanner {
        [JsonProperty("image_url")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class TagCombination {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Variation {
        [JsonProperty("buyee_html")]
        public object BuyeeHtml { get; set; }

        [JsonProperty("downloadable")]
        public object Downloadable { get; set; }

        [JsonProperty("factory_image_url")]
        public object FactoryImageUrl { get; set; }

        [JsonProperty("has_download_code")]
        public bool HasDownloadCode { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_anshin_booth_pack")]
        public bool IsAnshinBoothPack { get; set; }

        [JsonProperty("is_empty_allocatable_stock_with_preorder")]
        public bool IsEmptyAllocatableStockWithPreorder { get; set; }

        [JsonProperty("is_empty_stock")]
        public bool IsEmptyStock { get; set; }

        [JsonProperty("is_factory_item")]
        public bool IsFactoryItem { get; set; }

        [JsonProperty("is_mailbin")]
        public bool IsMailbin { get; set; }

        [JsonProperty("is_waiting_on_arrival")]
        public bool IsWaitingOnArrival { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("order_url")]
        public Uri OrderUrl { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }

        [JsonProperty("small_stock")]
        public object SmallStock { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class ItemInfo {
        public static ItemInfo FromJson(string json) => JsonConvert.DeserializeObject<ItemInfo>(json, Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this ItemInfo self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter {
        public static readonly JsonSerializerSettings Settings = new() {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
