using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LGCallRecorder
{
    public class CallRecord
    {
        public int StationNumber { get; set; }
        public string CO { get; set; }
        public double DurationMilliseconds { get; set; }

        [NotMapped]
        public TimeSpan Duration
        {
            get => TimeSpan.FromMilliseconds(DurationMilliseconds);
            set => DurationMilliseconds = value.TotalMilliseconds;
        }

        public DateTime Start { get; set; }
        public CallType CallType { get; set; }
        public string Dialed { get; set; }
        public decimal Cost { get; set; }
        public string AccountCode { get; set; }
        public DisconnectCause DisconnectCause { get; set; }
    }
}