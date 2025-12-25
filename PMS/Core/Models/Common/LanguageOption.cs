namespace PMS.Core.Models.Common;

public class LanguageOption(string code, string displayName)
{
    public string DisplayName { get; } = displayName;
    public string Code { get; } = code;

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj) => obj is LanguageOption other && Code == other.Code;

    public override int GetHashCode() => Code.GetHashCode();
}