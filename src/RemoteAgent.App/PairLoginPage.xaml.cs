namespace RemoteAgent.App;

public partial class PairLoginPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();
    private bool _handled;

    public PairLoginPage(string loginUrl)
    {
        InitializeComponent();
        LoginWebView.Source = new UrlWebViewSource { Url = loginUrl };
    }

    public Task<string?> ResultTask => _tcs.Task;

    private async void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (_handled) return;
        if (!e.Url.Contains("/pair/key", StringComparison.OrdinalIgnoreCase)) return;

        // Extract the deep link href from the "Open in Remote Agent App" anchor.
        // EvaluateJavaScriptAsync may return a JSON-quoted string on Android; strip quotes if present.
        var href = await LoginWebView.EvaluateJavaScriptAsync(
            "document.querySelector('a.btn')?.getAttribute('href')");

        if (string.IsNullOrWhiteSpace(href) || string.Equals(href, "null", StringComparison.Ordinal))
            return;

        if (href.Length >= 2 && href[0] == '"' && href[^1] == '"')
            href = href[1..^1];

        if (string.IsNullOrWhiteSpace(href)) return;

        _handled = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _tcs.TrySetResult(href);
            await Navigation.PopModalAsync();
        });
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _handled = true;
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync();
    }
}
