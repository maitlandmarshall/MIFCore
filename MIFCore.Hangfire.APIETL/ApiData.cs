using System;

namespace MIFCore.Hangfire.APIETL
{
    public class ApiData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }

        public string Endpoint { get; set; }
        public string Uri { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public string Data { get; set; }
    }
}
