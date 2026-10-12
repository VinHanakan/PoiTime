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
for($x=0;$x -lt $b.Width;$x+=7){for($y=0;$y -lt $b.Height;$y+=7){if($b.GetPixel($x,$y).A -ne 0){throw 'hidden elements still rendered'}}}; $b.Dispose()
$c.textOutline=$false; $c.showLine=$true; $c.textColor='#FF0000'; $c.fontFamily='Arial'; $c.fontSize=10
function ColoredPixels($bitmap){$count=0;for($x=0;$x -lt $bitmap.Width;$x++){for($y=0;$y -lt $bitmap.Height;$y++){$p=$bitmap.GetPixel($x,$y);if($p.A -gt 150 -and $p.R -gt 200 -and $p.G -lt 40 -and $p.B -lt 40){$count++}}};return $count}
$small=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('','TEST TEXT','',$c); $smallCount=ColoredPixels $small; $small.Dispose()
$c.fontSize=24; $c.fontBold=$true
$b=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('','TEST TEXT','',$c); $largeCount=ColoredPixels $b
if($smallCount -le 0 -or $largeCount -le $smallCount){throw 'font/color settings not applied'}
$b.Save((Join-Path $dir 'visual-text-check.png')); $stream=New-Object IO.MemoryStream; $b.Save($stream,[Drawing.Imaging.ImageFormat]::Png); $png=$stream.ToArray(); $stream.Dispose(); $b.Dispose()
$c.textOutline=$true; $c.textOutlineColor='#00FF00'; $c.textOutlineWidth=4
$outlined=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('','TEST TEXT','',$c)
$green=0;for($x=0;$x -lt $outlined.Width;$x++){for($y=0;$y -lt $outlined.Height;$y++){$p=$outlined.GetPixel($x,$y);if($p.A -gt 150 -and $p.G -gt 200 -and $p.R -lt 40){$green++}}}
if($green -le 0 -or (ColoredPixels $outlined) -le 0 -or $outlined.GetPixel(0,0).A -ne 0){throw 'outline color/fill/transparency incorrect'}
$outlined.Save((Join-Path $dir 'outline-check.png'));$outlined.Dispose()
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
if([ReportTimePlayer.VoiceAnnouncement]::FormatText('中文','日本語','Bilingual') -ne "中文`n`n日本語"){throw 'bilingual formatting failed'}
if([ReportTimePlayer.VoiceAnnouncement]::FormatText('中文','日本語','Japanese') -ne '日本語' -or [ReportTimePlayer.VoiceAnnouncement]::FormatText('中文','','Bilingual') -ne '中文' -or [ReportTimePlayer.VoiceAnnouncement]::FormatText('同文','同文','Bilingual') -ne '同文'){throw 'language selection/fallback failed'}
$wiki="{{台词翻译表`n|档名=106-1200`n|中文译文=中文<br/>台词`n|日文台词=日本語の台詞`n}}"
if([ReportTimePlayer.WikiVoicePackService]::ParseHourlyLines($wiki)['106-1200'] -ne '中文 台词' -or [ReportTimePlayer.WikiVoicePackService]::ParseHourlyLines($wiki,'日文台词')['106-1200'] -ne '日本語の台詞'){throw 'wiki bilingual parsing failed'}
$getJapanese=[Func[int,string]]{param($h) return "Japanese-$h"}
$c.regionWidth=480; $c.regionHeight=400; $c.regionScale=150; $c.positionOffsetX=-50; $c.positionOffsetY=30
$region=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('name','line','',$c)
if($region.Width -ne 720 -or $region.Height -ne 600){throw 'region dimensions/scale failed'};$region.Dispose()
$popup=New-Object ReportTimePlayer.AnnouncementPopup('name','line','',$c,([TimeSpan]::FromSeconds(3)))
$area=[Windows.Forms.Screen]::PrimaryScreen.WorkingArea
if($popup.Width -ne 720 -or $popup.Height -ne 600 -or $popup.Left -ne ($area.Right-720-16-50) -or $popup.Top -ne ($area.Bottom-600-16+30)){throw 'desktop region position failed'};$popup.Dispose()
$c.dialogueLanguage='Bilingual'
$get=[Func[int,string]]{param($h) return "Original-$h"};$preview=[Action[int,ReportTimePlayer.Config,string]]{param($h,$draft,$text)};$stopAction=[Action]{}
$form=New-Object ReportTimePlayer.AnnouncementSettingsForm($c,$preview,$stopAction,$get,$getJapanese)
$flags=[Reflection.BindingFlags]'Instance,NonPublic';$type=$form.GetType()
$editor=$type.GetField('lineEditor',$flags).GetValue($form);$hour=$type.GetField('hour',$flags).GetValue($form)
$jpEditor=$type.GetField('japaneseEditor',$flags).GetValue($form)
$hour.Value=12;$editor.Text='Edited at twelve';$jpEditor.Text='編集した台詞';$hour.Value=13
if($editor.Text -ne 'Original-13'){throw 'hour editor failed to switch'}
if($jpEditor.Text -ne 'Japanese-13'){throw 'Japanese editor switch failed'}
$hour.Value=12
if($jpEditor.Text -ne '編集した台詞' -or $form.EditedJapaneseLines[12] -ne '編集した台詞' -or $form.Draft.dialogueLanguage -ne 'Bilingual'){throw 'Japanese draft lost'}
if($editor.Text -ne 'Edited at twelve' -or $form.EditedLines[12] -ne 'Edited at twelve'){throw 'staged dialogue lost'}
if($form.Draft.fontSize -ne 24 -or [Drawing.ColorTranslator]::FromHtml($form.Draft.textColor).ToArgb() -ne [Drawing.Color]::Red.ToArgb() -or !$form.Draft.browserShowAnnouncement){throw 'appearance draft not retained'}
if(!$form.Draft.textOutline -or $form.Draft.textOutlineWidth -ne 4 -or [Drawing.ColorTranslator]::FromHtml($form.Draft.textOutlineColor).ToArgb() -ne [Drawing.Color]::Lime.ToArgb()){throw 'outline draft not retained'}
$form.Show();[Windows.Forms.Application]::DoEvents(); $tabs=$form.Controls | Where-Object {$_ -is [Windows.Forms.TabControl]};$tabs.SelectedIndex=3
$shot=New-Object Drawing.Bitmap($form.Width,$form.Height);$form.DrawToBitmap($shot,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)));$shot.Save((Join-Path $dir 'settings-visual-check.png'));$shot.Dispose();$form.Close();$form.Dispose()
'PASS: visibility controls, independent background alpha, font size/style/color, identical PNG transfer, visual-only events/stop/position/duration, staged hourly dialogue editor and draft'

