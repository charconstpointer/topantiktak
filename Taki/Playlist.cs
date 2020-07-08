using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Taki
{
    public sealed class Playlist <T> where T : ITrack
    {
        private readonly IDictionary<int, Stack<T>> _channels;

        private readonly Thread _ticker;

        public Playlist()
        {
            _channels = new ConcurrentDictionary<int, Stack<T>>();
            _ticker = new Thread(OnTick) {IsBackground = true};
        }

        public void Start()
        {
            _ticker.Start();
        }

        private void OnTick()
        {
            while (true)
            {
                try
                {
                    foreach (var (key, value) in _channels)
                    {
                        if (!value.TryPeek(out var current)) continue;
                        if (current.Stop > DateTime.UtcNow.AddHours(2))
                        {
                            continue;
                        }

                        value.TryPop(out _);
                        OnTrackChanged(key, value.Peek());
                        Console.WriteLine("Track has expired, popping");
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            // ReSharper disable once FunctionNeverReturns
        }

        public event EventHandler<TrackChangedEvent> TrackChanged;


        public int GetProgress(int channelId)
        {
            var currentTrack = GetCurrentlyPlayed(channelId);
            var duration = currentTrack.Stop.Subtract(currentTrack.Start);
            var progress = currentTrack.Stop.Subtract(DateTime.UtcNow.AddHours(2));
            var percentage = Math.Abs(progress.TotalSeconds / duration.TotalSeconds * 100 - 100);
            return (int) percentage;
        }

        public void AddChannel(int channelId, IEnumerable<T> tracks)
        {
            var t = tracks.ToList();
            if (_channels.ContainsKey(channelId)) throw new ApplicationException("Channel already exists");
            _channels.Add(channelId, new Stack<T>());
            var channel = _channels[channelId];
            var channelSchedule = t.ToList();
            var tracksToAdd = channelSchedule.Where(track => track.Stop >= DateTime.UtcNow.AddHours(2));
            foreach (var track in tracksToAdd.Reverse()) channel.Push(track);
        }

        public T GetCurrentlyPlayed(int channelId)
        {
            if (!_channels.ContainsKey(channelId)) throw new ApplicationException("No such channel");

            var hasTrack = _channels[channelId].TryPeek(out var track);
            if (hasTrack) return track;

            throw new ApplicationException("Channel is empty");
        }

        private void OnTrackChanged(int channelId, T track)
        {
            TrackChanged?.Invoke(this, new TrackChangedEvent
            {
                Track = track,
                ChannelId = channelId
            });
        }
    }
}