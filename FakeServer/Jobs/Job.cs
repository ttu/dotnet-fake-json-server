using System.Threading.Tasks;

namespace FakeServer.Jobs
{
    public class Job
    {
        public string ItemType { get; set; }

        public Task<dynamic> Action { get; set; }
    }

    public class JobsSettings
    {
        public int DelayMs { get; set; }
    }
}