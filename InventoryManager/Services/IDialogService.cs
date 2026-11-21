namespace InventoryManager.Services;

/// <summary>Lets view models show alerts without referencing any page directly.</summary>
public interface IDialogService
{
    /// <summary>Shows an information/error/confirmation message with an OK button.</summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>Asks a yes/no question. Returns true if the user accepts.</summary>
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
}

/// <summary>Shell-based implementation of <see cref="IDialogService"/>.</summary>
public class DialogService : IDialogService
{
    /// <summary>Async because the alert stays on screen until the user taps OK.</summary>
    public Task ShowMessageAsync(string title, string message)
        => Shell.Current.DisplayAlertAsync(title, message, "OK");

    /// <summary>Async because we must wait for the user's answer before continuing (e.g. before deleting).</summary>
    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
        => Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
}
