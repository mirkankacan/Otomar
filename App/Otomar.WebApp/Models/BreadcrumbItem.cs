namespace Otomar.WebApp.Models;

/// <summary>
/// Tek bir breadcrumb öğesi. Url null ise mevcut sayfa (link yok).
/// </summary>
public class BreadcrumbItem
{
    public string Text { get; init; } = string.Empty;
    public string? Url { get; init; }

    public BreadcrumbItem() { }

    public BreadcrumbItem(string text, string? url = null)
    {
        Text = text;
        Url = url;
    }
}
