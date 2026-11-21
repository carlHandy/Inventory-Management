using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryManager.ViewModels;

/// <summary>
/// Base class for all view models. Implements INotifyPropertyChanged so that when a property
/// changes the bound XAML controls update automatically.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isBusy;

    /// <summary>True while a database operation is running.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !_isBusy;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Sets the field, raises PropertyChanged if the value really changed, returns true if it did.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
