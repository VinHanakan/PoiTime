using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ReportTimePlayer
{
    public sealed class BrowserAudioServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly object gate = new object();
        private readonly List<BrowserEvent> events = new List<BrowserEvent>();
        private readonly Dictionary<string, AudioAsset> assets = new Dictionary<string, AudioAsset>();
        private readonly HashSet<TcpClient> connections = new HashSet<TcpClient>();
        private long sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        private DateTime lastPoll = DateTime.MinValue;
        public string Url { get; }
        public bool HasListener { get { lock (gate) return DateTime.UtcNow - lastPoll < TimeSpan.FromSeconds(3); } }

        public BrowserAudioServer(int port)
        {
            if (port < 1024 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            Url = "http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture) + "/";
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            _ = AcceptAsync();
        }
        public void Play(string path, int volume, bool preview)
        {
            lock (gate)
            {
                Prune();
                string id = Guid.NewGuid().ToString("N");
                assets[id] = new AudioAsset { Path = path, Created = DateTime.UtcNow };
                events.Add(new BrowserEvent { sequence = ++sequence, kind = "play", url = "audio/" + id, volume = Math.Max(0, Math.Min(100, volume)) / 100.0, preview = preview, Created = DateTime.UtcNow });
            }
        }
        public void StopPreview()
        {
            lock (gate) { Prune(); events.Add(new BrowserEvent { sequence = ++sequence, kind = "stopPreview", Created = DateTime.UtcNow }); }
        }
        private void Prune()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-2);
            events.RemoveAll(e => e.Created < cutoff);
            if (events.Count > 100) events.RemoveRange(0, events.Count - 100);
            foreach (var key in assets.Where(a => a.Value.Created < cutoff).Select(a => a.Key).ToArray()) assets.Remove(key);
        }
        private async Task AcceptAsync()
        {
            while (!shutdown.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    lock (gate) { if (shutdown.IsCancellationRequested) { client.Dispose(); break; } connections.Add(client); }
                    _ = ServeAsync(client);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { if (shutdown.IsCancellationRequested) break; }
            }
        }
        private async Task ServeAsync(TcpClient client)
        {
            try
            {
                client.ReceiveTimeout = 5000;
                using (var stream = client.GetStream())
                {
                    var header = new List<byte>();
                    var one = new byte[1];
                    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token))
                    {
                        timeout.CancelAfter(5000);
                        while (header.Count < 8192)
                        {
                            if (await stream.ReadAsync(one, 0, 1, timeout.Token).ConfigureAwait(false) == 0) return;
                            header.Add(one[0]);
                            int n = header.Count;
                            if (n >= 4 && header[n - 4] == 13 && header[n - 3] == 10 && header[n - 2] == 13 && header[n - 1] == 10) break;
                        }
                    }
                    string[] lines = Encoding.ASCII.GetString(header.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string[] request = lines[0].Split(' ');
                    if (request.Length != 3 || request[0] != "GET") { await Respond(stream, 405, "text/plain", Encoding.UTF8.GetBytes("GET only")); return; }
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string line in lines.Skip(1)) { int colon = line.IndexOf(':'); if (colon > 0) headers[line.Substring(0, colon)] = line.Substring(colon + 1).Trim(); }
                    if (!headers.TryGetValue("Host", out var host) || host != new Uri(Url).Authority ||
                        (headers.TryGetValue("Origin", out var origin) && origin != Url.TrimEnd('/')))
                    { await Respond(stream, 403, "text/plain", new byte[0]); return; }
                    var uri = new Uri(Url.TrimEnd('/') + request[1]);
                    if (uri.AbsolutePath == "/")
                    {
                        using (var source = typeof(BrowserAudioServer).Assembly.GetManifestResourceStream("PoiTime.BrowserSource.html"))
                        using (var reader = new StreamReader(source)) await Respond(stream, 200, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(reader.ReadToEnd()));
                    }
                    else if (uri.AbsolutePath == "/status")
                    {
                        long current; lock (gate) current = sequence;
                        await Json(stream, new { sequence = current, connected = HasListener });
                    }
                    else if (uri.AbsolutePath == "/events")
                    {
                        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        long.TryParse(query["after"], out long after);
                        BrowserEvent[] pending;
                        lock (gate) { lastPoll = DateTime.UtcNow; Prune(); pending = events.Where(e => e.sequence > after && e.Created > DateTime.UtcNow.AddSeconds(-15)).ToArray(); }
                        await Json(stream, pending);
                    }
                    else if (uri.AbsolutePath.StartsWith("/audio/", StringComparison.Ordinal))
                    {
                        AudioAsset asset; lock (gate) assets.TryGetValue(uri.AbsolutePath.Substring(7), out asset);
                        if (asset == null || !File.Exists(asset.Path)) { await Respond(stream, 404, "text/plain", new byte[0]); return; }
                        var bytes = File.ReadAllBytes(asset.Path);
                        string mime = Path.GetExtension(asset.Path).Equals(".wav", StringComparison.OrdinalIgnoreCase) ? "audio/wav" : "audio/mpeg";
                        await Respond(stream, 200, mime, bytes);
                    }
                    else await Respond(stream, 404, "text/plain", new byte[0]);
                }
            }
            catch (Exception) { /* Disconnected browser requests must not interrupt scheduled playback. */ }
            finally { lock (gate) connections.Remove(client); client.Dispose(); }
        }
        private Task Json(NetworkStream stream, object value) => Respond(stream, 200, "application/json", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
        private async Task Respond(NetworkStream stream, int status, string mime, byte[] body)
        {
            string reason = status == 200 ? "OK" : status == 404 ? "Not Found" : status == 403 ? "Forbidden" : "Method Not Allowed";
            var header = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + " " + reason + "\r\nContent-Type: " + mime + "\r\nContent-Length: " + body.Length + "\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header, 0, header.Length, shutdown.Token).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, shutdown.Token).ConfigureAwait(false);
        }
        public void Dispose()
        {
            if (shutdown.IsCancellationRequested) return;
            shutdown.Cancel(); listener.Stop();
            lock (gate) { foreach (var client in connections) client.Dispose(); connections.Clear(); events.Clear(); assets.Clear(); }
        }
        public sealed class BrowserEvent
        {
            public long sequence { get; set; }
            public string kind { get; set; }
            public string url { get; set; }
            public double volume { get; set; }
            public bool preview { get; set; }
            [System.Text.Json.Serialization.JsonIgnore] public DateTime Created { get; set; }
        }
        private sealed class AudioAsset { public string Path; public DateTime Created; }
    }
}
