#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2023 Pixeval/SettingsPageViewModel.cs
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

using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Pixeval.AppManagement;
using Pixeval.Attributes;
using Pixeval.Controls.IllustrationView;
using Pixeval.Controls.TokenInput;
using Pixeval.Database.Managers;
using Pixeval.Download;
using Pixeval.Download.MacroParser;
using Pixeval.Misc;
using Pixeval.SettingsModels;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;

namespace Pixeval.Pages.Misc;

[SettingsViewModel<AppSetting>(nameof(_appSetting))]
public partial class SettingsPageViewModel(AppSetting appSetting) : ObservableObject
{
    public static readonly IEnumerable<string> AvailableFonts = new InstalledFontCollection().Families.Select(f => f.Name);

    public static readonly ICollection<Token> AvailableIllustMacros;

    private readonly AppSetting _appSetting = appSetting;

    /// <summary>
    /// 写成字段防止被反射
    /// </summary>
    public TeachingTip SettingsTeachingTip = null!;

    static SettingsPageViewModel()
    {
        using var scope = App.AppViewModel.AppServicesScope;
        var factory = scope.ServiceProvider.GetRequiredService<IDownloadTaskFactory<IllustrationItemViewModel, IllustrationDownloadTask>>();
        AvailableIllustMacros = factory.PathParser.MacroProvider.AvailableMacros
            .Select(m => $"@{{{(m is IMacro<IllustrationItemViewModel>.IPredicate ? $"{m.Name}:" : m.Name)}}}")
            .Select(s => new Token(s, false, false))
            .ToList();
    }

    [DefaultValue(false)]
    public bool DisableDomainFronting
    {
        get => _appSetting.DisableDomainFronting;
        set => SetProperty(_appSetting.DisableDomainFronting, value, _appSetting, (setting, value) =>
        {
            setting.DisableDomainFronting = value;
            App.AppViewModel.MakoClient.Configuration.Bypass = !value;
        });
    }

    [DefaultValue(null)]
    public string? MirrorHost
    {
        get => _appSetting.MirrorHost;
        set => SetProperty(_appSetting.MirrorHost, value, _appSetting, (setting, value) =>
        {
            setting.MirrorHost = value;
            App.AppViewModel.MakoClient.Configuration.MirrorHost = value;
        });
    }

    [DefaultValue(typeof(DownloadConcurrencyDefaultValueProvider))]
    public int MaxDownloadTaskConcurrencyLevel
    {
        get => _appSetting.MaxDownloadTaskConcurrencyLevel;
        set => SetProperty(_appSetting.MaxDownloadTaskConcurrencyLevel, value, _appSetting, (setting, value) =>
        {
            App.AppViewModel.DownloadManager.ConcurrencyDegree = value;
            setting.MaxDownloadTaskConcurrencyLevel = value;
        });
    }

    public DateTimeOffset GetMinSearchEndDate(DateTimeOffset startDate)
    {
        return startDate.AddDays(1);
    }

    public string GetLastUpdateCheckDisplayString(DateTimeOffset lastChecked)
    {
        return $"{SettingsPageResources.LastCheckedPrefix}{lastChecked.ToString(CultureInfo.CurrentUICulture)}";
    }

    public void ResetDefault()
    {
        foreach (var propertyInfo in typeof(SettingsPageViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            propertyInfo.SetValue(this, propertyInfo.GetDefaultValue());
        }
    }

    public void ClearData<T, TModel>(ClearDataKind kind, IPersistentManager<T, TModel> manager) where T : new()
    {
        manager.Clear();
        SettingsTeachingTip.ShowAndHide(kind switch
        {
            ClearDataKind.BrowseHistory => SettingsPageResources.BrowseHistoriesCleared,
            ClearDataKind.SearchHistory => SettingsPageResources.SearchHistoriesCleared,
            ClearDataKind.DownloadHistory => SettingsPageResources.DownloadHistoriesCleared,
            _ => ThrowHelper.ArgumentOutOfRange<ClearDataKind, string>(kind)
        });
    }
}
