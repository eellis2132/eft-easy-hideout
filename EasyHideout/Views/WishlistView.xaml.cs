using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using EasyHideout.Services;
using EasyHideout.ViewModels;

namespace EasyHideout.Views;

public partial class WishlistView : UserControl
{
    public WishlistView()
    {
        InitializeComponent();
        IsVisibleChanged += (s, e) =>
        {
            if ((bool)e.NewValue && DataContext is WishlistViewModel vm)
                vm.Load();
        };
    }

    private void ItemRow_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is WishlistRow row)
            ServiceLocator.Get<TooltipService>().Set(row.StationTooltip);
    }

    private void ItemRow_MouseLeave(object sender, MouseEventArgs e)
    {
        ServiceLocator.Get<TooltipService>().Clear();
    }

    private void OwnedTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (string.IsNullOrWhiteSpace(tb.Text) || !int.TryParse(tb.Text, out _))
        {
            tb.Text = "0";
            BindingOperations.GetBindingExpression(tb, TextBox.TextProperty)?.UpdateSource();
        }
    }
}
