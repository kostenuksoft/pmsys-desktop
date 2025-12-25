using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PMS.Core.Models.Common;
using PMS.ViewModels;

namespace PMS.Core.Services.Interfaces;

public interface ITabService
{
    event EventHandler<TabEventArgs>? TabOpened;
    event EventHandler<TabEventArgs>? TabClosed;
    event EventHandler<TabEventArgs>? TabSelected;
    event EventHandler<TabEventArgs>? TabsReordered;

    IReadOnlyList<TabItemModel> Tabs { get; }
    TabItemModel? SelectedTab { get; }

    Task<TabItemModel?> OpenTabAsync<TViewModel>(TViewModel viewModel, string header, string? iconSource = null) where TViewModel : BaseViewModel;
    Task<TabItemModel?> OpenTabAsync(Type viewModelType, string header, object? parameter = null, string? iconSource = null);
    Task<bool> CloseTabAsync(TabItemModel tab);
    Task<bool> CloseTabAsync(Guid tabId);
    Task<bool> CloseAllTabsAsync();
    Task<bool> CloseOtherTabsAsync(TabItemModel exceptTab);
    void SelectTab(TabItemModel tab);
    void SelectTab(Guid tabId);
    TabItemModel? FindTab(Predicate<TabItemModel> predicate);
    TabItemModel? FindTabByViewModel<TViewModel>() where TViewModel : BaseViewModel;
    void ReorderTabs(int oldIndex, int newIndex);
    Task<bool> CanCloseTabAsync(TabItemModel tab);
}