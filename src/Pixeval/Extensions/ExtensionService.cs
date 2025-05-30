// Copyright (c) Pixeval.
// Licensed under the GPL v3 License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Pixeval.AppManagement;
using Pixeval.Extensions.Common;
using Pixeval.Extensions.Common.Commands.Transformers;
using Pixeval.Extensions.Common.Downloaders;
using Pixeval.Extensions.Common.Settings;
using Pixeval.Extensions.Models;
using Pixeval.Utilities;
using Windows.Win32;
using Pixeval.Extensions.Common.FormatProviders;
using WinUI3Utilities;

namespace Pixeval.Extensions;

public partial class ExtensionService : IDisposable
{
    public ObservableCollection<ExtensionsHostModel> HostModels { get; } = [];

    public IEnumerable<ExtensionsHostModel> ActiveModels => HostModels.Where(t => t.IsActive);

    public IReadOnlyList<ExtensionSettingsGroup> SettingsGroups => _settingsGroups;

    public IReadOnlyList<IExtension> Extensions => HostModels
        .Aggregate(new List<IExtension>(), (o, t) => o.Apply(p => p.AddRange(t.Extensions)));

    public IReadOnlyList<IExtension> ActiveExtensions => ActiveModels
        .Aggregate(new List<IExtension>(), (o, t) => o.Apply(p => p.AddRange(t.Extensions)));

    public IEnumerable<IImageTransformerCommandExtension> ActiveImageTransformerCommands => ActiveExtensions.OfType<IImageTransformerCommandExtension>();

    public IEnumerable<ITextTransformerCommandExtension> ActiveTextTransformerCommands => ActiveExtensions.OfType<ITextTransformerCommandExtension>();

    public IEnumerable<IDownloaderExtension> ActiveDownloaders => ActiveExtensions.OfType<IDownloaderExtension>();

    public IEnumerable<IImageFormatProviderExtension> ActiveImageFormatProviders => ActiveExtensions.OfType<IImageFormatProviderExtension>();

    public IEnumerable<INovelFormatProviderExtension> ActiveNovelFormatProviders => ActiveExtensions.OfType<INovelFormatProviderExtension>();

    private readonly List<ExtensionSettingsGroup> _settingsGroups = [];

    public int OutDateExtensionHostsCount { get; private set; }

    public void LoadAllHosts()
    {
        foreach (var dll in AppKnownFolders.Extensions.GetFiles("*.dll"))
        {
            _ = TryLoadHost(dll, out var isOutDate);
            if (isOutDate)
                ++OutDateExtensionHostsCount;
        }
    }

