using FluentAssertions;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.UiTests.ViewModels;

/// <summary>Tests for <see cref="ConnectionSettingsDialogViewModel"/>. FR-12.1, FR-12.2; TR-18.1, TR-18.2, TR-18.3, TR-18.4.</summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-12.1")]
[Trait("Requirement", "FR-12.2")]
[Trait("Requirement", "TR-18.1")]
[Trait("Requirement", "TR-18.2")]
[Trait("Requirement", "TR-18.3")]
[Trait("Requirement", "TR-18.4")]
public class ConnectionSettingsDialogViewModelTests
{
    private static ConnectionSettingsDefaults CreateDefaults(
        string host = "127.0.0.1",
        string port = "5243",
        string mode = "server",
        string agentId = "process",
        string apiKey = "",
        string perRequestContext = "",
        IReadOnlyList<string>? modes = null) =>
        new(host, port, mode, agentId, apiKey, perRequestContext,
            modes ?? new List<string> { "server", "client" });

    // FR-12.1, TR-18.4
    [Fact]
    public void Constructor_ShouldPopulateFromDefaults()
    {
        var defaults = CreateDefaults(host: "myhost", port: "9999", apiKey: "key123");
        var vm = new ConnectionSettingsDialogViewModel(defaults);

        vm.Host.Should().Be("myhost");
        vm.Port.Should().Be("9999");
        vm.ApiKey.Should().Be("key123");
        vm.SelectedConnectionMode.Should().Be("server");
        vm.SelectedAgentId.Should().Be("process");
        vm.ConnectionModes.Should().Contain("server");
        vm.ConnectionModes.Should().Contain("client");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void Constructor_WhenModeNotInList_ShouldSelectFirst()
    {
        var defaults = CreateDefaults(mode: "nonexistent");
        var vm = new ConnectionSettingsDialogViewModel(defaults);

        vm.SelectedConnectionMode.Should().Be("server");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void SubmitCommand_WithValidInputs_ShouldSetIsAccepted()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults());
        bool closeCalled = false;
        vm.RequestClose += accepted => closeCalled = true;

        vm.SubmitCommand.Execute(null);

        vm.IsAccepted.Should().BeTrue();
        closeCalled.Should().BeTrue();
        vm.ValidationMessage.Should().BeEmpty();
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void SubmitCommand_WithEmptyHost_ShouldSetValidationMessage()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults(host: ""));

        vm.SubmitCommand.Execute(null);

        vm.IsAccepted.Should().BeFalse();
        vm.ValidationMessage.Should().Contain("Host");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void SubmitCommand_WithInvalidPort_ShouldSetValidationMessage()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults(port: "notanumber"));

        vm.SubmitCommand.Execute(null);

        vm.IsAccepted.Should().BeFalse();
        vm.ValidationMessage.Should().Contain("Port");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void SubmitCommand_WithPortZero_ShouldSetValidationMessage()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults(port: "0"));

        vm.SubmitCommand.Execute(null);

        vm.IsAccepted.Should().BeFalse();
        vm.ValidationMessage.Should().Contain("Port");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void SubmitCommand_WithPortTooHigh_ShouldSetValidationMessage()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults(port: "70000"));

        vm.SubmitCommand.Execute(null);

        vm.IsAccepted.Should().BeFalse();
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void SubmitCommand_WithEmptyMode_ShouldSetValidationMessage()
    {
        var defaults = CreateDefaults(mode: "", modes: new List<string>());
        var vm = new ConnectionSettingsDialogViewModel(defaults);
        vm.SelectedConnectionMode = "";

        vm.SubmitCommand.Execute(null);

        vm.IsAccepted.Should().BeFalse();
        vm.ValidationMessage.Should().Contain("Mode");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void SubmitCommand_WithEmptyAgent_ShouldSetValidationMessage()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults(agentId: ""));

        vm.SubmitCommand.Execute(null);

        vm.IsAccepted.Should().BeFalse();
        vm.ValidationMessage.Should().Contain("Agent");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void CancelCommand_ShouldSetIsAcceptedFalseAndRaiseClose()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults());
        bool? closeAccepted = null;
        vm.RequestClose += accepted => closeAccepted = accepted;

        vm.CancelCommand.Execute(null);

        vm.IsAccepted.Should().BeFalse();
        closeAccepted.Should().BeFalse();
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void ToResult_ShouldReturnTrimmedValues()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults(host: "  myhost  ", port: " 5243 "));

        var result = vm.ToResult();

        result.Host.Should().Be("myhost");
        result.Port.Should().Be("5243");
    }

    // FR-12.1, TR-18.4
    [Fact]
    public void PropertyChanged_ShouldFireForHost()
    {
        var vm = new ConnectionSettingsDialogViewModel(CreateDefaults());
        string? changedProp = null;
        vm.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        vm.Host = "newhost";

        changedProp.Should().Be(nameof(vm.Host));
    }
}
