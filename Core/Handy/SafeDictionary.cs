#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using Godot;

    public partial class SafeDictionary<K, V> : Resource
    {
        private System.Collections.Generic.Dictionary<K, V> loaded;
        public System.Collections.Generic.Dictionary<K, V> Loaded
        {
            get
            {
                if (loaded == null) Load(loaded = []);

                return loaded;
            }
        }

        public virtual void Load(System.Collections.Generic.Dictionary<K, V> dictionary) { }
        public void Reset() => loaded = null;

        public bool TryGetValue(K key, out V value) => Loaded.TryGetValue(key, out value);
    }
}
#endif