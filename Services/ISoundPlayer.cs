using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Defines a service for playing a sound file.
    /// </summary>
    public interface ISoundPlayer
    {
        /// <summary>
        /// Starts playing a sound from a file.
        /// </summary>
        /// <param name="fileName">File to play sound from.</param>
        /// <param name="stateChanged">Callback to handle sound playback starting/stopping.</param>
        /// <returns>Unique identifier of sound being played.</returns>
        int PlaySound(string fileName, EventHandler<SoundEventArgs> stateChanged);

        /// <summary>
        /// Pauses the playing of a sound.
        /// </summary>
        /// <param name="soundId">Unique identifier of sound being played.</param>
        void PauseSound(int soundId);

        /// <summary>
        /// Resumes playing a paused sound.
        /// </summary>
        /// <param name="soundId"></param>
        void PlaySound(int soundId);

        /// <summary>
        /// Stops the playing of a sound and invalidates the unique identifier.
        /// </summary>
        /// <param name="soundId">Unique identifier of sound being played.</param>
        void StopSound(int soundId);

        /// <summary>
        /// Gets the length of a sound being played.
        /// </summary>
        /// <param name="soundId">Unique identifier of sound being played.</param>
        TimeSpan GetSoundLength(int soundId);

        /// <summary>
        /// Gets the current position of a sound being played.
        /// </summary>
        /// <param name="soundId">Unique identifier of sound being played.</param>
        TimeSpan GetSoundPosition(int soundId);

        /// <summary>
        /// Sets the current position of a sound being played.
        /// </summary>
        /// <param name="soundId">Unique identifier of sound being played.</param>
        /// <param name="position">New position to play from.</param>
        void SetSoundPosition(int soundId, TimeSpan position);
    }

    /// <summary>
    /// Information about a sound whose state has changed.
    /// </summary>
    public class SoundEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SoundEventArgs"/> class.
        /// </summary>
        /// <param name="soundId">The sound identifier.</param>
        /// <param name="state">The state.</param>
        public SoundEventArgs(int soundId, SoundState state)
        {
            SoundId = soundId;
            State = state;
        }

        /// <summary>
        /// Gets the unique identifier of the sound.
        /// </summary>
        public int SoundId { get; private set; }

        /// <summary>
        /// Gets the current state of the sound.
        /// </summary>
        public SoundState State { get; private set; }
    }

    /// <summary>
    /// The current state of a sound.
    /// </summary>
    public enum SoundState
    {
        /// <summary>
        /// Unspecified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Sound is being played.
        /// </summary>
        Playing,

        /// <summary>
        /// Sound is not being played.
        /// </summary>
        Stopped,

        /// <summary>
        /// Sound has finished playing.
        /// </summary>
        Complete,
    }
}
