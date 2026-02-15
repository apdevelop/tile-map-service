using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.WebUtilities;

namespace TileMapService.Utils
{
    static class UrlHelper
    {
        public static string GetQueryBase(string url)
        {
            var uri = new Uri(url);
            var baseUri = uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);

            return baseUri;
        }

        public static List<KeyValuePair<string, string>> GetQueryParameters(string url)
        {
            var uri = new Uri(url);
            var items = QueryHelpers.ParseQuery(uri.Query)
                .SelectMany(
                    kvp => kvp.Value,
                    (kvp, value) => new KeyValuePair<string, string>(kvp.Key.ToLower(), value ?? string.Empty))
                .ToList();

            return items;
        }

        public static string[] GetSegments(string url)
        {
            var uri = new Uri(url);
            var result = uri.Segments;

            return result;
        }
    }
}
