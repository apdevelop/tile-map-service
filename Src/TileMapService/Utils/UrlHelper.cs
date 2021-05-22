using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TileMapService.Utils
{
    static class UrlHelper
    {
        public static string GetQueryBase(string baseUrl)
        {
            var uri = new Uri(baseUrl);
            var baseUri = uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);

            return baseUri;
        }

        public static List<KeyValuePair<string, string>> GetQueryParameters(string baseUrl)
        {
            var uri = new Uri(baseUrl);
            var queryDictionary = QueryHelpers.ParseQuery(uri.Query);
            var items = queryDictionary
                .SelectMany(
                    kvp => kvp.Value,
                    (kvp, value) => new KeyValuePair<string, string>(kvp.Key.ToLower(), value))
                .ToList();

            return items;
        }
    }
}
