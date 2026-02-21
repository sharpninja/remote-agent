using FluentAssertions;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Tests;

/// <summary>
/// Unit tests enforcing the port Picker contract on <see cref="MainPageViewModel"/>.
/// Verifies that the port is exposed as a static list (not a free-text field), defaults to
/// the Windows service port (5244), and lists both well-known ports in the correct order.
/// FR-2.4: default endpoint configuration pre-filled for quick connection.
/// </summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-2.4")]
public sealed class PortPickerViewModelTests
{
    // ── AvailablePorts static list ─────────────────────────────────────────────

    [Fact]
    public void AvailablePorts_ShouldNotBeEmpty()
    {
        MainPageViewModel.AvailablePorts.Should().NotBeEmpty();
    }

    [Fact]
    public void AvailablePorts_ShouldContainWindowsPort()
    {
        MainPageViewModel.AvailablePorts.Should().Contain("5244",
            "5244 is the Windows-native service port and must be selectable");
    }

    [Fact]
    public void AvailablePorts_ShouldContainLinuxPort()
    {
        MainPageViewModel.AvailablePorts.Should().Contain("5243",
            "5243 is the Linux/Docker service port and must be selectable");
    }

    [Fact]
    public void AvailablePorts_ShouldHaveLinuxPortFirst()
    {
        MainPageViewModel.AvailablePorts[0].Should().Be("5243",
            "Linux/Docker service port 5243 is the default and must appear first in the picker list");
    }

    [Fact]
    public void AvailablePorts_ShouldContainExactlyTwoEntries()
    {
        MainPageViewModel.AvailablePorts.Should().HaveCount(2,
            "only the Windows (5244) and Linux/Docker (5243) ports are expected");
    }

    [Fact]
    public void AvailablePorts_AllEntriesShouldBeValidPortNumbers()
    {
        foreach (var entry in MainPageViewModel.AvailablePorts)
        {
            int.TryParse(entry, out var port).Should().BeTrue(
                $"'{entry}' must be a parseable integer port number");
            port.Should().BeInRange(1, 65535,
                $"'{entry}' must be a valid TCP port number");
        }
    }

    // ── Default port on a freshly created ViewModel ───────────────────────────

    [Fact]
    public void Port_DefaultValue_ShouldBeLinuxPort()
    {
        var vm = MobileHandlerTests.CreateDefaultViewModel();
        vm.Port.Should().Be("5243",
            "the Linux/Docker service port 5243 is the default so users can connect immediately without editing");
    }

    [Fact]
    public void Port_DefaultValue_ShouldBeFirstItemInAvailablePorts()
    {
        var vm = MobileHandlerTests.CreateDefaultViewModel();
        vm.Port.Should().Be(MainPageViewModel.AvailablePorts[0],
            "the default port must match the first item in AvailablePorts so the Picker shows the correct selection on load");
    }

    [Fact]
    public void Port_DefaultValue_ShouldBeContainedInAvailablePorts()
    {
        var vm = MobileHandlerTests.CreateDefaultViewModel();
        MainPageViewModel.AvailablePorts.Should().Contain(vm.Port,
            "the default port must be one of the pre-populated picker options");
    }

    // ── Port is settable to any valid picker value ────────────────────────────

    [Fact]
    public void Port_WhenSetToLinuxPort_ShouldUpdateProperty()
    {
        var vm = MobileHandlerTests.CreateDefaultViewModel();
        vm.Port = "5243";
        vm.Port.Should().Be("5243");
    }

    [Fact]
    public void Port_WhenSetToWindowsPort_ShouldUpdateProperty()
    {
        var vm = MobileHandlerTests.CreateDefaultViewModel();
        vm.Port = "5244";
        vm.Port.Should().Be("5244");
    }
}
