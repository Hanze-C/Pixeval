// Copyright (c) Pixeval.
// Licensed under the GPL v3 License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Mako.Global.Enum;
using Microsoft.UI.Xaml;

namespace Pixeval.Pages.Capability;

public sealed partial class RecommendationPage
{
    public RecommendationPage()
    {
        InitializeComponent();
        WorkTypeComboBox.SelectedEnum = App.AppViewModel.AppSettings.WorkType;
    }

    public override void OnPageActivated(NavigationEventArgs e, object? parameter) => ChangeSource();

    private void WorkTypeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => ChangeSource();

    private void ChangeSource()
    {
        WorkContainer.WorkView.ResetEngine(App.AppViewModel.MakoClient.RecommendationWorks(WorkTypeComboBox.GetSelectedItem<WorkType>(), App.AppViewModel.AppSettings.TargetFilter));
    }

    private void WorkContainer_OnRefreshRequested(object sender, RoutedEventArgs e) => ChangeSource();
}
