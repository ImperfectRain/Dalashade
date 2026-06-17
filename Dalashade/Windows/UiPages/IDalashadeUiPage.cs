using System;
using System.Numerics;

namespace Dalashade.Windows.UiPages;

public enum InterfaceModeVisibility
{
    User,
    Developer,
    Both
}

public interface IDalashadeUiPage
{
    string Id { get; }
    string Title { get; }
    bool DefaultOpen { get; }
    InterfaceModeVisibility Visibility { get; }
    string Summary();
    Vector4? SummaryColor();
    void Draw();
}

public sealed class DelegateDalashadeUiPage : IDalashadeUiPage
{
    private readonly Func<string> summary;
    private readonly Func<Vector4?> summaryColor;
    private readonly Action draw;

    public DelegateDalashadeUiPage(
        string id,
        string title,
        bool defaultOpen,
        InterfaceModeVisibility visibility,
        Func<string> summary,
        Action draw,
        Func<Vector4?>? summaryColor = null)
    {
        Id = id;
        Title = title;
        DefaultOpen = defaultOpen;
        Visibility = visibility;
        this.summary = summary;
        this.draw = draw;
        this.summaryColor = summaryColor ?? (() => null);
    }

    public string Id { get; }
    public string Title { get; }
    public bool DefaultOpen { get; }
    public InterfaceModeVisibility Visibility { get; }
    public string Summary() => summary();
    public Vector4? SummaryColor() => summaryColor();
    public void Draw() => draw();
}
