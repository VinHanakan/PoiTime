param([string]$BuildDirectory='bin\Beta4')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Net.Http
$dir=(Resolve-Path $BuildDirectory).Path
Add-Type -TypeDefinition @"
using System; using System.IO; using System.Reflection;
public static class BrowserTestResolver {
    public static void Register(string directory) {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
            string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
    }
}
"@
[BrowserTestResolver]::Register($dir)
[Reflection.Assembly]::LoadFrom((Join-Path $dir 'PoiTime.exe')) | Out-Null
$probe=New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,0); $probe.Start(); $port=$probe.LocalEndpoint.Port; $probe.Stop()
$server=New-Object ReportTimePlayer.BrowserAudioServer($port)
$client=New-Object Net.Http.HttpClient
try {
    $url=$server.Url

    $html=$client.GetStringAsync($url).Result
    if(!$html.Contains('background:transparent') -or !$html.Contains('item.audio.play()')){throw 'browser page missing'}

    $baseline=$client.GetStringAsync($url+'status').Result | ConvertFrom-Json
    if($baseline.sequence -le 0){throw 'missing initial cursor'}

    $events=$client.GetStringAsync($url+'events?after='+$baseline.sequence).Result | ConvertFrom-Json
    if($events.Count -ne 0 -or !$server.HasListener){throw 'initial connection failed'}
    $path=Join-Path $dir 'browser-audio-test.wav'; $bytes=[byte[]](1,2,3,4,5); [IO.File]::WriteAllBytes($path,$bytes)

    $server.Play($path,34,$true)
    $rawEvents=$client.GetStringAsync($url+'events?after='+$baseline.sequence).Result;  $events=($rawEvents | ConvertFrom-Json)

    if(@($events).Count -ne 1 -or [Math]::Abs($events.volume - .34) -gt .00001 -or !$events.preview){throw 'play event incorrect'}
    $audio=$client.GetByteArrayAsync($url+$events.url).Result
    if([Convert]::ToBase64String($audio) -ne [Convert]::ToBase64String($bytes)){throw 'audio asset mismatch'}
    $server.StopPreview()
    $next=($client.GetStringAsync($url+'events?after='+$events.sequence).Result | ConvertFrom-Json)
    if(@($next).Count -ne 1 -or $next.kind -ne 'stopPreview'){throw 'stop event missing'}
    $current=$client.GetStringAsync($url+'status').Result | ConvertFrom-Json
    $empty=($client.GetStringAsync($url+'events?after='+$current.sequence).Result | ConvertFrom-Json)
    if($empty.Count -ne 0){throw 'page refresh replays old audio'}
    $request=New-Object Net.Http.HttpRequestMessage([Net.Http.HttpMethod]::Get,($url+'events?after='+$baseline.sequence))
    $request.Headers.Add('Origin','https://example.com')
    if([int]$client.SendAsync($request).Result.StatusCode -ne 403){throw 'cross-origin access accepted'}
    if([int]$client.GetAsync($url+'audio/unknown').Result.StatusCode -ne 404){throw 'unknown asset not rejected'}
    $field=$server.GetType().GetField('listener',[Reflection.BindingFlags]'Instance,NonPublic')
    if($field.GetValue($server).LocalEndpoint.Address.ToString() -ne '127.0.0.1'){throw 'server is not loopback-only'}
    'PASS: loopback-only HTTP, embedded transparent page, play/volume/stop events, exact audio bytes, refresh cursor, origin protection'
} finally {$client.Dispose(); $server.Dispose(); $server.Dispose()}
