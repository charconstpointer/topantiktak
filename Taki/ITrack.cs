using System;

namespace Taki
{
    public interface ITrack
    {
        public DateTime Start { get; }
        public DateTime Stop { get; }
    }
}