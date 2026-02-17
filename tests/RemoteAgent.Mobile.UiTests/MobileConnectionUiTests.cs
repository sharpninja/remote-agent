using FluentAssertions;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium;

namespace RemoteAgent.Mobile.UiTests;

public sealed class MobileConnectionUiTests : IDisposable
{
    private AndroidDriver? _driver;

    [SkippableFact]
    public void ConnectionView_ShouldExposeCoreControls()
    {
        Skip.IfNot(IsConfigured(), "Set MOBILE_APPIUM_SERVER_URL and MOBILE_APP_PATH (and device settings) to run mobile UI tests.");
        EnsureDriver();

        _driver.Should().NotBeNull();
        _driver!.FindElement(MobileBy.AccessibilityId("mobile_connect_host")).Should().NotBeNull();
        _driver.FindElement(MobileBy.AccessibilityId("mobile_connect_port")).Should().NotBeNull();
        _driver.FindElement(MobileBy.AccessibilityId("mobile_connect_button")).Should().NotBeNull();
        _driver.FindElement(MobileBy.AccessibilityId("mobile_new_session_button")).Should().NotBeNull();
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

        var options = new AppiumOptions
        {
            PlatformName = "Android"
        };
        options.AddAdditionalAppiumOption("automationName", "UiAutomator2");
        options.AddAdditionalAppiumOption("deviceName", deviceName);
        options.AddAdditionalAppiumOption("app", appPath);

        if (!string.IsNullOrWhiteSpace(udid))
            options.AddAdditionalAppiumOption("udid", udid);

        _driver = new AndroidDriver(new Uri(serverUrl), options, TimeSpan.FromSeconds(120));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
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
