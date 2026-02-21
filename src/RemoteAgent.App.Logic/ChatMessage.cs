using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RemoteAgent.App.Services;

/// <summary>Message priority for chat messages (FR-3.1, FR-3.2). Normal and High for styling; Notify triggers a system notification and tap opens the app.</summary>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements</see>
public enum ChatMessagePriority { Normal, High, Notify }

/// <summary>Represents a single chat message (user, agent output, error, or session event) in the observable list bound to the chat UI (FR-1.6, FR-2.2, FR-4.1).</summary>
/// <remarks>Implements <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">TR-5.1</see> (observable collection) and TR-5.5 (archive). Agent output is rendered with markdown (FR-2.3, TR-5.3).</remarks>
/// <example><code>
/// var msg = new ChatMessage { IsUser = true, Text = "Hello" };
/// Messages.Add(msg);
/// store?.Add(msg);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements</see>
public class ChatMessage : INotifyPropertyChanged
{
    /// <summary>Optional id for persistence. Set by local message store after insert (TR-11.1).</summary>
    public Guid? Id { get; set; }

    /// <summary>True if the message was sent by the user (FR-2.1).</summary>
    public bool IsUser { get; init; }

    /// <summary>Raw message text (user input or agent output).</summary>
    public string Text { get; init; } = "";

    /// <summary>True if this message is agent stderr or an error (FR-2.2).</summary>
    public bool IsError { get; init; }

    /// <summary>True if this message is a session lifecycle event (FR-7.2), e.g. session started/stopped/error.</summary>
    public bool IsEvent { get; init; }

    /// <summary>When <see cref="IsEvent"/> is true, the event description or kind.</summary>
    public string? EventMessage { get; init; }

    /// <summary>Message priority (FR-3.1). Notify causes a system notification; tap opens the app (FR-3.2, FR-3.3).</summary>
    public ChatMessagePriority Priority { get; init; } = ChatMessagePriority.Normal;

    /// <summary>When set, indicates this message represents a file transfer. Contains the relative path of the transferred file.</summary>
    public string? FileTransferPath { get; init; }

    /// <summary>Plain text for display: event message or raw text (no markdown).</summary>
    public string DisplayText => IsEvent ? (EventMessage ?? "") : Text;

    private bool _isArchived;

    /// <summary>When true, the message is hidden from the main chat list (FR-4.1, FR-4.2, TR-5.5). Swipe left/right toggles this.</summary>
    public bool IsArchived
    {
        get => _isArchived;
        set { if (_isArchived == value) return; _isArchived = value; OnPropertyChanged(); }
    }

    /// <summary>Raised when a property changes (e.g. <see cref="IsArchived"/>).</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises <see cref="PropertyChanged"/> for binding. Used by setters.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>HTML for display in the chat: markdown rendered for agent output (FR-2.3), plain-escaped for user and event messages.</summary>
    /// <see cref="MarkdownFormat.ToHtml"/> <see cref="MarkdownFormat.PlainToHtml"/>
    public string RenderedHtml =>
        IsEvent ? MarkdownFormat.PlainToHtml(EventMessage) :
        IsUser ? MarkdownFormat.PlainToHtml(Text) :
        MarkdownFormat.ToHtml(Text, IsError);
}
