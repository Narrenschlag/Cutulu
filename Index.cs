using System.Collections.Generic;

namespace Cutulu
{
    public class Index<A, B>
    {
        public Dictionary<A, List<B>> dic { get; set; }
        public int Entries { get; set; }

        public Index()
        {
            dic = new Dictionary<A, List<B>>();
            Entries = 0;
        }

        public void Clear()
        {
            Entries = 0;
            dic.Clear();
        }

        public void Add(A key, B value)
        {
            if (!dic.TryGetValue(key, out List<B> list)) dic.Add(key, new List<B>() { value });
            else if (!list.Contains(value)) list.Add(value);

            else return;
            Entries++;
        }

        public bool Contains(A key) => dic.ContainsKey(key);

        public List<B> Get(A key) => Contains(key) ? dic[key] : null;
        public bool TryGet(A key, out List<B> list)
        {
            list = Get(key);
            return list.NotEmpty();
        }

        public B First(A key) => Contains(key) ? dic[key][0] : default;

        public void Remove(A key) => dic.TryRemove(key);
        public void Remove(A key, B value)
        {
            if (TryGet(key, out List<B> list) && list.Contains(value))
            {
                dic[key].Remove(value);

                if (dic[key].IsEmpty())
                    dic.Remove(key);

                Entries--;
            }
        }
    }
}