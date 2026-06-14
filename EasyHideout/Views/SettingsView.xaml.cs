using System.Windows.Controls;
using EasyHideout.ViewModels;

namespace EasyHideout.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public void Initialize(MainViewModel main)
    {
        DataContext = new SettingsViewModel(main);
    }
}