$root=Join-Path $dir 'settings-test-voices'; New-Item -ItemType Directory -Path $root -Force | Out-Null
foreach($id in '1','2') { $folder=Join-Path $root $id;New-Item -ItemType Directory -Path $folder -Force | Out-Null; @{name="Pack-$id";voices=@(@{hour=12;minute=0;fileName="$id-1200.mp3";text="Chinese-$id";japaneseText="Japanese-$id"})} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $folder 'config.json') -Encoding UTF8 }
$ctx=[Runtime.Serialization.FormatterServices]::GetUninitializedObject([ReportTimePlayer.ReportTimeContext]); $ctxType=$ctx.GetType()
$ctxType.GetField('settingsVoicesRoot',$flags).SetValue($ctx,$root);$ctxType.GetField('currentId',$flags).SetValue($ctx,'1')
$form=New-Object ReportTimePlayer.AnnouncementSettingsForm($c,$preview,$stopAction,$get,$getJapanese,$ctx)
$editor=$type.GetField('lineEditor',$flags).GetValue($form);$hour=$type.GetField('hour',$flags).GetValue($form);$packs=$type.GetField('packBox',$flags).GetValue($form)
$hour.Value=12;if($editor.Text -ne 'Chinese-1'){throw 'selected pack editor not loaded'}
$editor.Text='Edited pack one';$packs.SelectedIndex=1;if($editor.Text -ne 'Chinese-2' -or $form.Draft.id -ne '2'){throw 'pack switch did not load selected dialogue'}
$editor.Text='Edited pack two';$packs.SelectedIndex=0;if($editor.Text -ne 'Edited pack one'){throw 'pack-specific edits lost'}
if($form.LinesForPack('2',$false)[12] -ne 'Edited pack two' -or @($form.EditedPackIds).Count -ne 2){throw 'edits crossed packs'}
if($ctx.ReadVoicePack('1').voices[0].text -ne 'Chinese-1'){throw 'unsaved edit written to disk'}
if($form.Draft.regionWidth -ne 480 -or $form.Draft.regionScale -ne 150 -or $form.Draft.positionOffsetX -ne -50){throw 'layout draft lost'}
$form.Show();[Windows.Forms.Application]::DoEvents();$tabs=$form.Controls | Where-Object {$_ -is [Windows.Forms.TabControl]};$tabs.SelectedIndex=0
$shot=New-Object Drawing.Bitmap($form.Width,$form.Height);$form.DrawToBitmap($shot,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)));$shot.Save((Join-Path $dir 'packs-settings-check.png'));$shot.Dispose();$form.Close();$form.Dispose()
'PASS: regional scale and desktop offsets, unified pack selector, pack-specific drafts, cancel without disk mutation'

$c.regionWidth=360;$c.regionHeight=0;$c.regionScale=100;$c.showPortrait=$false;$c.showName=$false;$c.showLine=$true;$c.fontSize=16;$c.positionOffsetX=0;$c.positionOffsetY=0
$short=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('',"中文`n`n日本語",'',$c)
$measured=[ReportTimePlayer.AnnouncementPopup]::MeasureRegion('',"中文`n`n日本語",'',$c)
if($short.Height -ge 480 -or $short.Height -ne $measured.Height){throw 'automatic bilingual height still padded'}
$last=0;for($y=0;$y -lt $short.Height;$y++){for($x=0;$x -lt $short.Width;$x++){if($short.GetPixel($x,$y).A -gt 150){$last=$y;break}}}
if(($short.Height-$last) -gt 30){throw 'excess empty bottom region remains'};$short.Dispose()
$popup=New-Object ReportTimePlayer.AnnouncementPopup('',"中文`n`n日本語",'',$c,([TimeSpan]::FromSeconds(3)))
if(($popup.Top+$popup.Height) -ne ($area.Bottom-16)){throw 'automatic height bottom-right anchor drifted'};$popup.Dispose()
'PASS: bilingual automatic height fits visible content and preserves bottom-right anchor'
