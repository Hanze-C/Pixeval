// Copyright (c) Pixeval.
// Licensed under the GPL v3 License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Mako.Global.Enum;
using Pixeval.Messages;
using Pixeval.Util.UI;
using Windows.Foundation;
using WinUI3Utilities;
using Symbol = FluentIcons.Common.Symbol;

namespace Pixeval.Controls;

public sealed partial class NovelItem
{
    [GeneratedDependencyProperty]
    public partial NovelItemViewModel ViewModel { get; set; }

    public event TypedEventHandler<NovelItem, NovelItemViewModel>? ViewModelChanged;

    public event TypedEventHandler<NovelItem, NovelItemViewModel>? OpenNovelRequested;

    public event TypedEventHandler<NovelItem, NovelItemViewModel>? RequestOpenUserInfoPage;

    public event TypedEventHandler<NovelItem, NovelItemViewModel>? RequestAddToBookmark;

#pragma warning disable CS0067, CS0414 // Event is never used
    public event Func<TeachingTip> RequestTeachingTip = null!;
#pragma warning restore CS0067, CS0414 // Event is never used

    public NovelItem() => InitializeComponent();

    public int IsPointerOver
    {
        get;
        private set
        {
            try
            {
                var old = field;
                field = value;
                var currentView = ConnectedAnimationService.GetForCurrentView();
                if (IsPointerOver > 0 && old <= 0)
                {
                    var anim1 = currentView.PrepareToAnimate("ForwardConnectedAnimation1", this);
                    var anim2 = currentView.PrepareToAnimate("ForwardConnectedAnimation2", Image);
                    var anim3 = currentView.PrepareToAnimate("ForwardConnectedAnimation3", HeartButton);
                    var anim4 = currentView.PrepareToAnimate("ForwardConnectedAnimation4", TitleTextBlock);
                    var anim5 = currentView.PrepareToAnimate("ForwardConnectedAnimation5", AuthorTextBlock);
                    var anim6 = currentView.PrepareToAnimate("ForwardConnectedAnimation6", TagsList);
                    anim1.Configuration = anim2.Configuration = anim3.Configuration =
                        anim4.Configuration = anim5.Configuration = anim6.Configuration =
                            new BasicConnectedAnimationConfiguration();
                    _ = anim1.TryStart(NovelItemPopup);
                    _ = anim2.TryStart(PopupImage);
                    _ = anim3.TryStart(PopupHeartButton);
                    _ = anim4.TryStart(PopupTitleTextBlock);
                    _ = anim5.TryStart(PopupAuthorButton);
                    _ = anim6.TryStart(PopupTagsList);
                    NovelItemPopup.Child.To<FrameworkElement>().Width = ActualWidth + 10;
                    NovelItemPopup.IsOpen = true;
                }
                else if (IsPointerOver <= 0 && old > 0)
                {
                    var anim1 = currentView.PrepareToAnimate("BackwardConnectedAnimation1", NovelItemPopup);
                    var anim2 = currentView.PrepareToAnimate("BackwardConnectedAnimation2", PopupImage);
                    var anim3 = currentView.PrepareToAnimate("BackwardConnectedAnimation3", PopupHeartButton);
                    var anim4 = currentView.PrepareToAnimate("BackwardConnectedAnimation4", PopupTitleTextBlock);
                    var anim5 = currentView.PrepareToAnimate("BackwardConnectedAnimation5", PopupAuthorButton);
                    var anim6 = currentView.PrepareToAnimate("BackwardConnectedAnimation6", PopupTagsList);
                    anim1.Configuration = anim2.Configuration = anim3.Configuration =
                        anim4.Configuration = anim5.Configuration = anim6.Configuration =
                            new BasicConnectedAnimationConfiguration();
                    anim1.Completed += (_, _) =>
                    {
                        NovelItemPopup.IsOpen = false;
                        NovelItemPopup.Visibility = Visibility.Visible;
                    };
                    _ = anim1.TryStart(this);
                    _ = anim2.TryStart(Image);
                    _ = anim3.TryStart(HeartButton);
                    _ = anim4.TryStart(TitleTextBlock);
                    _ = anim5.TryStart(AuthorTextBlock);
                    _ = anim6.TryStart(TagsList);
                    _ = Task.Delay(100).ContinueWith(_ => NovelItemPopup.Visibility = Visibility.Collapsed,
                        TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    partial void OnViewModelPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        ViewModelChanged?.Invoke(this, ViewModel);
    }

    private void TagButton_OnClicked(object sender, RoutedEventArgs e)
    {
        _ = WeakReferenceMessenger.Default.Send(new WorkTagClickedMessage(SimpleWorkType.Novel, ((TextBlock) ((Button) sender).Content).Text));
    }

    private async void NovelItem_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        await Task.Delay(1000);
        IsPointerOver += 1;
    }

    private async void NovelItem_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        await Task.Delay(100);
        IsPointerOver -= 1;
    }

    private void NovelItemPopup_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        IsPointerOver += 1;
    }

    private void OpenNovel_OnClicked(object sender, RoutedEventArgs e)
    {
        OpenNovelRequested?.Invoke(this, ViewModel);
    }

    private XamlUICommand OpenNovelCommand { get; } = EntryItemResources.OpenNovel.GetCommand(Symbol.BookOpen);

    private void AddToBookmark_OnClicked(object sender, RoutedEventArgs e)
    {
        RequestAddToBookmark?.Invoke(this, ViewModel);
    }

    private void OpenUserInfoPage_OnClicked(object sender, RoutedEventArgs e)
    {
        RequestOpenUserInfoPage?.Invoke(this, ViewModel);
    }
}
