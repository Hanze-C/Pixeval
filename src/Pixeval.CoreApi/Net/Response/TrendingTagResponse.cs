﻿#region Copyright (c) Pixeval/Pixeval.CoreApi
// GPL v3 License
// 
// Pixeval/Pixeval.CoreApi
// Copyright (c) 2023 Pixeval.CoreApi/TrendingTagResponse.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Pixeval.CoreApi.Model;

namespace Pixeval.CoreApi.Net.Response;

// ReSharper disable UnusedAutoPropertyAccessor.Global
internal class TrendingTagResponse
{
    [JsonPropertyName("trend_tags")]
    public required IEnumerable<TrendTag> TrendTags { get; set; }

    public class TrendTag
    {
        [JsonPropertyName("tag")]
        public required string TagStr { get; set; }

        [JsonPropertyName("translated_name")]
        public required string TranslatedName { get; set; }

        [JsonPropertyName("illust")]
        public required Illustration Illust { get; set; }
    }
}