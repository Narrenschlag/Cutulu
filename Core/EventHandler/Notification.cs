namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System;

    public abstract class ANotification<T>
    {
        private readonly Dictionary<object, Action<T>> Handlers = [];

        protected void Add(object host, Action<T> action)
        {
            if (host.IsNull()) return;
            Handlers[host] = action;
        }

        public void Remove(object host)
        {
            if (host.IsNull()) return;
            Handlers.Remove(host);
        }

        protected void Invoke(T value)
        {
            var nullHosts = new List<object>();

            foreach (var kvp in Handlers)
            {
                if (kvp.Key == null)
                {
                    nullHosts.Add(kvp.Key);
                }
                else
                {
                    kvp.Value?.Invoke(value);
                }
            }

            // Remove null hosts after iteration
            foreach (var nullHost in nullHosts)
            {
                Handlers.Remove(nullHost);
            }
        }

        public void Clear()
        {
            Handlers.Clear();
        }
    }

    public partial class Notification : ANotification<object>
    {
        public void Add(object host, Action action)
        {
            base.Add(host, value => action());
        }

        public void Invoke()
        {
            base.Invoke(null);
        }
    }

    public partial class Notification<T> : ANotification<T>
    {
        public new void Add(object host, Action<T> action) => base.Add(host, action);

        public new void Invoke(T value) => base.Invoke(value);
    }
}