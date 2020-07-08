using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Taki
{
    public class Track
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public DateTime ScheduleTime { get; set; }
        public DateTime Stop { get; set; }
    }

    public class MojeTrack : ITrack
    {
        public int Id { get; }
        public int ChannelId { get; }
        public string Title { get; }
        public string Artist { get; }
        public DateTime Start { get; }
        public DateTime Stop { get; }

        public MojeTrack(int id, int channelId, string title, string artist, DateTime start, DateTime stop)
        {
            Id = id;
            Title = title;
            Artist = artist;
            Start = start;
            Stop = stop;
            ChannelId = channelId;
        }
    }

    public class Moje
    {
        public int Id { get; set; }
        public IEnumerable<Track> Songs { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var client = new HttpClient();
                var schedule = await GetMoje(client);
                var mojeSchedule = schedule.Select(x => x.AsEntity()).ToList();
                var playlist = new Playlist<MojeTrack>();
                foreach (var mojeTracks in mojeSchedule.Where(mojeTracks => mojeTracks.Any()))
                {
                    playlist.AddChannel(mojeTracks.FirstOrDefault().ChannelId, mojeTracks);
                }

                playlist.TrackChanged += PlaylistOnTrackChanged;
                playlist.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Console.ReadKey();
        }

        private static void PlaylistOnTrackChanged(object? sender, TrackChangedEvent e)
        {
            Console.WriteLine($"Zmianka {e.ChannelId}");
        }

        private static async Task<IEnumerable<Moje>> GetMoje(HttpClient client)
        {
            // var ids = new List<int> {79};
            var ids = Enumerable.Range(1, 111).Select(x => x);
            var schedules = new List<Moje>();
            foreach (var id in ids)
            {
                try
                {
                    Console.WriteLine($"Fetching #{id}");
                    var schedule = await client.GetFromJsonAsync<Moje>(
                        $"http://moje.polskieradio.pl/api/?mobilestationid={id}&key=d590cafd-31c0-4eef-b102-d88ee2341b1a");
                    for (var i = 0; i < schedule.Songs.Count() - 1; i++)
                    {
                        var current = schedule.Songs.ElementAt(i);
                        var next = schedule.Songs.ElementAt(i + 1);
                        current.Stop = next.ScheduleTime;
                        current.ScheduleTime = current.ScheduleTime.Trim(TimeSpan.TicksPerMinute);
                        current.Stop = current.Stop.Trim(TimeSpan.TicksPerMinute);
                    }

                    if (schedule.Songs.Any()) schedules.Add(schedule);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return schedules;
        }
    }

    public static class Mapper
    {
        public static IEnumerable<MojeTrack> AsEntity(this Moje moje)
            => moje.Songs?.Select(t => new MojeTrack(t.Id, moje.Id, t.Title, t.Artist, t.ScheduleTime, t.Stop));
    }

    public static class Foo
    {
        public static DateTime Trim(this DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks, date.Kind);
        }
    }
}