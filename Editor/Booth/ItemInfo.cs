using System;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace io.github.rollphes.epmanager.booth {
    internal partial class ItemInfo {
        [JsonProperty("description")]
        internal string Description { get; set; }

        [JsonProperty("factory_description")]
        internal object FactoryDescription { get; set; }

        [JsonProperty("id")]
        internal long Id { get; set; }

        [JsonProperty("is_adult")]
        internal bool IsAdult { get; set; }

        [JsonProperty("is_buyee_possible")]
        internal bool IsBuyeePossible { get; set; }

        [JsonProperty("is_end_of_sale")]
        internal bool IsEndOfSale { get; set; }

        [JsonProperty("is_placeholder")]
        internal bool IsPlaceholder { get; set; }

        [JsonProperty("is_sold_out")]
        internal bool IsSoldOut { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("price")]
        internal string Price { get; set; }

        [JsonProperty("purchase_limit")]
        internal object PurchaseLimit { get; set; }

        [JsonProperty("shipping_info")]
        internal string ShippingInfo { get; set; }

        [JsonProperty("small_stock")]
        internal object SmallStock { get; set; }

        [JsonProperty("url")]
        internal Uri Url { get; set; }

        [JsonProperty("wish_list_url")]
        internal Uri WishListUrl { get; set; }

        [JsonProperty("wish_lists_count")]
        internal long WishListsCount { get; set; }

        [JsonProperty("wished")]
        internal bool Wished { get; set; }

        [JsonProperty("buyee_variations")]
        internal object[] BuyeeVariations { get; set; }

        [JsonProperty("category")]
        internal Category Category { get; set; }

        [JsonProperty("embeds")]
        internal string[] Embeds { get; set; }

        [JsonProperty("images")]
        internal Image[] Images { get; set; }

        [JsonProperty("order")]
        internal Order Order { get; set; }

        [JsonProperty("gift")]
        internal object Gift { get; set; }

        [JsonProperty("report_url")]
        internal Uri ReportUrl { get; set; }

        [JsonProperty("share")]
        internal Share Share { get; set; }

        [JsonProperty("shop")]
        internal Shop Shop { get; set; }

        [JsonProperty("sound")]
        internal object Sound { get; set; }

        [JsonProperty("tags")]
        internal Tag[] Tags { get; set; }

        [JsonProperty("tag_banners")]
        internal TagBanner[] TagBanners { get; set; }

        [JsonProperty("tag_combination")]
        internal TagCombination TagCombination { get; set; }

        [JsonProperty("tracks")]
        internal object Tracks { get; set; }

        [JsonProperty("variations")]
        internal Variation[] Variations { get; set; }
    }

    internal partial class Category {
        [JsonProperty("id")]
        internal long Id { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("parent")]
        internal Tag Parent { get; set; }

        [JsonProperty("url")]
        internal Uri Url { get; set; }
    }

    internal partial class Tag {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("url")]
        internal Uri Url { get; set; }
    }

    internal partial class Image {
        [JsonProperty("caption")]
        internal object Caption { get; set; }

        [JsonProperty("original")]
        internal Uri Original { get; set; }

        [JsonProperty("resized")]
        internal Uri Resized { get; set; }
    }

    internal partial class Order {
        [JsonProperty("purchased_at")]
        internal string PurchasedAt { get; set; }

        [JsonProperty("url")]
        internal Uri Url { get; set; }
    }

    internal partial class Share {
        [JsonProperty("hashtags")]
        internal string[] Hashtags { get; set; }

        [JsonProperty("text")]
        internal string Text { get; set; }
    }

    internal partial class Shop {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("subdomain")]
        internal string Subdomain { get; set; }

        [JsonProperty("thumbnail_url")]
        internal Uri ThumbnailUrl { get; set; }

        [JsonProperty("url")]
        internal Uri Url { get; set; }

        [JsonProperty("verified")]
        internal bool Verified { get; set; }
    }

    internal partial class TagBanner {
        [JsonProperty("image_url")]
        internal Uri ImageUrl { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("url")]
        internal Uri Url { get; set; }
    }

    internal partial class TagCombination {
        [JsonProperty("category")]
        internal string Category { get; set; }

        [JsonProperty("tag")]
        internal string Tag { get; set; }

        [JsonProperty("url")]
        internal Uri Url { get; set; }
    }

    internal partial class Variation {
        [JsonProperty("buyee_html")]
        internal object BuyeeHtml { get; set; }

        [JsonProperty("downloadable")]
        internal object Downloadable { get; set; }

        [JsonProperty("factory_image_url")]
        internal object FactoryImageUrl { get; set; }

        [JsonProperty("has_download_code")]
        internal bool HasDownloadCode { get; set; }

        [JsonProperty("id")]
        internal long Id { get; set; }

        [JsonProperty("is_anshin_booth_pack")]
        internal bool IsAnshinBoothPack { get; set; }

        [JsonProperty("is_empty_allocatable_stock_with_preorder")]
        internal bool IsEmptyAllocatableStockWithPreorder { get; set; }

        [JsonProperty("is_empty_stock")]
        internal bool IsEmptyStock { get; set; }

        [JsonProperty("is_factory_item")]
        internal bool IsFactoryItem { get; set; }

        [JsonProperty("is_mailbin")]
        internal bool IsMailbin { get; set; }

        [JsonProperty("is_waiting_on_arrival")]
        internal bool IsWaitingOnArrival { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("order_url")]
        internal Uri OrderUrl { get; set; }

        [JsonProperty("price")]
        internal long Price { get; set; }

        [JsonProperty("small_stock")]
        internal object SmallStock { get; set; }

        [JsonProperty("status")]
        internal string Status { get; set; }

        [JsonProperty("type")]
        internal string Type { get; set; }
    }

    internal partial class ItemInfo {
        internal static ItemInfo FromJson(string json) {
            return JsonConvert.DeserializeObject<ItemInfo>(json, Converter.Settings);
        }
    }

    internal static class Serialize {
        internal static string ToJson(this ItemInfo self) {
            return JsonConvert.SerializeObject(self, Converter.Settings);
        }
    }

    internal static class Converter {
        internal static readonly JsonSerializerSettings Settings = new() {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
