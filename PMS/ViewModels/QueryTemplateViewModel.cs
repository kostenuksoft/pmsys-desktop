using ReactiveUI;

namespace PMS.ViewModels;

public class QueryTemplateViewModel : ReactiveObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
}