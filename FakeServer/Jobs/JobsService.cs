using FakeServer.WebSockets;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FakeServer.Jobs
{
    public class JobsService
    {
        private readonly Dictionary<string, Job> _queue = new Dictionary<string, Job>();
        private readonly IMessageBus _bus;
        private readonly Action _delay;

        public JobsService(IMessageBus bus, IOptions<JobsSettings> jobsSettings)
        {
            _bus = bus;

            _delay = new Action(() =>
            {
                if (jobsSettings.Value.DelayMs > 0)
                    Thread.Sleep(jobsSettings.Value.DelayMs);
            });
        }

        public string StartNewJob(string collection, string method, Func<dynamic> func)
        {
            var queueId = Guid.NewGuid().ToString().Substring(0, 5);
            var queueUrl = $"async/queue/{queueId}";

            var task = Task.Run(() =>
            {
                _delay();

                var itemId = func();

                var data = new { Method = method, Path = $"{collection}/{itemId}", Collection = collection, ItemId = itemId };
                _bus.Publish("updated", data);

                return itemId;
            });

            _queue.Add(queueId, new Job { Collection = collection, Action = task });

            return queueUrl;
        }

        public Job GetJob(string queueId)
        {
            _queue.TryGetValue(queueId, out Job process);
            return process;
        }

        public bool DeleteJob(string queueId)
        {
            return _queue.Remove(queueId);
        }
    }
}