using Microsoft.AspNetCore.Http;
using System.Net;

namespace TileMapService
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Returns true if request is local, otherwise false.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <returns>True if request is local, otherwise false.</returns>
        public static bool IsLocal(this HttpRequest request)
        {
            // Request.IsLocal in ASP.NET Core
            // https://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/

            var connection = request.HttpContext.Connection;
            if (connection.RemoteIpAddress != null)
            {
                return connection.LocalIpAddress != null
                    ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                    : IPAddress.IsLoopback(connection.RemoteIpAddress);
            }
            else if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
