using System.Diagnostics;
using System.Text;

namespace FZScout.Tests.Xaml;

/// <summary>
/// Records what WPF reports about bindings it can't resolve, like a path to a property the view model doesn't have.
/// WPF doesn't throw for those; it shows a default value instead.
/// </summary>
class BindingErrorRecorder : TraceListener
{
    readonly StringBuilder _messageBeingWritten = new();
    readonly List<string> _bindingErrors = [];

    /// <summary>Call on the WPF thread.</summary>
    public static (T Result, IReadOnlyList<string> BindingErrors) RecordBindingErrorsWhile<T>(Func<T> function)
    {
        // Without this, WPF only reports binding errors when a debugger is attached or a registry setting turns them on.
        PresentationTraceSources.Refresh();
        var dataBindingSource = PresentationTraceSources.DataBindingSource;
        dataBindingSource.Switch.Level = SourceLevels.Warning;
        var recorder = new BindingErrorRecorder();
        dataBindingSource.Listeners.Add(recorder);
        try
        {
            var result = function();
            return (result, recorder._bindingErrors);
        }
        finally
        {
            dataBindingSource.Listeners.Remove(recorder);
        }
    }

    /// <summary>WPF writes the start of each message, like "System.Windows.Data Error: 40 : ", then the rest with WriteLine.</summary>
    public override void Write(string? message) => this._messageBeingWritten.Append(message);

    public override void WriteLine(string? message)
    {
        this._messageBeingWritten.Append(message);
        this._bindingErrors.Add(this._messageBeingWritten.ToString());
        this._messageBeingWritten.Clear();
    }
}
