// Copyright (c) Pixeval.
// Licensed under the GPL v3 License.

using System;
using Mako.Model;
using Pixeval.Util;

namespace Pixeval.Controls;

public partial class SpotlightItemViewModel : ThumbnailEntryViewModel<Spotlight>, IFactory<Spotlight, SpotlightItemViewModel>
{
    public static SpotlightItemViewModel CreateInstance(Spotlight entry) => new(entry);

    public SpotlightItemViewModel(Spotlight spotlight) : base(spotlight) => InitializeCommandsBase();

    protected override string ThumbnailUrl => Entry.Thumbnail;

    public override Uri AppUri => MakoHelper.GenerateSpotlightAppUri(Entry.Id);

    public override Uri WebsiteUri => MakoHelper.GenerateSpotlightWebUri(Entry.Id);

    public override Uri PixEzUri => MakoHelper.GenerateSpotlightPixEzUri(Entry.Id);
}
