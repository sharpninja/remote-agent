using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Requests;

/// <summary>
/// Triggers QR-code pairing. When <see cref="RawUri"/> is provided (e.g. from a deep link) the
/// scanner is bypassed and the URI is parsed directly. When it is null the camera scanner is
/// invoked so the user can point at a QR code.
/// </summary>
public sealed record ScanQrCodeRequest(
    Guid CorrelationId,
    MainPageViewModel Workspace) : IRequest<CommandResult>
{
    /// <summary>Optional pre-supplied pairing URI; set by the deep-link handler.</summary>
    public string? RawUri { get; init; }
}
