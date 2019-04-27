using System.Collections.Generic;

namespace FakeServer.CustomResponse
{
    public class CustomResponseSettings
    {
        public bool Enabled { get; set; }

        public List<ScriptSettings> Scripts { get; set; }
    }

    public class ScriptSettings
    {
        public string Script { get; set; }
        public List<string> Methods { get; set; }
        public List<string> Paths { get; set; }
        public List<string> Usings { get; set; }
        public List<string> References { get; set; }
    }
}