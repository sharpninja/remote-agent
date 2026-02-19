namespace RemoteAgent.Service;

/// <summary>
/// A user permitted to authenticate against the <c>/pair</c> web endpoint to retrieve the server API key.
/// <para>
/// Store the SHA-256 hex digest of the password in <see cref="PasswordHash"/>.
/// Compute it with: <c>echo -n "password" | sha256sum</c> or
/// <c>[Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("password"))).ToLower()</c>
/// </para>
/// </summary>
public sealed record PairingUser
{
    /// <summary>Case-insensitive login username.</summary>
    public string Username { get; init; } = "";

    /// <summary>SHA-256 hex digest (lowercase) of the plaintext password.</summary>
    public string PasswordHash { get; init; } = "";
}
