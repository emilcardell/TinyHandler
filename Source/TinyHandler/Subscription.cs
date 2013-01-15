using System;

namespace TinyHandler
{
    public abstract class Subscription<T> : ISubscription
    {
        public Action<T> OnProcessed { get; set; }
    }
}
