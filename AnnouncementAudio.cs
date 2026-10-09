using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace ReportTimePlayer
{
    // Keep the same Windows audio session alive between short announcements.
    // Idle samples are exactly zero; no audible keep-alive tone is generated.
    public sealed class AnnouncementAudio : ISampleProvider, IDisposable
    {
        private readonly object gate = new object();
        private readonly List<Clip> clips = new List<Clip>();
        private WaveOutEvent output;
        private float[] scratch = new float[0];
        private bool disposed;
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        public void Start()
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(AnnouncementAudio));
                if (output != null && output.PlaybackState == PlaybackState.Playing) return;
            }
            // Dispose outside the read lock: the output thread may be waiting for Read.
            var old = output;
            output = null;
            old?.Dispose();
            var next = new WaveOutEvent { DesiredLatency = 100, NumberOfBuffers = 3 };
            try
            {
                next.Init(new SampleToWaveProvider16(this));
                next.Play();
                output = next;
            }
            catch { next.Dispose(); throw; }
        }

        public TimeSpan Play(string path, int volume, bool preview)
        {
            Start();
            var reader = new AudioFileReader(path) { Volume = Math.Max(0, Math.Min(100, volume)) / 100f };
            try
            {
                ISampleProvider source = reader;
                if (source.WaveFormat.Channels == 1) source = new MonoToStereoSampleProvider(source);
                if (source.WaveFormat.Channels != 2) throw new NotSupportedException("语音文件必须是单声道或立体声。");
                if (source.WaveFormat.SampleRate != 48000) source = new WdlResamplingSampleProvider(source, 48000);
                var duration = reader.TotalTime;
                lock (gate)
                {
                    if (disposed) throw new ObjectDisposedException(nameof(AnnouncementAudio));
                    clips.Add(new Clip { Reader = reader, Source = source, Preview = preview });
                }
                return duration;
            }
            catch { reader.Dispose(); throw; }
        }

        public void StopPreview()
        {
            lock (gate)
                for (int i = clips.Count - 1; i >= 0; i--)
                    if (clips[i].Preview) { clips[i].Reader.Dispose(); clips.RemoveAt(i); }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (gate)
            {
                Array.Clear(buffer, offset, count);
                if (disposed) return 0;
                if (scratch.Length < count) scratch = new float[count];
                for (int i = clips.Count - 1; i >= 0; i--)
                {
                    var clip = clips[i];
                    int read = clip.Source.Read(scratch, 0, count);
                    for (int n = 0; n < read; n++) buffer[offset + n] += scratch[n];
                    if (read == 0) { clip.Reader.Dispose(); clips.RemoveAt(i); }
                }
                for (int n = offset; n < offset + count; n++) buffer[n] = Math.Max(-1f, Math.Min(1f, buffer[n]));
                return count;
            }
        }

        public void Dispose()
        {
            WaveOutEvent previous;
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                previous = output; output = null;
            }
            previous?.Dispose();
            lock (gate) { foreach (var clip in clips) clip.Reader.Dispose(); clips.Clear(); }
        }
        private sealed class Clip { public AudioFileReader Reader; public ISampleProvider Source; public bool Preview; }
    }
}
