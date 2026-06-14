using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasyHideout.Services;
using EasyHideout.ViewModels;

namespace EasyHideout.Views;

public partial class PriorityView : UserControl
{
    public PriorityView()
    {
        InitializeComponent();
        IsVisibleChanged += (s, e) =>
        {
            if ((bool)e.NewValue && DataContext is PriorityViewModel vm)
                vm.Load();
        };
    }

    private void PriorityRow_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not PriorityItemRow row) return;
        if (!row.FoundInRaid && row.AvgPrice > 0)
        {
            var total = (long)row.AvgPrice * row.QuantityRemaining;
            var fleaNote = row.MinLevelForFlea > 0 ? $"  ·  flea lv {row.MinLevelForFlea}" : "";
            ServiceLocator.Get<TooltipService>().Set($"{row.StationName}  ·  ~{row.AvgPrice:N0}₽ ea · ×{row.QuantityRemaining} needed · ~{total:N0}₽ total{fleaNote}");
        }
        else
        {
            ServiceLocator.Get<TooltipService>().Set($"{row.StationName}  —  Next Level Items");
        }
    }

    private void FocusRow_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not FocusItemRow row) return;
        var firNote = row.FoundInRaid ? "  ·  FiR required" : "";
        ServiceLocator.Get<TooltipService>().Set($"{row.DisplayName}  —  needed by {row.L1StationCount} station{(row.L1StationCount == 1 ? "" : "s")}{firNote}");
    }

    private void PriorityRow_MouseLeave(object sender, MouseEventArgs e)
    {
        ServiceLocator.Get<TooltipService>().Clear();
    }
}
