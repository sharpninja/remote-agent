using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace RemoteAgent.App;

public partial class QrScannerPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();
    private bool _handled;

    public QrScannerPage()
    {
        InitializeComponent();
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false,
        };
    }

    public Task<string?> ResultTask => _tcs.Task;

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results.FirstOrDefault()?.Value;
        if (value == null || _handled) return;
        _handled = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _tcs.TrySetResult(value);
            await Navigation.PopModalAsync();
        });
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _handled = true;
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync();
    }
}
