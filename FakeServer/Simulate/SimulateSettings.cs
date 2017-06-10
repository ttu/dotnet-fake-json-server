using System.Collections.Generic;

namespace FakeServer.Simulate
{
    public class SimulateSettings
    {
        public DelaySettings Delay { get; set; }
    }

    public class DelaySettings
    {
        public bool Enabled { get; set; }

        public List<string> Methods { get; set; }

        public int MinMs { get; set; }

        public int MaxMs { get; set; }
    }
}