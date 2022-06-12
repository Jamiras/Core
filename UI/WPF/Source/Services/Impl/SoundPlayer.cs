using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.UI.WPF.Services.Impl
{
    [Export(typeof(ISoundPlayer))]
    internal class SoundPlayer : ISoundPlayer, IDisposable
    {
        [DebuggerDisplay("{_player.Source} {State}")]
        private class Sound
        {
            private static int _nextSoundId;
            private readonly SoundPlayer _owner;
            private readonly MediaPlayer _player;
            private readonly EventHandler<SoundEventArgs> _handler;
            private SoundState _state;

            public Sound(SoundPlayer owner, EventHandler<SoundEventArgs> handler)
            {
                _owner = owner;
                _handler = handler;

                SoundId = ++_nextSoundId;

                _player = new MediaPlayer();
                _player.MediaOpened += PlayerMediaOpened;
                _player.MediaEnded += PlayerMediaEnded;
            }

            private void PlayerMediaOpened(object sender, EventArgs e)
            {
                if (State == SoundState.None)
                    Play();
            }

            private void PlayerMediaEnded(object sender, EventArgs e)
            {
                State = SoundState.Complete;

                // if the event handler didn't restart the sound, it's no longer active.
                if (State == SoundState.Complete)
                    _owner._activeSounds.Remove(this);
            }

            internal Sound(int id)
            {
                SoundId = id;
            }

            public int SoundId { get; private set; }

            private SoundState State
            {
                get { return _state; }
                set
                {
                    if (_state != value)
                    {
                        _state = value;

                        if (_handler != null)
                            _handler(_owner, new SoundEventArgs(SoundId, _state));
                    }
                }
            }

            private void Invoke(Action action)
            {
                if (_owner._isDisposed)
                    State = SoundState.Stopped;
                else if (_player.Dispatcher.CheckAccess())
                    action();
                else
                    _player.Dispatcher.Invoke(action, null);
            }

            private TimeSpan InvokeGetTimeSpan(Func<TimeSpan> action)
            {
                if (_owner._isDisposed)
                {
                    State = SoundState.Stopped;
                    return TimeSpan.Zero;
                }

                if (_player.Dispatcher.CheckAccess())
                    return action();

                try
                {
                    return (TimeSpan)_player.Dispatcher.Invoke(action, null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.GetType() + "\n\n" + ex.Message);
                    return TimeSpan.Zero;
                }
            }

            public void Load(string fileName)
            {
                Invoke(() => _player.Open(new Uri(fileName)));
            }

            public void Play()
            {
                if (State != SoundState.Playing)
                {
                    Invoke(_player.Play);
                    State = SoundState.Playing;
                }
            }

            public void Pause()
            {
                if (State == SoundState.Playing)
                {
                    Invoke(_player.Pause);
                    State = SoundState.Stopped;
                }
            }

            public void Stop()
            {
                if (State == SoundState.Playing)
                {
                    Invoke(_player.Stop);
                    State = SoundState.Stopped;
                }
            }

            public TimeSpan Length
            {
                get { return InvokeGetTimeSpan(() => _player.NaturalDuration.TimeSpan); }
            }

            public TimeSpan Position
            {
                get { return InvokeGetTimeSpan(() => _player.Position); }
                set
                {
                    Invoke(() =>
                    {
                        if (State == SoundState.Playing)
                            _player.Stop();

                        _player.Position = value;
                    });
                    State = SoundState.Stopped;
                }
            }
        }

        private readonly List<Sound> _activeSounds = new List<Sound>();
        private bool _isDisposed;

        public void Dispose()
        {
            _isDisposed = true;

            foreach (var sound in _activeSounds)
                sound.Stop();

            _activeSounds.Clear();
        }

        private class SoundIdComparer : IComparer<Sound>
        {
            public int Compare(Sound x, Sound y)
            {
                return x.SoundId - y.SoundId;
            }
        }

        private Sound GetSound(int soundId)
        {
            int index = _activeSounds.BinarySearch(new Sound(soundId), new SoundIdComparer());
            if (index < 0)
                throw new ArgumentException("sound " + soundId + " not found");

            return _activeSounds[index];
        }

        public int PlaySound(string fileName, EventHandler<SoundEventArgs> stateChanged)
        {
            var sound = new Sound(this, stateChanged);
            sound.Load(fileName);
            _activeSounds.Add(sound);
            return sound.SoundId;
        }

        public void PauseSound(int soundId)
        {
            var sound = GetSound(soundId);
            sound.Pause();
        }

        public void StopSound(int soundId)
        {
            var sound = GetSound(soundId);
            _activeSounds.Remove(sound);

            sound.Pause();
        }

        public void PlaySound(int soundId)
        {
            var sound = GetSound(soundId);
            sound.Play();
        }

        public TimeSpan GetSoundLength(int soundId)
        {
            var sound = GetSound(soundId);
            return sound.Length;
        }

        public TimeSpan GetSoundPosition(int soundId)
        {
            var sound = GetSound(soundId);
            return sound.Position;
        }

        public void SetSoundPosition(int soundId, TimeSpan position)
        {
            var sound = GetSound(soundId);
            sound.Position = position;
        }
    }
}
