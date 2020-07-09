namespace Taki
{
    public class TrackChangedEvent<T>
    {
        public int ChannelId { get; set; }
        public T Track { get; set; }
    }
}