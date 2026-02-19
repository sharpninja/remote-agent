using System.Net;

namespace RemoteAgent.Service.Web;

/// <summary>Inline HTML templates for the <c>/pair</c> device-pairing web flow.</summary>
internal static class PairingHtml
{
    private const string LoginTemplate = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>Remote Agent — Pair Device</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { font-family: system-ui, sans-serif; max-width: 400px; margin: 80px auto; padding: 0 16px; color: #1a1a1a; }
            h2  { margin-bottom: 4px; }
            p   { margin-top: 0; color: #555; }
            input { display: block; width: 100%; padding: 10px 12px; margin: 6px 0; border: 1px solid #ccc; border-radius: 6px; font-size: 15px; }
            input:focus { outline: none; border-color: #0078d4; box-shadow: 0 0 0 3px rgba(0,120,212,.15); }
            button { width: 100%; padding: 12px; background: #0078d4; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 16px; margin-top: 8px; }
            button:hover { background: #005a9e; }
            .msg { padding: 10px 12px; border-radius: 6px; margin-bottom: 12px; font-size: 14px; }
            .error { background: #fff0f0; color: #c00; border: 1px solid #fcc; }
            .info  { background: #f0f4ff; color: #003; border: 1px solid #aac; }
            code   { background: #eee; padding: 1px 4px; border-radius: 3px; }
          </style>
        </head>
        <body>
          <h2>Pair Remote Agent</h2>
          <p>Sign in to retrieve your device API key.</p>
          %%MSG%%
          %%FORM%%
        </body>
        </html>
        """;

    private const string KeyTemplate = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>Remote Agent — API Key</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { font-family: system-ui, sans-serif; max-width: 420px; margin: 40px auto; padding: 0 16px; text-align: center; color: #1a1a1a; }
            h2  { margin-bottom: 4px; }
            p   { color: #555; margin-top: 0; }
            #qr { margin: 16px auto; }
            #qr canvas { border-radius: 8px; }
            .key { font-family: monospace; background: #f5f5f5; padding: 12px; border-radius: 6px; word-break: break-all; margin: 16px 0; font-size: 13px; border: 1px solid #ddd; text-align: left; }
            .btn { display: block; padding: 14px; background: #0078d4; color: white; border-radius: 6px; text-decoration: none; font-size: 16px; margin-top: 8px; }
            .btn:hover { background: #005a9e; }
            .warn { padding: 10px 12px; border-radius: 6px; background: #fffbe0; color: #7a6000; border: 1px solid #e6d800; font-size: 14px; }
          </style>
        </head>
        <body>
          <h2>Your API Key</h2>
          %%BODY%%
          <div id="qr"></div>
          <script src="https://cdn.jsdelivr.net/npm/qrcode@1.5.4/build/qrcode.min.js"></script>
          <script>
            (function() {
              var data = %%QR_DATA%%;
              if (!data) return;
              QRCode.toCanvas(document.createElement('canvas'), data, { width: 256 }, function(err, c) {
                if (!err) { document.getElementById('qr').appendChild(c); }
              });
            })();
          </script>
        </body>
        </html>
        """;

    public static string LoginPage(bool error = false, bool noPairingUsers = false)
    {
        var msg = noPairingUsers
            ? "<div class=\"msg info\">No pairing users are configured. " +
              "Add <code>PairingUsers</code> to the <code>Agent</code> section in <code>appsettings.json</code>.</div>"
            : error
                ? "<div class=\"msg error\">Invalid username or password.</div>"
                : "";

        var form = noPairingUsers ? "" : """
            <form method="post">
              <input name="username" placeholder="Username" required autofocus autocomplete="username">
              <input name="password" type="password" placeholder="Password" required autocomplete="current-password">
              <button type="submit">Sign in</button>
            </form>
            """;

        return LoginTemplate.Replace("%%MSG%%", msg).Replace("%%FORM%%", form);
    }

    public static string KeyPage(string apiKey, string deepLink)
    {
        string body, qrData;

        if (string.IsNullOrEmpty(apiKey))
        {
            body = "<div class=\"warn\">No API key is configured on this server. " +
                   "Set <code>Agent:ApiKey</code> in <code>appsettings.json</code>.</div>";
            qrData = "null";
        }
        else
        {
            var encodedKey  = WebUtility.HtmlEncode(apiKey);
            var encodedLink = WebUtility.HtmlEncode(deepLink);
            body = $"<p>Scan the QR code with your Remote Agent app, or tap <strong>Open in App</strong>.</p>" +
                   $"<div class=\"key\">{encodedKey}</div>" +
                   $"<a class=\"btn\" href=\"{encodedLink}\">Open in Remote Agent App</a>";
            // Serialize as JSON string — safe to embed directly in <script>.
            qrData = System.Text.Json.JsonSerializer.Serialize(deepLink);
        }

        return KeyTemplate.Replace("%%BODY%%", body).Replace("%%QR_DATA%%", qrData);
    }
}
