using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EasyHideout.ViewModels;

namespace EasyHideout.Views;

public partial class ActiveNodesView : UserControl
{
    public ActiveNodesView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (DataContext is not ActiveNodesViewModel vm) return;
            ApplyLayout(vm.DetailDock);
            vm.PropertyChanged += OnVmPropertyChanged;
        };

        Unloaded += (s, e) =>
        {
            if (DataContext is ActiveNodesViewModel vm)
                vm.PropertyChanged -= OnVmPropertyChanged;
        };

        IsVisibleChanged += (s, e) =>
        {
            if ((bool)e.NewValue && DataContext is ActiveNodesViewModel vm)
                vm.Load();
        };
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActiveNodesViewModel.DetailDock) &&
            sender is ActiveNodesViewModel vm)
        {
            Dispatcher.Invoke(() => ApplyLayout(vm.DetailDock));
        }
    }

    private void ApplyLayout(Dock dock)
    {
        const double splitterSize = 5;
        const double defaultPanelSize = 300;

        bool isHorizontal = dock == Dock.Top || dock == Dock.Bottom;

        if (isHorizontal)
        {
            // Rows: panel | splitter | content  (or reversed for Bottom)
            GridRow0.Height = dock == Dock.Bottom ? new GridLength(1, GridUnitType.Star) : new GridLength(defaultPanelSize);
            GridRow1.Height = new GridLength(splitterSize);
            GridRow2.Height = dock == Dock.Bottom ? new GridLength(defaultPanelSize) : new GridLength(1, GridUnitType.Star);

            // Columns: single full-width column
            GridCol0.Width = new GridLength(1, GridUnitType.Star);
            GridCol1.Width = new GridLength(0);
            GridCol2.Width = new GridLength(0);

            DetailSplitter.Width = double.NaN;  // Auto
            DetailSplitter.Height = splitterSize;
        }
        else
        {
            // Columns: panel | splitter | content  (or reversed for Right)
            GridCol0.Width = dock == Dock.Right ? new GridLength(1, GridUnitType.Star) : new GridLength(defaultPanelSize);
            GridCol1.Width = new GridLength(splitterSize);
            GridCol2.Width = dock == Dock.Right ? new GridLength(defaultPanelSize) : new GridLength(1, GridUnitType.Star);

            // Rows: single full-height row
            GridRow0.Height = new GridLength(1, GridUnitType.Star);
            GridRow1.Height = new GridLength(0);
            GridRow2.Height = new GridLength(0);

            DetailSplitter.Width = splitterSize;
            DetailSplitter.Height = double.NaN;  // Auto
        }
    }
}
