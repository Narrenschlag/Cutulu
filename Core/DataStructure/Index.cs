using System.Collections.Generic;

namespace Cutulu.Core
{
    public class Index<A, B>
    {
        public readonly Dictionary<A, List<B>> Dic = [];
        public int Entries { get; set; }

        public Index()
        {
            Entries = 0;
        }

        public void Clear()
        {
            Entries = 0;
            Dic.Clear();
        }

        public void Add(A key, B value)
        {
            if (!Dic.TryGetValue(key, out List<B> list)) Dic.Add(key, [value]);
            else if (!list.Contains(value)) list.Add(value);

            else return;
            Entries++;
        }

        public bool Contains(A key) => Dic.ContainsKey(key);

        public List<B> Get(A key) => Contains(key) ? Dic[key] : null;
        public bool TryGet(A key, out List<B> list)
        {
            list = Get(key);
            return list.NotEmpty();
        }

        public B First(A key) => Contains(key) ? Dic[key][0] : default;

        public void Remove(A key) => Dic.TryRemove(key);
        public void Remove(A key, B value)
        {
            if (TryGet(key, out List<B> list) && list.Contains(value))
            {
                Dic[key].Remove(value);

                if (Dic[key].IsEmpty())
                    Dic.Remove(key);

                Entries--;
            }
        }
    }
}