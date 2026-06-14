using System.Windows.Controls;
using EasyHideout.ViewModels;

namespace EasyHideout.Views;

public partial class FocusNodeView : UserControl
{
    public FocusNodeView()
    {
        InitializeComponent();
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue && DataContext is FocusStationViewModel vm)
                vm.Load();
        };
    }
}
