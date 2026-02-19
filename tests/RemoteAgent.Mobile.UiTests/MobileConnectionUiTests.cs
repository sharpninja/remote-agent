using FluentAssertions;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium;
using System.Diagnostics;

namespace RemoteAgent.Mobile.UiTests;

/// <summary>
/// Mobile UI smoke coverage for FR-1.1, FR-1.6, FR-2.4, FR-2.7, FR-7.2 and TR-2.1, TR-5.2, TR-5.8, TR-8.5 via Appium-hosted Android UI tests.
/// </summary>
[Trait("Category", "Requirements")]
[Trait("Requirement", "FR-1.1")]
[Trait("Requirement", "FR-1.6")]
[Trait("Requirement", "FR-2.4")]
[Trait("Requirement", "FR-2.7")]
[Trait("Requirement", "FR-7.2")]
[Trait("Requirement", "TR-2.1")]
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
        FindByAutomationId("mobile_connect_host").Should().NotBeNull();
        FindByAutomationId("mobile_connect_port").Should().NotBeNull();
        FindByAutomationId("mobile_connect_button").Should().NotBeNull();
        FindByAutomationId("mobile_new_session_button").Should().NotBeNull();
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
    public void ConnectionView_PortPicker_ShouldDefaultToWindowsPort()
    {
        // FR-2.4: default endpoint configuration is prefilled for quick connection.
        Skip.IfNot(IsConfigured(), "Set MOBILE_APPIUM_SERVER_URL and MOBILE_APP_PATH (and device settings) to run mobile UI tests.");
        EnsureDriver();

        _driver.Should().NotBeNull();
        var portPicker = FindByAutomationId("mobile_connect_port");
        var value = portPicker.Text;
        value.Should().NotBeNullOrWhiteSpace();
        value.Should().Be("5244", "5244 is the Windows service port and the expected default");
    }

    [SkippableFact]
    public void ConnectionView_PortPicker_ShouldNotBeAnEditableTextField()
    {
        // Enforces that the port control is a Picker (combo box), not a free-text Entry.
        // On Android, Entry renders as android.widget.EditText; Picker renders as a non-editable view.
        Skip.IfNot(IsConfigured(), "Set MOBILE_APPIUM_SERVER_URL and MOBILE_APP_PATH (and device settings) to run mobile UI tests.");
        EnsureDriver();

        _driver.Should().NotBeNull();
        var portPicker = FindByAutomationId("mobile_connect_port");
        var className = portPicker.GetAttribute("className") ?? "";
        className.Should().NotBe("android.widget.EditText",
            "the port control must be a Picker (combo box), not a free-text Entry, to restrict input to known port values");
    }

    [SkippableFact]
    public void ConnectionView_PortPicker_ShouldExposeWindowsAndLinuxPorts()
    {
        // Verifies the picker drop-down contains both well-known ports.
        Skip.IfNot(IsConfigured(), "Set MOBILE_APPIUM_SERVER_URL and MOBILE_APP_PATH (and device settings) to run mobile UI tests.");
        EnsureDriver();

        _driver.Should().NotBeNull();

        // Open the picker by tapping it.
        FindByAutomationId("mobile_connect_port").Click();

        // Wait briefly for the Android dialog to appear then search for both port labels.
        Thread.Sleep(500);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        IWebElement? windows5244 = null;
        IWebElement? linux5243   = null;
        while (!cts.IsCancellationRequested && (windows5244 is null || linux5243 is null))
        {
            try { windows5244 = _driver!.FindElement(MobileBy.XPath("//*[@text='5244']")); } catch { /* not yet visible */ }
            try { linux5243   = _driver!.FindElement(MobileBy.XPath("//*[@text='5243']")); } catch { /* not yet visible */ }
            if (windows5244 is null || linux5243 is null) Thread.Sleep(200);
        }

        windows5244.Should().NotBeNull("5244 (Windows service port) must appear as a picker option");
        linux5243.Should().NotBeNull("5243 (Linux/Docker service port) must appear as a picker option");

        // Dismiss the dialog without changing selection.
        try { _driver!.PressKeyCode(4); /* BACK */ } catch { /* ignore */ }
    }

    private IWebElement FindByAutomationId(string id)
    {
        _driver.Should().NotBeNull();

        try
        {
            return _driver!.FindElement(MobileBy.AccessibilityId(id));
        }
        catch (NoSuchElementException)
        {
            return _driver!.FindElement(MobileBy.Id(id));
        }
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
        var appPackage = Environment.GetEnvironmentVariable("MOBILE_APP_PACKAGE") ?? "com.companyname.remoteagent.app";
        var appActivity = Environment.GetEnvironmentVariable("MOBILE_APP_ACTIVITY") ?? "crc6411305d2bb8acc544.MainActivity";
        var appWaitActivity = Environment.GetEnvironmentVariable("MOBILE_APP_WAIT_ACTIVITY") ?? appActivity;

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
        options.AddAdditionalAppiumOption("appPackage", appPackage);
        options.AddAdditionalAppiumOption("appActivity", appActivity);
        options.AddAdditionalAppiumOption("appWaitActivity", appWaitActivity);
        options.AddAdditionalAppiumOption("adbExecTimeout", 120000);
        options.AddAdditionalAppiumOption("appWaitDuration", 180000);
        options.AddAdditionalAppiumOption("androidInstallTimeout", 180000);
        options.AddAdditionalAppiumOption("uiautomator2ServerLaunchTimeout", 240000);
        options.AddAdditionalAppiumOption("newCommandTimeout", 300);
        options.AddAdditionalAppiumOption("noReset", true);
        options.AddAdditionalAppiumOption("fullReset", false);

        _driver = new AndroidDriver(new Uri(serverUrl), options, TimeSpan.FromSeconds(300));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
    }

    private static void WaitForAndroidServiceReady(string? udid)
    {
        var adbPath = ResolveAdbPath();
        var timeoutAt = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < timeoutAt)
        {
            var args = string.IsNullOrWhiteSpace(udid)
                ? "shell service check settings"
                : $"-s {udid} shell service check settings";

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = adbPath,
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

    private static string ResolveAdbPath()
    {
        var explicitPath = Environment.GetEnvironmentVariable("MOBILE_ADB_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            return explicitPath;

        var sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (!string.IsNullOrWhiteSpace(sdkRoot))
        {
            var adb = Path.Combine(sdkRoot, "platform-tools", "adb");
            if (File.Exists(adb))
                return adb;

            var adbExe = Path.Combine(sdkRoot, "platform-tools", "adb.exe");
            if (File.Exists(adbExe))
                return adbExe;
        }

        return "adb";
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
