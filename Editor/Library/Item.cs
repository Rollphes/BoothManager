using System;
using System.Linq;

using io.github.rollphes.epmanager.booth;

namespace io.github.rollphes.epmanager.library {
    internal class Item {
        internal long Id { get; private set; }
        internal string Name { get; private set; }
        internal string Description { get; private set; }
        internal Tag[] Tags { get; private set; }
        internal Category Category { get; set; }
        internal Shop Shop { get; private set; }
        internal long WishListsCount { get; private set; }
        internal bool IsWished { get; private set; }
        internal Image[] Images { get; private set; }
        internal Uri Url { get; private set; }
        internal Variation[] Variations { get; private set; }

        private readonly ItemInfo _info;

        internal Item(ItemInfo info) {
            this.Id = info.Id;
            this.Name = info.Name;
            this.Description = info.Description;
            this.Tags = info.Tags;
            this.Category = info.Category;
            this.Shop = info.Shop;
            this.WishListsCount = info.WishListsCount;
            this.IsWished = info.Wished;
            this.Images = info.Images;
            this.Url = info.Url;
            this.Variations = info.Variations.Select((variationInfo) => new Variation(variationInfo)).ToArray();

            this._info = info;
        }
    }
}
