using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace PMS.Core.Models.Common;

public class WizardPage
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public Control Content { get; set; }
    public Func<Dictionary<string, object>, Task<bool>>? ValidateAsync { get; set; }
    public string? NextButtonText { get; set; }
    public bool AllowSkip { get; set; }
}