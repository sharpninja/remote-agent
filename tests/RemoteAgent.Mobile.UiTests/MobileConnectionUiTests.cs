using FluentAssertions;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium;
using System.Diagnostics;

namespace RemoteAgent.Mobile.UiTests;

/// <summary>
/// Mobile UI smoke coverage for FR-2.7 and TR-8.5 via Appium-hosted Android UI tests.
/// </summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-2.4")]
[Trait("Requirement", "FR-2.7")]
[Trait("Requirement", "FR-7.2")]
[Trait("Requirement", "TR-5.2")]
[Trait("Requirement", "TR-5.8")]
[Trait("Requirement", "TR-8.5")]
public sealed class MobileConnectionUiTests : IDisposable
{
    private AndroidDriver? _driver;

    [SkippableFact]
    public void ConnectionView_ShouldExposeCoreControls()
    {
        // FR-2.7: dedicated connection view before chat workspace.
        Skip.IfNot(IsConfigured(), "Set MOBILE_APPIUM_SERVER_URL and MOBILE_APP_PATH (and device settings) to run mobile UI tests.");
        EnsureDriver();

        _driver.Should().NotBeNull();
        _driver!.FindElement(MobileBy.AccessibilityId("mobile_connect_host")).Should().NotBeNull();
        _driver.FindElement(MobileBy.AccessibilityId("mobile_connect_port")).Should().NotBeNull();
        _driver.FindElement(MobileBy.AccessibilityId("mobile_connect_button")).Should().NotBeNull();
        _driver.FindElement(MobileBy.AccessibilityId("mobile_new_session_button")).Should().NotBeNull();
    }

    [SkippableFact]
    public void ConnectionView_ShouldExposeTerminateAndStatusElements()
    {
        // FR-2.4, FR-7.2: connection actions and status copy are visible.
        Skip.IfNot(IsConfigured(), "Set MOBILE_APPIUM_SERVER_URL and MOBILE_APP_PATH (and device settings) to run mobile UI tests.");
        EnsureDriver();

        _driver.Should().NotBeNull();
        _driver!.FindElement(MobileBy.XPath("//*[@text='Terminate Session']")).Should().NotBeNull();
        _driver.FindElement(MobileBy.XPath("//*[@text='Connect to Server']")).Should().NotBeNull();
        _driver.FindElement(MobileBy.XPath("//*[@text='Establish a session before entering chat.']")).Should().NotBeNull();
    }

    [SkippableFact]
    public void ConnectionView_PortField_ShouldDefaultToExpectedPort()
    {
        // FR-2.4: default endpoint configuration is prefilled for quick connection.
        Skip.IfNot(IsConfigured(), "Set MOBILE_APPIUM_SERVER_URL and MOBILE_APP_PATH (and device settings) to run mobile UI tests.");
        EnsureDriver();

        _driver.Should().NotBeNull();
        var portInput = _driver!.FindElement(MobileBy.AccessibilityId("mobile_connect_port"));
        var value = portInput.Text;
        value.Should().NotBeNullOrWhiteSpace();
        value.Should().Contain("5243");
    }

    private static bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOBILE_APPIUM_SERVER_URL"))
            && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOBILE_APP_PATH"));
    }

    private void EnsureDriver()
    {
        if (_driver != null)
            return;

        var serverUrl = Environment.GetEnvironmentVariable("MOBILE_APPIUM_SERVER_URL")!;
        var appPath = Environment.GetEnvironmentVariable("MOBILE_APP_PATH")!;
        var deviceName = Environment.GetEnvironmentVariable("MOBILE_DEVICE_NAME") ?? "Android";
        var udid = Environment.GetEnvironmentVariable("MOBILE_DEVICE_UDID");

        WaitForAndroidServiceReady(udid);

        var options = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2",
            DeviceName = deviceName,
            App = appPath
        };

        if (!string.IsNullOrWhiteSpace(udid))
            options.AddAdditionalAppiumOption("udid", udid);

        _driver = new AndroidDriver(new Uri(serverUrl), options, TimeSpan.FromSeconds(120));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
    }

    private static void WaitForAndroidServiceReady(string? udid)
    {
        var timeoutAt = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < timeoutAt)
        {
            var args = string.IsNullOrWhiteSpace(udid)
                ? "shell service check settings"
                : $"-s {udid} shell service check settings";

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            if (process is not null)
            {
                process.WaitForExit(5000);
                var output = process.StandardOutput.ReadToEnd();
                if (process.ExitCode == 0
                    && output.Contains("service settings: found", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            Thread.Sleep(2000);
        }
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
        catch
        {
            // ignore driver shutdown errors during test cleanup
        }
    }
}
