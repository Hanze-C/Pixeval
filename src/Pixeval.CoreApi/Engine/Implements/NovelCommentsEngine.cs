#region Copyright

// GPL v3 License
// 
// Pixeval/Pixeval.CoreApi
// Copyright (c) 2024 Pixeval.CoreApi/NovelCommentsEngine.cs
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
using System.Threading;
using Pixeval.CoreApi.Model;
using Pixeval.CoreApi.Net;

namespace Pixeval.CoreApi.Engine.Implements;

public class NovelCommentsEngine(long novelId, MakoClient makoClient, EngineHandle? engineHandle)
    : AbstractPixivFetchEngine<Comment>(makoClient, engineHandle)
{
    private readonly long _novelId = novelId;

    public override IAsyncEnumerator<Comment> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
    {
        return RecursivePixivAsyncEnumerators.Comment<NovelCommentsEngine>.WithInitialUrl(this, MakoApiKind.AppApi,
            engine => $"/v3/novel/comments?novel_id={engine._novelId}")!;
    }
}
