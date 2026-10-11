param([string]$BuildDirectory='bin\Beta6')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing,System.Windows.Forms,System.Net.Http
$dir=(Resolve-Path $BuildDirectory).Path
Add-Type -TypeDefinition @"
using System; using System.IO; using System.Reflection;
public static class VisualTestResolver { public static void Register(string directory) { AppDomain.CurrentDomain.AssemblyResolve += (sender,args) => { string path=Path.Combine(directory,new AssemblyName(args.Name).Name+".dll"); return File.Exists(path)?Assembly.LoadFrom(path):null; }; } }
"@
[VisualTestResolver]::Register($dir)
[Reflection.Assembly]::LoadFrom((Join-Path $dir 'PoiTime.exe')) | Out-Null
$c=New-Object ReportTimePlayer.Config
$c.backgroundOpacity=0; $c.showName=$false; $c.showPortrait=$false; $c.showLine=$false
$b=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('name','line','',$c)
for($x=0;$x -lt 360;$x+=7){for($y=0;$y -lt 250;$y+=7){if($b.GetPixel($x,$y).A -ne 0){throw 'hidden elements still rendered'}}}; $b.Dispose()
$c.showLine=$true; $c.textColor='#FF0000'; $c.fontFamily='Arial'; $c.fontSize=10
function ColoredPixels($bitmap){$count=0;for($x=0;$x -lt 360;$x++){for($y=0;$y -lt 250;$y++){$p=$bitmap.GetPixel($x,$y);if($p.A -gt 150 -and $p.R -gt 200 -and $p.G -lt 40 -and $p.B -lt 40){$count++}}};return $count}
$small=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('','TEST TEXT','',$c); $smallCount=ColoredPixels $small; $small.Dispose()
$c.fontSize=24; $c.fontBold=$true
$b=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('','TEST TEXT','',$c); $largeCount=ColoredPixels $b
if($smallCount -le 0 -or $largeCount -le $smallCount){throw 'font/color settings not applied'}
$b.Save((Join-Path $dir 'visual-text-check.png')); $stream=New-Object IO.MemoryStream; $b.Save($stream,[Drawing.Imaging.ImageFormat]::Png); $png=$stream.ToArray(); $stream.Dispose(); $b.Dispose()
$probe=New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,0);$probe.Start();$port=$probe.LocalEndpoint.Port;$probe.Stop()
$server=New-Object ReportTimePlayer.BrowserAudioServer($port);$http=New-Object Net.Http.HttpClient
try{
  $base=$http.GetStringAsync($server.Url+'status').Result | ConvertFrom-Json
  $server.Play($null,34,$true,$png,'TopLeft',([TimeSpan]::FromSeconds(5)))
  $event=$http.GetStringAsync($server.Url+'events?after='+$base.sequence).Result | ConvertFrom-Json
  if($event.url -ne $null -or !$event.visualUrl -or $event.position -ne 'TopLeft' -or $event.durationMs -ne 5500){throw 'visual-only event metadata incorrect'}
  $response=$http.GetAsync($server.Url+$event.visualUrl).Result
  if($response.Content.Headers.ContentType.MediaType -ne 'image/png'){throw 'incorrect image MIME'}
  $actual=$response.Content.ReadAsByteArrayAsync().Result
  if([Convert]::ToBase64String($actual) -ne [Convert]::ToBase64String($png)){throw 'web image differs from desktop renderer'}
  if([int]$http.GetAsync($server.Url+'image/unknown').Result.StatusCode -ne 404){throw 'unregistered image accessible'}
  $server.StopPreview()
  $stop=$http.GetStringAsync($server.Url+'events?after='+$event.sequence).Result | ConvertFrom-Json
  if($stop.kind -ne 'stopPreview'){throw 'visual stop event missing'}
}finally{$http.Dispose();$server.Dispose()}
$get=[Func[int,string]]{param($h) return "Original-$h"};$preview=[Action[int,ReportTimePlayer.Config,string]]{param($h,$draft,$text)};$stopAction=[Action]{}
$form=New-Object ReportTimePlayer.AnnouncementSettingsForm($c,$preview,$stopAction,$get)
$flags=[Reflection.BindingFlags]'Instance,NonPublic';$type=$form.GetType()
$editor=$type.GetField('lineEditor',$flags).GetValue($form);$hour=$type.GetField('hour',$flags).GetValue($form)
$hour.Value=12;$editor.Text='Edited at twelve';$hour.Value=13
if($editor.Text -ne 'Original-13'){throw 'hour editor failed to switch'}
$hour.Value=12
if($editor.Text -ne 'Edited at twelve' -or $form.EditedLines[12] -ne 'Edited at twelve'){throw 'staged dialogue lost'}
if($form.Draft.fontSize -ne 24 -or [Drawing.ColorTranslator]::FromHtml($form.Draft.textColor).ToArgb() -ne [Drawing.Color]::Red.ToArgb() -or !$form.Draft.browserShowAnnouncement){throw 'appearance draft not retained'}
$form.Show();[Windows.Forms.Application]::DoEvents(); $tabs=$form.Controls | Where-Object {$_ -is [Windows.Forms.TabControl]};$tabs.SelectedIndex=1
$shot=New-Object Drawing.Bitmap($form.Width,$form.Height);$form.DrawToBitmap($shot,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)));$shot.Save((Join-Path $dir 'settings-visual-check.png'));$shot.Dispose();$form.Close();$form.Dispose()
'PASS: visibility controls, independent background alpha, font size/style/color, identical PNG transfer, visual-only events/stop/position/duration, staged hourly dialogue editor and draft'
