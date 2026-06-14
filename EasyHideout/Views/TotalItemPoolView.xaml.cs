using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EasyHideout.ViewModels;

namespace EasyHideout.Views;

public partial class TotalItemPoolView : UserControl
{
    public TotalItemPoolView()
    {
        InitializeComponent();
        IsVisibleChanged += (s, e) =>
        {
            if ((bool)e.NewValue && DataContext is ItemPoolViewModel vm)
                vm.Load();
        };
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
