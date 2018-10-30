using System;

namespace LGCallRecorder
{
    public class CallRecord
    {
        public int StationNumber { get; set; }
        public string CO { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Start { get; set; }
        public CallType CallType { get; set; }
        public string Dialed { get; set; }
        public decimal Cost { get; set; }
        public string AccountCode { get; set; }
        public DisconnectCause DisconnectCause { get; set; }
    }
}