namespace Cutulu.Core
{
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using System.IO;
    using System;

    /// <summary>
    /// This class was mainly made for the authoritative card dealer of my Merchants & Mercenaries game. It does what it's name suggests.
    /// </summary>
    public class CopyOnWrite<ENTRY> where ENTRY : CopyOnWrite<ENTRY>.IEntry
    {
        // General Caches and Notifiers
        private Dictionary<uint, ENTRY> Entries { get; set; }

        public readonly Notification<ENTRY> EntryAdded = new();
        public readonly Notification<ENTRY> EntryModified = new();
        public readonly Notification<ENTRY> EntryRemoved = new();

        // Authoritative Caches and Values
        private Dictionary<uint, (Guid A, Guid B)> Hashes { get; set; }
        private Dictionary<Guid, Guid, uint> Hashed { get; set; }

        private Dictionary<uint, int> UsageCount { get; set; }
        private uint EntryUID { get; set; }

        public CopyOnWrite()
        {
            Entries = [];

            UsageCount = [];
            Hashed = [];
            Hashes = [];

            EntryUID = 0;
        }

        public void Clear(bool clearNotifications = false)
        {
            UsageCount.Clear();
            Entries.Clear();
            Hashed.Clear();
            Hashes.Clear();
            EntryUID = 0;

            if (clearNotifications)
            {
                EntryAdded.Clear();
                EntryModified.Clear();
                EntryRemoved.Clear();
            }
        }

        public int this[uint uid, bool removeBelowZero = true]
        {
            get => UsageCount.TryGetValue(uid, out var value) ? value : 0;
            set
            {
                if (ContainsEntry(uid) == false) return;

                if (value > 0)
                {
                    UsageCount[uid] = value;
                }

                else
                {
                    if (removeBelowZero && UsageCount.ContainsKey(uid)) Remove(uid);
                    else UsageCount.Remove(uid);
                }
            }
        }

        public int IncreaseUsage(uint uid, int count = 1) => this[uid] += count;
        public int DecreaseUsage(uint uid, int count = 1) => this[uid] -= count;
        public int SetUsage(uint uid, int count = 1) => this[uid] = count;
        public int GetUsage(uint uid) => this[uid];

        public int Count => Entries.Count;

        public IReadOnlyCollection<ENTRY> GetEntries() => Entries.Values;

        public ENTRY GetEntryOrDefault(uint uid, ENTRY @default = default) => TryGetEntry(uid, out var entry) ? entry : @default;

        public bool ContainsEntry(uint uid) => Entries.ContainsKey(uid);

        public bool TryGetEntry(uint uid, out ENTRY entry) => Entries.TryGetValue(uid, out entry);

        /// <summary>
        /// This is the main function of the class. Use this if you want to get the entry UID for the base entry object. For example the file it is based on. 
        /// </summary>
        public uint GetOrCreateUID(ENTRY entryBase)
        {
            (var A, var B) = GetHash(entryBase);

            if (Hashed.TryGetValue(A, B, out var uid)) return uid;

            return Add(entryBase.GetSafeEntry<ENTRY>());
        }

        /// <summary>
        /// Prepares entry of uid for modification. Use returned entry for modifications. Call FinishModification(entry) after modifications. If count is above 0 the usage count will be considered.
        /// </summary>
        public ENTRY StartModification(uint uid, int count = 0)
        {
            if (uid < 1 || Entries.TryGetValue(uid, out var entry) == false) return default;

            // Allow modifying the given instance directly
            if (count > 0 && this[uid] <= count) return entry;

            var duplicate = entry.GetSafeEntry<ENTRY>();
            duplicate.UID = 0;

            // Reduce usage count
            if (count > 0) this[uid] -= count;

            return duplicate;
        }

        /// <summary>
        /// Registers given entry after modification. If count is above 0 the usage count will be modified by that value. EntryModified.Invoke() is called after addition.
        /// </summary>
        public void FinishModification(ENTRY entry, int count = 0)
        {
            // Add but do not invoke add notification
            Add(entry, count, false);

            // Invoke notification
            EntryModified.Invoke(entry);
        }

        /// <summary>
        /// Registers entry and assigns UID. If count is above 0 the usage count will be modified by that value. EntryAdded.Invoke() is called after addition.
        /// </summary>
        public uint Add(ENTRY entry, int count = 0)
        {
            // Add and invoke add notification
            return Add(entry, count, true);
        }

        private uint Add(ENTRY entry, int count, bool invokeAdded)
        {
            // Non valid entry
            if (entry.IsNull())
            {
                Debug.LogError($"CopyOnWrite: Tried to add null entry of type {typeof(ENTRY)} [ADDITION: {invokeAdded}]");
                return default;
            }

            // Get hashed GUIDs of the entry
            var hashed = GetHash(entry);

            // Check if there is already a duplicate
            if (Hashed.TryGetValue(hashed.A, hashed.B, out var uid))
            {
                if (count > 0) this[uid] += count;
                return uid;
            }

            // Create and assign UID
            entry = entry.GetSafeEntry<ENTRY>();
            uid = ++EntryUID;
            entry.UID = uid;

            // Register entry under uid
            Hashed[hashed.A, hashed.B] = uid;
            Entries[uid] = entry;
            Hashes[uid] = hashed;

            if (count > 0) this[uid] = count;

            // Invoke notification
            if (invokeAdded) EntryAdded.Invoke(entry);

            // Answer with uid after creation
            return uid;
        }

        /// <summary>
        /// Removes any trace of the given entry. EntryRemoved.Invoke() is called after removal.
        /// </summary>
        public bool Remove(uint uid, bool destroy = true)
        {
            UsageCount.Remove(uid);

            if (Hashes.TryGetValue(uid, out var hashes))
            {
                Hashed.Remove(hashes.A, hashes.B);
                Hashes.Remove(uid);
            }

            if (Entries.TryGetValue(uid, out var entry))
            {
                Entries.Remove(uid);

                // Invoke notification
                EntryRemoved.Invoke(entry);

                // Destroy entry after removal
                if (destroy) entry.Destroy();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns hashed Guids A and B of given entry.
        /// </summary>
        public static (Guid A, Guid B) GetHash(ENTRY entry)
        {
            var data = entry.GetHashableData();
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            foreach (var item in data)
                writer.Encode(item);

            var hash = SHA256.HashData(stream.ToArray());

            return (new(hash[..16]), new(hash[16..]));
        }

        /// <summary>
        /// Only updates local Entries cache; does not affect usage counts, hashes, or UID tracking.
        /// Use with care in sync scenarios.
        /// </summary>
        public void Apply(params ENTRY[] entries)
        {
            if (entries.IsEmpty()) return;

            foreach (var entry in entries)
                if (entry.NotNull() && entry.UID != 0)
                {
                    if (Entries.TryGetValue(entry.UID, out var old))
                    {
                        Debug.LogError($"CopyOnWrite: Entry {entry.UID} was already present in cache. Destroying old entry.");
                        old.Destroy();
                    }

                    Entries[entry.UID] = entry;
                }
        }

        /// <summary>
        /// Interface used for CopyOnWrite entries.
        /// </summary>
        public interface IEntry
        {
            // Unique Identifier for the entry
            public uint UID { get; set; }

            // Here you return properties you want to hash for the entry like a name and other values
            public object[] GetHashableData();

            // Can be redirected to Godot.Resource.Duplicate()
            public T DuplicateEntry<T>() where T : ENTRY;

            // Just return "this" if it's already a safe copy, else return a duplicate
            public T GetSafeEntry<T>() where T : ENTRY;

            // Dispose of the entry
            public void Destroy();
        }
    }
}