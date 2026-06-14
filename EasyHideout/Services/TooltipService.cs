using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyHideout.Services;

public class TooltipService : INotifyPropertyChanged
{
    private string _currentMessage = "";

    public string CurrentMessage
    {
        get => _currentMessage;
        set { _currentMessage = value; OnPropertyChanged(); }
    }

    public void Set(string message) => CurrentMessage = message;
    public void Clear() => CurrentMessage = "";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
