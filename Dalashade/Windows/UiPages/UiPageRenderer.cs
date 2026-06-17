using System.Collections.Generic;
using System.Linq;

namespace Dalashade.Windows.UiPages;

internal static class UiPageRenderer
{
    public static void Draw(IEnumerable<IDalashadeUiPage> pages, InterfaceMode mode)
    {
        foreach (var page in pages.Where(page => IsVisible(page.Visibility, mode)))
        {
            UiSection.Draw(
                page.Id,
                page.Title,
                page.DefaultOpen,
                page.Summary(),
                page.Draw,
                page.SummaryColor());
        }
    }

    private static bool IsVisible(InterfaceModeVisibility visibility, InterfaceMode mode)
    {
        return visibility == InterfaceModeVisibility.Both
               || (visibility == InterfaceModeVisibility.User && mode == InterfaceMode.User)
               || (visibility == InterfaceModeVisibility.Developer && mode == InterfaceMode.Developer);
    }
}
