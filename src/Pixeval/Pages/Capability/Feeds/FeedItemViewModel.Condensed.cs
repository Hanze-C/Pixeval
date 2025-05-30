// Copyright (c) Pixeval.
// Licensed under the GPL v3 License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Mako.Model;
using Pixeval.Util.IO.Caching;
using WinUI3Utilities;

namespace Pixeval.Pages.Capability.Feeds;

public partial class FeedItemCondensedViewModel(List<Feed?> entries) : AbstractFeedItemViewModel(new IFeedEntry.CondensedFeedEntry(entries))
{
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override Uri AppUri => ThrowHelper.NotSupported<Uri>("AppUri is not supported for condensed feeds");

    public override Uri WebsiteUri => ThrowHelper.NotSupported<Uri>("WebUri is not supported for condensed feeds");

    public override Uri PixEzUri => ThrowHelper.NotSupported<Uri>("PixEzUri is not supported for condensed feeds");

    [ObservableProperty]
    public override partial ImageSource? UserAvatar { get; protected set; }

    [ObservableProperty]
    public override partial SolidColorBrush ItemBackground { get; set; } = new(Colors.Transparent);

    public override string PostUsername => entries[0]?.PostUsername ?? string.Empty;

    public override string PostDateFormatted => $"{FormatDate(entries[^1]?.PostDate ?? default)} ~ {FormatDate(entries[0]?.PostDate ?? default)}";

    private static string FormatDate(DateTimeOffset postDate)
    {
        return (DateTime.Now - postDate) < TimeSpan.FromDays(1)
            ? postDate.ToString("hh:mm tt")
            : postDate.ToString("M");
    }

    public override async Task LoadAsync()
    {
        if (UserAvatar is not null)
            return;

        if (entries[0]?.PostUserThumbnail is { Length: > 0 } url)
            UserAvatar = await CacheHelper.GetSourceFromCacheAsync(url, desiredWidth: 35);
        else
            UserAvatar = await CacheHelper.ImageNotAvailableTask.Value;
    }
}
