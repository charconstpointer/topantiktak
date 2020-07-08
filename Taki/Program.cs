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
        public string Title { get; }
        public string Artist { get; }
        public DateTime Start { get; }
        public DateTime Stop { get; }

        public MojeTrack(int id, string title, string artist, DateTime start, DateTime stop)
        {
            Id = id;
            Title = title;
            Artist = artist;
            Start = start;
            Stop = stop;
        }
    }

    public class Moje
    {
        public IEnumerable<Track> Songs { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient();
            var schedule = await GetMoje(client);
            var mojeSchedule = schedule.Select(x => x.AsEntity()).ToList();
            var playlist = new Playlist<MojeTrack>();
            Parallel.ForEach(mojeSchedule, (mojeTracks) =>
            {

                playlist.AddChannel(mojeSchedule.IndexOf(mojeTracks), mojeTracks);
            });
            // foreach (var mojeTracks in mojeSchedule)
            // {
            //   
            //     
            // }
            playlist.TrackChanged += PlaylistOnTrackChanged;
            playlist.Start();
            Console.ReadKey();
        }

        private static void PlaylistOnTrackChanged(object? sender, TrackChangedEvent e)
        {
            Console.WriteLine($"Zmianka {e.ChannelId}");
        }

        private static async Task<IEnumerable<Moje>> GetMoje(HttpClient client)
        {
            var ids = Enumerable.Range(1, 100).Select(x => x);
            var schedules = new List<Moje>();
            foreach (var id in ids)
            {
                var schedule = await client.GetFromJsonAsync<Moje>(
                    "http://moje.polskieradio.pl/api/?mobilestationid=103&key=d590cafd-31c0-4eef-b102-d88ee2341b1a");
                Console.WriteLine($"Fetching #{id}");
                for (var i = 0; i < schedule.Songs.Count() - 1; i++)
                {
                    var current = schedule.Songs.ElementAt(i);
                    var next = schedule.Songs.ElementAt(i + 1);
                    current.Stop = next.ScheduleTime;
                    current.ScheduleTime = current.ScheduleTime.Trim(TimeSpan.TicksPerMinute);
                    current.Stop = current.Stop.Trim(TimeSpan.TicksPerMinute);
                }

                schedules.Add(schedule);
            }

            return schedules;
        }
    }

    public static class Mapper
    {
        public static IEnumerable<MojeTrack> AsEntity(this Moje moje)
            => moje.Songs.Select(t => new MojeTrack(t.Id, t.Title, t.Artist, t.ScheduleTime, t.Stop));
    }

    public static class Foo
    {
        public static DateTime Trim(this DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks, date.Kind);
        }
    }
}