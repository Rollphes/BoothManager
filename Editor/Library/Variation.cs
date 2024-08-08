using System;

using io.github.rollphes.epmanager.booth;

namespace io.github.rollphes.epmanager.library {
    internal class Variation {
        internal long Id { get; private set; }
        internal string Name { get; private set; }
        internal long Price { get; private set; }
        internal Uri OrderUrl { get; private set; }
        internal bool IsBought { get; private set; }
        //internal Package[] Packages { get; private set; }

        internal Variation(VariationInfo info) {
            this.Id = info.Id;
            this.Name = info.Name;
            this.Price = info.Price;
            this.OrderUrl = info.OrderUrl;
            this.IsBought = info.OrderUrl != null;
        }
    }
}
