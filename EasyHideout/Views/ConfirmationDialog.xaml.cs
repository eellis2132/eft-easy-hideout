using System.Windows;

namespace EasyHideout.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string message, Window owner)
    {
        InitializeComponent();
        Owner = owner;
        MessageText.Text = message;
    }

    private void ConfirmBtn_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void CancelBtn_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    public static bool Show(string message, Window owner)
        => new ConfirmationDialog(message, owner).ShowDialog() == true;
}
