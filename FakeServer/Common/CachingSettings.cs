namespace FakeServer.Common
{
    public class CachingSettings
    {
        public ETag ETag { get; set; } = new ETag();
    }

    public class ETag
    {
        public bool Enabled { get; set; } = false;
    }
}
