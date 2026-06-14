using System.Windows;
using System.Windows.Controls;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Services;
using EasyHideout.ViewModels;
using EasyHideout.Views;

namespace EasyHideout;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = (MainViewModel)DataContext;
        SettingsViewControl.Initialize(vm);

        RestoreWindowPosition();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Title = AppMode.WindowTitle;
        if (AppMode.IsDev)
            DevBadge.Visibility = Visibility.Visible;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        ((MainViewModel)DataContext).StopAutoRefreshTimer();
        if (WindowState == WindowState.Normal)
            SaveWindowBounds(Left, Top, Width, Height);
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        MaximizeBtnText.Text = WindowState == WindowState.Maximized ? "⧉" : "□";

        // Apply margin when maximized to respect taskbar
        Margin = WindowState == WindowState.Maximized ? new Thickness(6) : new Thickness(0);
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeBtn_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void RestoreWindowPosition()
    {
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings?.WindowLeft == null || settings?.WindowTop == null)
            {
                CenterOnPrimaryScreen();
                return;
            }

            var left = settings.WindowLeft.Value;
            var top = settings.WindowTop.Value;

            if (!IsPositionOnScreen(left, top))
            {
                CenterOnPrimaryScreen();
                return;
            }

            Left = left;
            Top = top;

            if (settings.WindowWidth is > 0 && settings.WindowHeight is > 0)
            {
                Width = settings.WindowWidth.Value;
                Height = settings.WindowHeight.Value;
            }
        }
        catch
        {
            CenterOnPrimaryScreen();
        }
    }

    private void CenterOnPrimaryScreen()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;
    }

    private static bool IsPositionOnScreen(double left, double top)
    {
        var vsLeft = SystemParameters.VirtualScreenLeft;
        var vsTop = SystemParameters.VirtualScreenTop;
        var vsRight = vsLeft + SystemParameters.VirtualScreenWidth;
        var vsBottom = vsTop + SystemParameters.VirtualScreenHeight;
        return left >= vsLeft - 100 && left < vsRight - 200 &&
               top >= vsTop - 10 && top < vsBottom - 100;
    }

    private static void SaveWindowBounds(double left, double top, double width, double height)
    {
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null) return;
            settings.WindowLeft = left;
            settings.WindowTop = top;
            settings.WindowWidth = width;
            settings.WindowHeight = height;
            db.SaveChanges();
        }
        catch { }
    }
}
