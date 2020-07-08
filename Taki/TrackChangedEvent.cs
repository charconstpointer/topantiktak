namespace Taki
{
    public class TrackChangedEvent
    {
        public int ChannelId { get; set; }
        public ITrack Track { get; set; }
    }
}