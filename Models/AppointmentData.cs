using System;

namespace DotNetCoreRazor_MSGraph.Models {
    public class AppointmentData {
        public string Id { get; set; }
        public string Subject { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAllDay { get; set; }
    }
}
