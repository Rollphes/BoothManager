using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using io.github.rollphes.epmanager.booth;
using io.github.rollphes.epmanager.utility;

namespace io.github.rollphes.epmanager.library {
    internal enum LibraryUpdateStatus {
        ItemIdFetchingInLibrary,
        ItemIdFetchingInGift,
        ItemInfoFetching
    }

    internal enum ArgLimitType {
        All,
        AllAgesOnly,
        R18Only
    }

    internal class GetAllItemsOptions {
        internal string SearchText;
        internal ArgLimitType ArgLimitType = ArgLimitType.All;
        internal Tag[] Tags = new Tag[] { };
    }

    internal static class Library {
        internal static Action<LibraryUpdateStatus, int, int> OnUpdateCacheProgress;

        private static readonly Dictionary<long, Item> _itemCache = new();

        internal static Item Get(long itemId) {
            return _itemCache.ContainsKey(itemId)
                ? throw new KeyNotFoundException($"itemId:{itemId} was not found in cache")
                : _itemCache[itemId];
        }

        internal static Item[] GetAll(GetAllItemsOptions options = null) {
            var items = _itemCache.Values.ToArray();
            if (options == null) {
                return items;
            } else {
                var textFiltered = string.IsNullOrEmpty(options.SearchText)
                    ? items
                    : Array.FindAll(items, (item) => {
                        var normalizedItemName = Utility.ConvertToSearchText(item.Name);
                        var normalizedFilter = Utility.ConvertToSearchText(options.SearchText);
                        return Regex.IsMatch(normalizedItemName, normalizedFilter);
                    });

                var argLimitFiltered = options.ArgLimitType == ArgLimitType.All
                    ? textFiltered
                    : Array.FindAll(textFiltered, (item) => {
                        var isR18 = item.Tags.Any((tag) => tag.Name == "R18");
                        return options.ArgLimitType == ArgLimitType.AllAgesOnly != isR18;
                    });

                var tagFiltered = options.Tags.Count() == 0
                    ? argLimitFiltered
                    : Array.FindAll(argLimitFiltered, (item) => item.Tags.Any((tag) => tag.Name == options.Tags[0].Name));

                return tagFiltered;
            }
        }

        internal static Tag[] GetTags(string searchText = null) {
            var tags = GetAll().SelectMany((item) => item.Tags).ToArray();
            var distinctTagList = new List<Tag>();

            foreach (var tag in tags) {
                if (!distinctTagList.Select((tag) => tag.Name).ToList().Contains(tag.Name)) {
                    distinctTagList.Add(tag);
                }
            }

            return searchText == null
                ? distinctTagList.ToArray()
                : Array.FindAll(distinctTagList.ToArray(), (tag) => {
                    var normalizedItemName = Utility.ConvertToSearchText(tag.Name);
                    var normalizedFilter = Utility.ConvertToSearchText(searchText);
                    return Regex.IsMatch(normalizedItemName, normalizedFilter);
                });
        }

        internal static async Task UpdateCache() {
            _itemCache.Clear();

            var itemInfos = await BoothClient.FetchItemInfos((status, index, length) => {
                switch (status) {
                    case FetchItemInfoStatus.ItemIdFetchingInGift:
                        OnUpdateCacheProgress?.Invoke(LibraryUpdateStatus.ItemIdFetchingInGift, index, length);
                        break;
                    case FetchItemInfoStatus.ItemIdFetchingInLibrary:
                        OnUpdateCacheProgress?.Invoke(LibraryUpdateStatus.ItemIdFetchingInLibrary, index, length);
                        break;
                    case FetchItemInfoStatus.ItemInfoFetching:
                        OnUpdateCacheProgress?.Invoke(LibraryUpdateStatus.ItemInfoFetching, index, length);
                        break;
                }
            });
            foreach (var itemInfo in itemInfos) {
                _itemCache.Add(itemInfo.Id, new Item(itemInfo));
            }
        }
    }
}
