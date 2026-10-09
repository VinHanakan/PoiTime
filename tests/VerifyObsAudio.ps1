param([string]$BuildDirectory = "bin\Release")
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$dir=(Resolve-Path $BuildDirectory).Path
$resolver=[ResolveEventHandler]{param($sender,$args) $path=Join-Path $dir ((New-Object Reflection.AssemblyName($args.Name)).Name+'.dll'); if([IO.File]::Exists($path)){return [Reflection.Assembly]::LoadFrom($path)}; return $null}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$a=[Reflection.Assembly]::LoadFrom((Join-Path $dir 'PoiTime.exe'))
$c=New-Object ReportTimePlayer.Config
$c.backgroundColor='#224466'; $c.backgroundOpacity=0
$b=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('test','line','',$c)
if($b.GetPixel(0,0).A -ne 0){throw 'transparent background failed'}
$found=$false
for($x=18;$x -lt 150;$x++){for($y=35;$y -lt 70;$y++){if($b.GetPixel($x,$y).A -gt 240){$found=$true}}}
if(!$found){throw 'text faded with background'}
$b.Dispose(); $c.backgroundOpacity=50
$b=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('test','line','',$c)
if([Math]::Abs($b.GetPixel(0,0).A-127) -gt 1){throw '50 percent alpha failed'}
$b.Save((Join-Path $dir 'background-check.png')); $b.Dispose()
$img=New-Object Drawing.Bitmap(40,40); $g=[Drawing.Graphics]::FromImage($img); $g.Clear([Drawing.Color]::Red); $g.Dispose(); $img.Save((Join-Path $dir 'test-background.png')); $img.Dispose()
$c.backgroundMode='Image'; $c.backgroundImage=Join-Path $dir 'test-background.png'; $c.backgroundOpacity=25
$b=[ReportTimePlayer.AnnouncementPopup]::RenderSurface('test','line','',$c)
$p=$b.GetPixel(0,0); if($p.R -lt 250 -or [Math]::Abs($p.A-63) -gt 1){throw 'image alpha failed'}; $b.Dispose()
$c.showAnnouncement=$true
$popup=New-Object ReportTimePlayer.AnnouncementPopup('test','line','',$c,([TimeSpan]::FromSeconds(3)))
$popup.Show(); [Windows.Forms.Application]::DoEvents(); if(!$popup.Visible){throw 'popup show failed'}; $popup.Close(); $popup.Dispose()
$preview=[Action[int,ReportTimePlayer.Config]]{param($hour,$draft)}
$stop=[Action]{}
$form=New-Object ReportTimePlayer.AnnouncementSettingsForm($c,$preview,$stop)
$form.Show(); [Windows.Forms.Application]::DoEvents(); if($form.Draft.backgroundOpacity -ne 25){throw 'draft settings failed'}; $form.Close(); $form.Dispose()
# Silent WAV exercises the real playback/stop path without registering startup.
$wav=Join-Path $dir 'preview-test.wav'
$w=New-Object IO.BinaryWriter([IO.File]::Create($wav)); $n=44100*2*3
$w.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $w.Write([int](36+$n)); $w.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt ')); $w.Write([int]16); $w.Write([int16]1); $w.Write([int16]1); $w.Write([int]44100); $w.Write([int]88200); $w.Write([int16]2); $w.Write([int16]16); $w.Write([Text.Encoding]::ASCII.GetBytes('data')); $w.Write([int]$n); $w.Write((New-Object byte[] $n)); $w.Dispose()
$t=$a.GetType('ReportTimePlayer.ReportTimeContext'); $ctx=[Runtime.Serialization.FormatterServices]::GetUninitializedObject($t); $flags=[Reflection.BindingFlags]'Instance,NonPublic'
$voice=New-Object ReportTimePlayer.VoiceAnnouncement; $voice.hour=12; $voice.fileName='preview-test.wav'
$pack=New-Object ReportTimePlayer.VoiceFolderConfig; $pack.voices=New-Object 'Collections.Generic.List[ReportTimePlayer.VoiceAnnouncement]'; $pack.voices.Add($voice)
$t.GetField('voiceFolder',$flags).SetValue($ctx,$dir); $t.GetField('voiceFolderConfig',$flags).SetValue($ctx,$pack)
$icon=New-Object Windows.Forms.NotifyIcon; $t.GetField('notifyIcon',$flags).SetValue($ctx,$icon)
$c.showAnnouncement=$false

$t.GetMethod('PreviewAnnouncement',$flags).Invoke($ctx,@([int]12,$c.PSObject.BaseObject)) | Out-Null
$audio=$t.GetField('audio',$flags).GetValue($ctx)
if(!$audio){throw 'preview audio failed'}
$outputField=$audio.GetType().GetField('output',$flags)
$session=$outputField.GetValue($audio)
if($session.PlaybackState -ne 'Playing'){throw 'audio session not playing'}
for($i=0;$i -lt 15;$i++) {
    $t.GetMethod('StopPreview',$flags).Invoke($ctx,@()) | Out-Null
    $t.GetMethod('PreviewAnnouncement',$flags).Invoke($ctx,@([int]12,$c.PSObject.BaseObject)) | Out-Null
    if(![object]::ReferenceEquals($session,$outputField.GetValue($audio))){throw 'output session changed during preview'}
}
$t.GetMethod('StopPreview',$flags).Invoke($ctx,@()) | Out-Null
if($session.PlaybackState -ne 'Playing'){throw 'stop preview stopped persistent session'}
$t.GetMethod('PreviewAnnouncement',$flags).Invoke($ctx,@([int]12,$c.PSObject.BaseObject)) | Out-Null
$deadline=[DateTime]::UtcNow.AddSeconds(4)
while([DateTime]::UtcNow -lt $deadline){[Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 40}
if($session.PlaybackState -ne 'Playing'){throw 'EOF stopped persistent session'}
$samples=New-Object float[] 1000
if($audio.Read($samples,0,1000) -ne 1000){throw 'idle buffer not filled'}
if(($samples | Where-Object {$_ -ne 0}).Count -ne 0){throw 'idle audio not silent'}
$audio.Dispose(); $audio.Dispose()
$icon.Dispose()
$anchor=New-Object ReportTimePlayer.ObsCaptureForm(([Action]{}),([Action]{}),([Action]{}))
$anchor.Show(); [Windows.Forms.Application]::DoEvents(); $handle=$anchor.Handle
if(!$anchor.Visible -or $anchor.WindowState -ne 'Minimized'){throw 'capture window missing while minimized'}
$anchor.Restore(); $anchor.Close(); [Windows.Forms.Application]::DoEvents()
if($anchor.IsDisposed -or $anchor.Handle -ne $handle -or $anchor.WindowState -ne 'Minimized'){throw 'capture window closed instead of minimizing'}
$anchor.Dispose()
'PASS: alpha rendering, settings draft, 15 repeated previews, same audio session after stop/EOF, exact silent idle, persistent minimized capture window'
