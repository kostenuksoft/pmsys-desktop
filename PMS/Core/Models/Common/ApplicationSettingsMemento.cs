namespace PMS.Core.Models.Common;

public class ApplicationSettingsMemento
{
    public LanguageOption SelectedDefaultLanguage { get; init; }
    public int PageSize { get; init; }
    public int MaxExportRows { get; init; }

    public ApplicationSettingsMemento(
        LanguageOption selectedDefaultLanguage,
        int pageSize,
        int maxExportRows)
    {
        SelectedDefaultLanguage = selectedDefaultLanguage;
        PageSize = pageSize;
        MaxExportRows = maxExportRows;
    }
}