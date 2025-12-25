using Microsoft.Extensions.DependencyInjection;
using PMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PMS.ViewModels.Tabs.Interfaces;
using PMS.Core.Services.Interfaces;
using PMS.Core.Models.Common;

namespace PMS.Core.Services;

public class TabService : ITabService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ObservableCollection<TabItemModel> _tabs = new();
    private TabItemModel? _selectedTab;

    public event EventHandler<TabEventArgs>? TabOpened;
    public event EventHandler<TabEventArgs>? TabClosed;
    public event EventHandler<TabEventArgs>? TabSelected;
    public event EventHandler<TabEventArgs>? TabsReordered;

    public IReadOnlyList<TabItemModel> Tabs => _tabs;
    public TabItemModel? SelectedTab => _selectedTab;

    public TabService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TabItemModel?> OpenTabAsync<TViewModel>(TViewModel viewModel, string header, string? iconSource)
        where TViewModel : BaseViewModel
    {
        var existingTab = _tabs.FirstOrDefault(t => t.Content == viewModel);
        if (existingTab != null)
        {
            SelectTab(existingTab);
            return existingTab;
        }

        var tab = new TabItemModel
        {
            Header = header,
            Content = viewModel,
            IconSource = iconSource,
            IsClosable = true,
            IsSelected = true
        };

        if (viewModel is INotifyUnsavedChanges notifyChanges)
        {
            notifyChanges.HasUnsavedChangesChanged += (s, e) =>
            {
                tab.HasChanges = notifyChanges.HasUnsavedChanges;
            };
        }

        _tabs.Add(tab);
        TabOpened?.Invoke(this, new TabEventArgs(tab));

        return await Task.FromResult(tab);
    }

    public async Task<TabItemModel?> OpenTabAsync(Type viewModelType, string header, object? parameter = null, string? iconSource = null)
    {
        try
        {
            if (_serviceProvider.GetRequiredService(viewModelType) is not BaseViewModel viewModel)
            {
                return null;
            }

            if (parameter != null && viewModel is IInitializableViewModel initializable)
            {
                await initializable.InitializeAsync(parameter);
            }

            return await OpenTabAsync(viewModel, header, iconSource);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open tab: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> CloseTabAsync(TabItemModel tab)
    {
        if (!_tabs.Contains(tab))
        {
            return false;
        }

        if (!await CanCloseTabAsync(tab))
        {
            return false;
        }

        var index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        if (_selectedTab == tab)
        {
            if (_tabs.Count > 0)
            {
                var newIndex = Math.Min(index, _tabs.Count - 1);
                SelectTab(_tabs[newIndex]);
            }
            else
            {
                _selectedTab = null;
            }
        }

        if (tab.Content is IDisposable disposable)
        {
            disposable.Dispose();
        }

        TabClosed?.Invoke(this, new TabEventArgs(tab));
        return true;
    }

    public async Task<bool> CloseTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        return tab != null && await CloseTabAsync(tab);
    }

    public async Task<bool> CloseAllTabsAsync()
    {
        var tabsToClose = _tabs.ToList();
        foreach (var tab in tabsToClose)
        {
            if (!await CloseTabAsync(tab))
                return false;
        }
        return true;
    }

    public async Task<bool> CloseOtherTabsAsync(TabItemModel exceptTab)
    {
        var tabsToClose = _tabs.Where(t => t != exceptTab).ToList();
        foreach (var tab in tabsToClose)
        {
            if (!await CloseTabAsync(tab))
                return false;
        }
        return true;
    }

    public void SelectTab(TabItemModel tab)
    {
        if (!_tabs.Contains(tab))
        {
            return;
        }

        if (_selectedTab != null)
        {
            _selectedTab.IsSelected = false;
        }

        _selectedTab = tab;
        tab.IsSelected = true;

        TabSelected?.Invoke(this, new TabEventArgs(tab));
    }

    public void SelectTab(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
            SelectTab(tab);
    }

    public TabItemModel? FindTab(Predicate<TabItemModel> predicate)
    {
        return _tabs.FirstOrDefault(t => predicate(t));
    }

    public TabItemModel? FindTabByViewModel<TViewModel>() where TViewModel : BaseViewModel
    {
        return _tabs.FirstOrDefault(t => t.Content is TViewModel);
    }

    public void ReorderTabs(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _tabs.Count ||
            newIndex < 0 || newIndex >= _tabs.Count)
            return;

        var tab = _tabs[oldIndex];
        _tabs.RemoveAt(oldIndex);
        _tabs.Insert(newIndex, tab);

        TabsReordered?.Invoke(this, new TabEventArgs(tab));
    }

    public async Task<bool> CanCloseTabAsync(TabItemModel tab)
    {
        if (!tab.IsClosable)
        {
            return false;
        }

        if (tab.Content is INotifyUnsavedChanges { HasUnsavedChanges: true })
        {
            return await Task.FromResult(true);
        }

        return true;
    }
}