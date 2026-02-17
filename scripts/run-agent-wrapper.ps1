# Wrapper to launch an agent process for integration tests. Logs the full command line
# before execution and when the process exits so test output is visible.
# Usage: pwsh -NoProfile -File run-agent-wrapper.ps1 -AgentPath "C:\path\to\agent.exe" [-Arguments "args"]
# Requires PowerShell 7+.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $AgentPath,

    [Parameter(Mandatory = $false)]
    [string] $Arguments = ""
)

$fullCmd = if ($Arguments) { "& `"$AgentPath`" $Arguments" } else { "& `"$AgentPath`"" }
Write-Host "[run-agent-wrapper] Executing: $fullCmd"

# Let the child inherit stdin/stdout/stderr so the service sees the agent's output (service already redirected the wrapper's streams).
$psi = [System.Diagnostics.ProcessStartInfo]@{
    FileName       = $AgentPath
    Arguments      = $Arguments
    UseShellExecute = $false
    CreateNoWindow  = $true
}
$p = [System.Diagnostics.Process]::Start($psi)
$p.WaitForExit()
$code = $p.ExitCode
Write-Host "[run-agent-wrapper] Command exited with code $code"
exit $code
