using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace OrangeJuiceModMaker.Data
{
    public class MyMusicPlayer : IDisposable
    {
        public AudioFileReader? Reader { get; private set; }
        public WaveOutEvent Out { get; set; } = new();

        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler? EndOfSong;
        public bool IsLooped { get; set; } = false;
        public TimeSpan LoopPoint { get; set; } = TimeSpan.Zero;

        private bool isDisposed = false;
        public MyMusicPlayer()
        {
            _ = Task.Run(() =>
            {
                TimeSpan oldPosition = TimeSpan.Zero;
                while (!isDisposed)
                {
                    Thread.Sleep(1);


                    if (Reader is null)
                    {
                        oldPosition = TimeSpan.Zero;
                        continue;
                    }

                    TimeSpan cp;
                    lock (this)
                    {
                        if (Reader is null)
                        {
                            continue;
                        }

                        if (isDisposed)
                        {
                            continue;
                        }

                        try
                        {
                            cp = Reader.CurrentTime;
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    if (cp.Add(TimeSpan.FromMilliseconds(300)) > Duration)
                    {
                        if (IsLooped)
                        {
                            Position = LoopPoint;
                        }
                        else
                        {
                            EndOfSong?.Invoke(this, EventArgs.Empty);
                        }

                        continue;
                    }

                    if (cp == oldPosition)
                    {
                        continue;
                    }

                    oldPosition = cp;
                    PositionChanged?.Invoke(this, cp);
                }
            });
        }


        public TimeSpan Position
        {
            get
            {
                if (Reader is null)
                {
                    return TimeSpan.Zero;
                }

                return Reader.CurrentTime;
            }
            set
            {
                if (Reader is null)
                {
                    return;
                }
                Reader.CurrentTime = value;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (Reader is null)
                {
                    return TimeSpan.Zero;
                }

                return Reader.TotalTime;
            }
        }

        public void Open(string path)
        {
            lock (this)
            {
                Reader?.Dispose();
                Reader = new AudioFileReader(path);
                Out.Init(Reader);
            }
        }

        public void Dispose()
        {
            isDisposed = true;
            Reader?.Dispose();
            Out.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
