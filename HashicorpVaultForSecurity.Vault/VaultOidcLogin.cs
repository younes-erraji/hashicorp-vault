using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace HashicorpVaultForSecurity.Vault;

/// <summary>
/// Logs a user in to Vault through an OIDC provider and returns a Vault token.
///
/// This is the interactive flow the Vault CLI uses (<c>vault login -method=oidc</c>):
/// it opens the user's browser, the user signs in with their own credentials,
/// the provider redirects back to a local listener, and Vault hands back a token.
///
/// Because it opens a browser and listens on localhost, this is meant for
/// CLI / desktop / local-dev tools, not for a hosted web server.
/// </summary>
public static class VaultOidcLogin
{
    /// <summary>
    /// Runs the interactive OIDC login and returns the Vault client token for the signed-in user.
    /// </summary>
    /// <param name="address">The Vault server URL, e.g. <c>http://127.0.0.1:8200</c>.</param>
    /// <param name="oidc">OIDC mount, role and redirect settings.</param>
    /// <param name="namespace">Optional Vault namespace (Enterprise / HCP).</param>
    /// <param name="cancellationToken">Token used to cancel while waiting for the browser login.</param>
    /// <returns>The Vault client token for the logged-in user.</returns>
    public static async Task<string> LoginAsync(
        string address,
        VaultOidcSettings oidc,
        string? @namespace = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentNullException.ThrowIfNull(oidc);
        ArgumentException.ThrowIfNullOrWhiteSpace(oidc.RedirectUri);

        using HttpClient http = new HttpClient { BaseAddress = new Uri(address) };
        if (!string.IsNullOrWhiteSpace(@namespace))
        {
            http.DefaultRequestHeaders.Add("X-Vault-Namespace", @namespace);
        }

        // Start the local listener BEFORE opening the browser, so we never miss the redirect.
        using HttpListener listener = CreateListener(oidc.RedirectUri);
        listener.Start();

        try
        {
            // Step 1: ask Vault for the provider's authorization URL.
            string authUrl = await GetAuthUrlAsync(http, oidc, cancellationToken);

            // Step 2: open the browser so the user can sign in.
            OpenBrowser(authUrl);

            // Step 3: wait for the provider -> Vault redirect to hit our listener.
            (string state, string code, string nonce) = await WaitForCallbackAsync(listener, cancellationToken);

            // Step 4: exchange the code with Vault for a token.
            return await ExchangeCodeForTokenAsync(http, oidc, state, code, nonce, cancellationToken);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static HttpListener CreateListener(string redirectUri)
    {
        // HttpListener needs the prefix to end with '/', e.g. "http://localhost:8250/oidc/".
        Uri uri = new Uri(redirectUri);
        string prefix = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath.TrimEnd('/')}/";

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        return listener;
    }

    private static async Task<string> GetAuthUrlAsync(
        HttpClient http, VaultOidcSettings oidc, CancellationToken cancellationToken)
    {
        JObject body = new JObject
        {
            ["redirect_uri"] = oidc.RedirectUri,
            ["role"] = oidc.Role,
        };

        using StringContent content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await http.PostAsync(
            $"/v1/auth/{oidc.MountPoint}/oidc/auth_url", content, cancellationToken);

        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Vault returned {(int)response.StatusCode} requesting the OIDC auth URL: {payload}");
        }

        string? authUrl = JObject.Parse(payload)["data"]?["auth_url"]?.ToString();
        if (string.IsNullOrWhiteSpace(authUrl))
        {
            throw new InvalidOperationException("Vault did not return an OIDC auth URL.");
        }

        return authUrl;
    }

    private static async Task<(string State, string Code, string Nonce)> WaitForCallbackAsync(
        HttpListener listener, CancellationToken cancellationToken)
    {
        // GetContextAsync has no cancellation overload; abort it if the caller cancels.
        Task<HttpListenerContext> contextTask = listener.GetContextAsync();
        using (cancellationToken.Register(listener.Abort))
        {
            HttpListenerContext context = await contextTask;

            IReadOnlyDictionary<string, string> query =
                ParseQuery(context.Request.Url?.Query ?? string.Empty);

            query.TryGetValue("state", out string? state);
            query.TryGetValue("code", out string? code);
            string nonce = query.TryGetValue("nonce", out string? n) ? n : string.Empty;

            await WriteBrowserResponseAsync(context, state, code);

            if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException(
                    "The OIDC redirect did not include the expected 'state' and 'code' values.");
            }

            return (state, code, nonce);
        }
    }

    private static async Task WriteBrowserResponseAsync(
        HttpListenerContext context, string? state, string? code)
    {
        bool ok = !string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(code);
        string message = ok
            ? "Login complete. You can close this tab and return to the app."
            : "Login failed. Please return to the app and try again.";

        byte[] html = Encoding.UTF8.GetBytes(
            $"<html><body style='font-family:sans-serif'>{message}</body></html>");

        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = html.Length;
        await context.Response.OutputStream.WriteAsync(html);
        context.Response.OutputStream.Close();
    }

    private static async Task<string> ExchangeCodeForTokenAsync(
        HttpClient http, VaultOidcSettings oidc, string state, string code, string nonce,
        CancellationToken cancellationToken)
    {
        string callback =
            $"/v1/auth/{oidc.MountPoint}/oidc/callback" +
            $"?state={Uri.EscapeDataString(state)}" +
            $"&code={Uri.EscapeDataString(code)}" +
            $"&nonce={Uri.EscapeDataString(nonce)}";

        using HttpResponseMessage response = await http.GetAsync(callback, cancellationToken);
        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Vault returned {(int)response.StatusCode} exchanging the OIDC code for a token: {payload}");
        }

        string? token = JObject.Parse(payload)["auth"]?["client_token"]?.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Vault did not return a client token after OIDC login.");
        }

        return token;
    }

    private static void OpenBrowser(string url)
    {
        // UseShellExecute lets the OS pick the default browser.
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    /// <summary>
    /// Parses a URL query string (e.g. <c>?state=a&amp;code=b</c>) into a dictionary,
    /// URL-decoding each value.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = pair.IndexOf('=');
            if (separator < 0)
            {
                result[Uri.UnescapeDataString(pair)] = string.Empty;
                continue;
            }

            string key = Uri.UnescapeDataString(pair[..separator]);
            string value = Uri.UnescapeDataString(pair[(separator + 1)..]);
            result[key] = value;
        }

        return result;
    }
}
