// Copyright (c) Pixeval.
// Licensed under the GPL v3 License.

using System.Collections.Generic;

namespace Pixeval.Download.MacroParser;

public interface IMetaPathParser<in TContext>
{
    IReadOnlyList<IMacro> MacroProvider { get; }

    static abstract IMetaPathParser<TContext> Instance { get; }

    string Reduce(string raw, TContext context);
}