    public bool TryLoadHost(string path, out bool isOutdated)
    {
        isOutdated = false;
        try
        {
            if (LoadHost(path, out isOutdated) is not { } model)
                return false;
            LoadExtensions(model);
            var inserted = false;
            for (var i = HostModels.Count; i > 0; --i)
                if (HostModels[i - 1].Priority < model.Priority)
                {
                    HostModels.Insert(i, model);
                    inserted = true;
                    break;
                }

            if (!inserted)
                HostModels.Insert(0, model);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void UnloadHost(ExtensionsHostModel model)
    {
        _ = HostModels.Remove(model);
        if (_settingsGroups.FirstOrDefault(t => t.Model == model) is { } group)
            _ = _settingsGroups.Remove(group);
        foreach (var extension in model.Extensions)
            extension.OnExtensionUnloaded();
        model.Dispose();
    }

    private static ExtensionsHostModel? LoadHost(string path, out bool isOutdated)
    {
        isOutdated = false;
        try
        {
            var dllHandle = PInvoke.LoadLibrary(path);
            if (dllHandle is null)
                return null;
            try
            {
                var dllGetExtensionsHostPtr =
                    PInvoke.GetProcAddress(dllHandle, nameof(IExtensionsHost.DllGetExtensionsHost));
                if ((nint) dllGetExtensionsHostPtr is 0)
                    return null;
                var dllGetExtensionsHost = Marshal.GetDelegateForFunctionPointer<IExtensionsHost.DllGetExtensionsHost>(dllGetExtensionsHostPtr);
                var result = dllGetExtensionsHost(out var ppv);
                if (result is not 0)
                    return null;
                var wrappers = new StrategyBasedComWrappers();
                var rcw = (IExtensionsHost) wrappers.GetOrCreateObjectForComInstance(ppv, CreateObjectFlags.UniqueInstance);
                _ = Marshal.Release(ppv);

                if (rcw.GetSdkVersion() != IExtensionsHost.SdkVersion.ToString())
                {
                    dllHandle.Dispose();
                    isOutdated = true;
                    return null;
                }
                rcw.Initialize(AppSettings.CurrentCulture.Name, AppKnownFolders.Temp.FullPath, AppKnownFolders.Extensions.FullPath);
                return new(rcw) { Handle = dllHandle };
            }
            catch
            {
                dllHandle.Dispose();
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private void LoadExtensions(ExtensionsHostModel model)
    {
        var extensions = model.Extensions;
        LoadSubExtensions(extensions);
        LoadSettingsExtension(model, extensions);
    }

    private void LoadSubExtensions(IEnumerable<IExtension> extensions)
    {
        foreach (var extension in extensions)
            extension.OnExtensionLoaded();
    }

    private void LoadSettingsExtension(ExtensionsHostModel model, IEnumerable<IExtension> extensions)
    {
        var converter = new SettingsValueConverter();
        var extensionSettingsGroup = new ExtensionSettingsGroup(model);
        _settingsGroups.Add(extensionSettingsGroup);
        var values = model.Values;
        var settingsExtensions = extensions.OfType<ISettingsExtension>();
        foreach (var settingsExtension in settingsExtensions)
        {
            var token = settingsExtension.GetToken();
            switch (settingsExtension)
            {
                case IStringSettingsExtension i:
                {
                    if (values.TryGetValue(token, out var value) && value is string v)
                        i.OnValueChanged(v);
                    else
                        values[token] = v = i.GetDefaultValue();
                    extensionSettingsGroup.Add(new ExtensionStringSettingsEntry(i, v));
                    break;
                }
                case IIntOrEnumSettingsExtension i:
                {
                    if (values.TryGetValue(token, out var value) && value is int v)
                        i.OnValueChanged(v);
                    else
                        values[token] = v = i.GetDefaultValue();
                    switch (i)
                    {
                        case IIntSettingsExtension a:
                            extensionSettingsGroup.Add(new ExtensionIntSettingsEntry(a, v));
                            break;
                        case IEnumSettingsExtension b:
                            extensionSettingsGroup.Add(new ExtensionEnumSettingsEntry(b, v));
                            break;
                    }
                    break;
                }
                case IColorSettingsExtension i:
                {
                    if (values.TryGetValue(token, out var value) && value is uint v)
                        i.OnValueChanged(v);
                    else
                        values[token] = v = i.GetDefaultValue();
                    extensionSettingsGroup.Add(new ExtensionColorSettingsEntry(i, v));
                    break;
                }
                case IStringsArraySettingsExtension i:
                {
                    string[] v;
                    if (values.TryGetValue(token, out var value)
                        && value is string s
                        && converter.TryConvertBack<string[]>(s, false, out var resultStringArray))
                    {
                        v = resultStringArray!;
                        i.OnValueChanged(v);
                    }
                    else
                    {
                        v = i.GetDefaultValue();
                        if (converter.TryConvert(v, out var result))
                            values[token] = result;
                    }
                    extensionSettingsGroup.Add(new ExtensionStringsArraySettingsEntry(i, v));
                    break;
                }
                case IDateTimeOffsetSettingsExtension i:
                {
                    if (values.TryGetValue(token, out var value) && value is DateTimeOffset v)
                        i.OnValueChanged(v);
                    else
                        values[token] = v = i.GetDefaultValue();
                    extensionSettingsGroup.Add(new ExtensionDateSettingsEntry(i, v));
                    break;
                }
                case IBoolSettingsExtension i:
                {
                    if (values.TryGetValue(token, out var value) && value is bool v)
                        i.OnValueChanged(v);
                    else
                        values[token] = v = i.GetDefaultValue();
                    extensionSettingsGroup.Add(new ExtensionBoolSettingsEntry(i, v));
                    break;
                }
                case IDoubleSettingsExtension i:
                {
                    if (values.TryGetValue(token, out var value) && value is double v)
                        i.OnValueChanged(v);
                    else
                        values[token] = v = i.GetDefaultValue();
                    extensionSettingsGroup.Add(new ExtensionDoubleSettingsEntry(i, v));
                    break;
                }
                default:
                    break;
            }
        }
    }

    public bool Disposed { get; private set; }

    public void Dispose()
    {
        if (Disposed)
            return;
        Disposed = true;
        GC.SuppressFinalize(this);
        while (HostModels is [{ } model, ..])
            UnloadHost(model);
    }
}
