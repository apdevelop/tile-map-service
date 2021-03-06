﻿using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    class LocalFilesTileSource : ITileSource
    {
        private TileSourceConfiguration configuration;

        public LocalFilesTileSource(TileSourceConfiguration configuration)
        {
            this.configuration = configuration; // May be changed later in InitAsync
        }

        #region ITileSource implementation

        async Task ITileSource.InitAsync()
        {
            if (String.IsNullOrEmpty(this.configuration.Name))
            {
                throw new InvalidOperationException();
            }

            if (String.IsNullOrEmpty(this.configuration.Location))
            {
                throw new InvalidOperationException();
            }

            // TODO: configuration values priority:
            // 1. Default values for local files.
            // 2. Actual values (from first found tile properties).
            // 3. Values from configuration file - overrides given above, if provided.

            this.configuration = new TileSourceConfiguration
            {
                Name = this.configuration.Name,
                Format = this.configuration.Format, // TODO: from file properties
                Title = this.configuration.Title,
                Tms = this.configuration.Tms ?? false,
                Location = this.configuration.Location,
                ContentType = Utils.TileFormatToContentType(this.configuration.Format), // TODO: from file properties
            };

            await Task.Delay(0); // TODO: implement
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            var path = GetLocalFilePath(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z);
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                using (var fileStream = fileInfo.OpenRead())
                {
                    var buffer = new byte[fileInfo.Length];
                    await fileStream.ReadAsync(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            else
            {
                return null;
            }
        }

        TileSourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion

        private static string GetLocalFilePath(string template, int x, int y, int z)
        {
            var uriString = template
                            .Replace("{x}", x.ToString(CultureInfo.InvariantCulture))
                            .Replace("{y}", y.ToString(CultureInfo.InvariantCulture))
                            .Replace("{z}", z.ToString(CultureInfo.InvariantCulture));

            return new Uri(uriString).LocalPath;
        }
    }
}