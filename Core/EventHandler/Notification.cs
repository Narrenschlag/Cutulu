namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System;

    public abstract class ANotification<T>
    {
        private readonly Dictionary<object, Action<T>> Handlers = [];

        protected void Add(object host, Action<T> action)
        {
            if (host.IsNull() || Handlers.ContainsKey(host)) return;
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
        public void Add(object host, Action action) => base.Add(host, value => action());
        public void Bind(object host, Action action) => Add(host, action);

        public void Invoke()
        {
            base.Invoke(null);
        }

        public static Notification operator +(Notification notifier, (object host, Action action) pair)
        {
            notifier.Add(pair.host, pair.action);
            return notifier;
        }

        public static Notification operator -(Notification notifier, object host)
        {
            notifier.Remove(host);
            return notifier;
        }
    }

    public partial class Notification<T1> : ANotification<T1>
    {
        public new void Add(object host, Action<T1> action) => base.Add(host, action);
        public void Bind(object host, Action<T1> action) => Add(host, action);

        public new void Invoke(T1 value) => base.Invoke(value);

        public static Notification<T1> operator +(Notification<T1> notifier, (object host, Action<T1> action) pair)
        {
            notifier.Add(pair.host, pair.action);
            return notifier;
        }

        public static Notification<T1> operator -(Notification<T1> notifier, object host)
        {
            notifier.Remove(host);
            return notifier;
        }
    }

    public partial class Notification<T1, T2> : ANotification<(T1, T2)>
    {
        public void Add(object host, Action<T1, T2> action) => base.Add(host, value => action(value.Item1, value.Item2));
        public void Bind(object host, Action<T1, T2> action) => Add(host, action);

        public void Invoke(T1 value1, T2 value2) => base.Invoke((value1, value2));

        public static Notification<T1, T2> operator +(Notification<T1, T2> notifier, (object host, Action<T1, T2> action) pair)
        {
            notifier.Add(pair.host, pair.action);
            return notifier;
        }

        public static Notification<T1, T2> operator -(Notification<T1, T2> notifier, object host)
        {
            notifier.Remove(host);
            return notifier;
        }
    }
}