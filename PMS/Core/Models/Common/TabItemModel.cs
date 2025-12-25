using System;

namespace PMS.Core.Models.Common;

public class TabItemModel : ReactiveTab
{
    private string _header = string.Empty;
    private object? _content;
    private bool _isSelected;
    private bool _isClosable = true;
    private bool _hasChanges;
    private string? _iconSource;
    private object? _tag;
    private Guid _id = Guid.NewGuid();

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }

    public object? Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsClosable
    {
        get => _isClosable;
        set => SetProperty(ref _isClosable, value);
    }

    public bool HasChanges
    {
        get => _hasChanges;
        set => SetProperty(ref _hasChanges, value);
    }

    public string? IconSource
    {
        get => _iconSource;
        set => SetProperty(ref _iconSource, value);
    }

    public object? Tag
    {
        get => _tag;
        set => SetProperty(ref _tag, value);
    }

    public string DisplayHeader => HasChanges ? $"{Header} *" : Header;

}