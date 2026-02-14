using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RemoteAgent.App.Services;

public enum ChatMessagePriority { Normal, High, Notify }

public class ChatMessage : INotifyPropertyChanged
{
    /// <summary>Optional id for persistence (TR-11.1). Set by storage after insert.</summary>
    public Guid? Id { get; set; }
    public bool IsUser { get; init; }
    public string Text { get; init; } = "";
    public bool IsError { get; init; }
    public bool IsEvent { get; init; }
    public string? EventMessage { get; init; }
    public ChatMessagePriority Priority { get; init; } = ChatMessagePriority.Normal;
    public string DisplayText => IsEvent ? (EventMessage ?? "") : Text;

    private bool _isArchived;
    public bool IsArchived
    {
        get => _isArchived;
        set { if (_isArchived == value) return; _isArchived = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>HTML for display: markdown rendered for agent output, plain for user/event.</summary>
    public string RenderedHtml =>
        IsEvent ? MarkdownFormat.PlainToHtml(EventMessage) :
        IsUser ? MarkdownFormat.PlainToHtml(Text) :
        MarkdownFormat.ToHtml(Text, IsError);
}
