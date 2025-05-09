// Copyright (c) Pixeval.Controls.
// Licensed under the GPL v3 License.

using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Pixeval.Utilities;
using Windows.Foundation;
using Windows.Graphics;
using WinUI3Utilities;

namespace Pixeval.Controls.Windowing;

public sealed partial class TabPage
{
    public TabView TabView => TabViewControl;

    private bool _ownsWindow;

    public static event TypedEventHandler<AppWindow, AppWindowClosingEventArgs>? CreatedWindowClosing;

    public TabPage()
    {
        InitializeComponent();
        // 在子程序集暂时不能使用x:Uid
        ToolTipService.SetToolTip(RevokeAllTabsButton, TabPageResources.RevokeAllTabsButtonToolTipServiceToolTip);
    }

    /// <inheritdoc />
    public override void OnPageActivated(NavigationEventArgs e, object? parameter)
    {
        _ownsWindow = parameter as bool? ?? false;
    }

    private void TabPage_OnLoaded(object sender, RoutedEventArgs e)
    {

        if (_ownsWindow)
        {
            Window.SetTitleBar(CustomDragRegion);
            CustomDragRegion.MinWidth = 188;
        }

        ((IStructuralDisposalCompleter) this).Hook();
    }

    public void AddPage(NavigationViewTag viewModel)
    {
        var frame = new Frame
        {
            // TabViewItem不在视觉树上，而Content在，加个Frame方便找到NavigationViewTag
            Tag = viewModel,
            Background = Application.Current.Resources["LayerFillColorDefaultBrush"].To<Brush>()
        };
        frame.Loaded += Frame_OnLoaded;
        var tabViewItem = new TabViewItem
        {
            // see https://github.com/microsoft/microsoft-ui-xaml/issues/3329
            Header = string.IsNullOrEmpty(viewModel.Header) ? " " : viewModel.Header,
            IconSource = viewModel.IconSource ?? new ImageIconSource { ImageSource = WindowFactory.IconImageSource },
            IsClosable = true,
            Tag = viewModel,
            Content = frame
        };
        viewModel.PropertyChanged += (sender, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(viewModel.Header):
                    tabViewItem.Header = string.IsNullOrEmpty(viewModel.Header) ? " " : viewModel.Header;
                    break;
                case nameof(viewModel.IconSource):
                    tabViewItem.IconSource = viewModel.IconSource ?? new ImageIconSource { ImageSource = WindowFactory.IconImageSource };
                    break;
            }
        };
        TabView.TabItems.Add(tabViewItem);
        TabView.SelectedItem = tabViewItem;
    }

    private void RemoveTab(TabViewItem tab)
    {
        _ = TabView.TabItems.Remove(tab);

        if (TabView.TabItems.Count is 0 && _ownsWindow)
            Window.Close();
    }

    private void Frame_OnLoaded(object sender, RoutedEventArgs e)
    {
        var frame = sender.To<Frame>();
        if (frame.Content is not null)
            return;
        var viewModel = frame.GetTag<NavigationViewTag>();
        _ = frame.Navigate(viewModel.NavigateTo, viewModel.Parameter, viewModel.TransitionInfo);
    }

    private async void TabView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs e)
    {
        await Task.Yield();
        RemoveTab(e.Tab);
        if (e.Tab.Content is Frame { Content: IStructuralDisposalCompleter completer })
            completer.CompleteDisposalRecursively();
    }

    private void TabView_OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs e)
    {
        // 只有一条就不新建窗口
        if (TabView.TabItems.Count < 2)
            return;
        var tabPage = new TabPage { _ownsWindow = true };
        var size = new SizeInt32();
        if (sender.SelectedItem is TabViewItem { Content: FrameworkElement { ActualSize: { X: var x, Y: var y } } })
        {
            size.Width = (int) x;
            size.Height = (int) y;
        }
        tabPage.TabView.TabItems.Add(e.Tab);
        RemoveTab(e.Tab);
        WindowFactory.RootWindow.Fork(tabPage, out _)
            .WithSizeLimit(640, 360)
            .Init(nameof(Pixeval), size)
            .WithClosing(CreatedWindowClosing)
            .Activate();
    }

    private void RevokeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_ownsWindow || WindowFactory.RootWindow.PageContent.FindDescendant<TabPage>() is not { } rootTabPage)
            return;
        var arr = TabView.TabItems.ToArray();
        TabView.TabItems.Clear();
        rootTabPage.TabView.TabItems.AddRange(arr);
        Window.Close();
    }

    /*
    private void TabView_OnTabTearOutRequested(TabView sender, TabViewTabTearOutRequestedEventArgs e)
    {
        if (_tabTearOutWindow is null)
            return;

        var newPage = _tabTearOutWindow.PageContent.To<TabPage>();

        foreach (var tab in e.Tabs.Cast<TabViewItem>())
        {
            tab.FindAscendant<TabView>()?.TabItems.Remove(tab);
            newPage.TabView.TabItems.Add(tab);
        }
    }

    private void TabView_OnTabTearOutWindowRequested(TabView sender, TabViewTabTearOutWindowRequestedEventArgs e)
    {
        _tabTearOutWindow = WindowFactory.RootWindow.Fork(new TabPage { _ownsWindow = true }, out _)
            .WithSizeLimit(640, 360)
            .Init(nameof(Pixeval));

        e.NewWindowId = _tabTearOutWindow.AppWindow.Id;
    }

    private void TabView_OnExternalTornOutTabsDropping(TabView sender, TabViewExternalTornOutTabsDroppingEventArgs e)
    {
        e.AllowDrop = true;
    }

    private void TabView_OnExternalTornOutTabsDropped(TabView sender, TabViewExternalTornOutTabsDroppedEventArgs e)
    {
        var position = 0;

        foreach (var tab in e.Tabs.Cast<TabViewItem>())
        {
            tab.FindAscendant<TabView>()?.TabItems.Remove(tab);
            sender.TabItems.Insert(e.DropIndex + position, tab);
            ++position;
        }
    }
    */
    private void TabPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        ((IStructuralDisposalCompleter) this).CompleteDisposalRecursively();
    }
}
