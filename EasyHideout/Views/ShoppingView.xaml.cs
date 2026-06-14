using System.Windows;
using System.Windows.Controls;
using EasyHideout.ViewModels;

namespace EasyHideout.Views;

public partial class ShoppingView : UserControl
{
    public ShoppingView()
    {
        InitializeComponent();
        IsVisibleChanged += (s, e) =>
        {
            if ((bool)e.NewValue && DataContext is ShoppingViewModel vm)
                vm.Load();
        };
    }
}
