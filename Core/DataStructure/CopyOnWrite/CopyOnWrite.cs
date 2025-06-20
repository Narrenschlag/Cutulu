namespace Cutulu.Core
{
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using System.IO;
    using System;
    using Godot;

    /// <summary>
    /// This class was mainly made for the authoritive card dealer of my Merchants & Mercenaries game. It does what it's name suggests.
    /// </summary>
    public abstract class CopyOnWrite<ENTRY> where ENTRY : ICopyOnWriteEntry
    {
        // General Caches and Notifiers
        public Dictionary<uint, ENTRY> Entries { get; set; }

        public readonly Notification<ENTRY> EntryAdded = new();
        public readonly Notification<ENTRY> EntryUpdated = new();
        public readonly Notification<ENTRY> EntryRemoved = new();

        // Authoritive Caches and Values
        public Dictionary<uint, (Guid A, Guid B)> Hashes { get; set; }
        public Dictionary<Guid, Guid, uint> Hashed { get; set; }
        public Dictionary<uint, uint> UsageCount { get; set; }
        
        private uint EntryUID { get; set; }

        public CopyOnWrite()
        {
            Entries = [];

            UsageCount = [];
            Hashed = [];
            Hashes = [];

            EntryUID = 0;
        }

        public void Clear()
        {
            Entries.Clear();
            UsageCount.Clear();
            Hashed.Clear();
            Hashes.Clear();
            EntryUID = 0;
        }

        public IEnumerable<ENTRY> GetEntries() => Entries.Values;

        public ENTRY GetEntry(uint uid, ENTRY @default = default) => TryGetEntry(uid, out var entry) ? entry : @default;

        public bool ContainsEntry(uint uid) => Entries.ContainsKey(uid);

        public bool TryGetEntry(uint uid, out ENTRY entry) => Entries.TryGetValue(uid, out entry);

        /// <summary>
        /// This is the main function of the class. Use this if you want to get the entry UID for the base entry object. For example the file it is based on. 
        /// </summary>
        public uint GetOrCreateUID(ENTRY entryBase)
        {
            (var A, var B) = GetHash(entryBase);

            if (Hashed.TryGetValue(A, B, out var uid)) return uid;

            return Add(entryBase.DuplicateEntry<ENTRY>());
        }

        /// <summary>
        /// Prepares entry of uid for modification. Use returned entry for modifications. Call FinishModification(entry) after modifications.
        /// </summary>
        public ENTRY PrepareForModification(uint uid, int count)
        {
            if (uid < 1 || Entries.TryGetValue(uid, out var entry) == false) return default;

            // Allow modifying the given instance directly
            if (UsageCount.TryGetValue(uid, out var usageCount) == false || usageCount <= count) return entry;

            var duplicate = entry.DuplicateEntry<ENTRY>();
            duplicate.UID = 0;

            // Reduce usages by one
            Remove(uid, count);

            return duplicate;
        }

        /// <summary>
        /// Registers given entry after modification.
        /// </summary>
        public void FinishModification(ENTRY entry, int count)
        {
            // Add but do not invoke add notification
            Add(entry, count, false);

            // Invoke notification
            EntryUpdated.Invoke(entry);
        }

        /// <summary>
        /// Registers entry, assigns UID and increases usage count by given count.
        /// </summary>
        public uint Add(ENTRY entry, int count = 1)
        {
            // Add and invoke add notification
            return Add(entry, count, true);
        }

        private uint Add(ENTRY entry, int count, bool invokeAdded)
        {
            // Non valid entry
            if (entry.IsNull()) return default;
            var _count = (uint)Mathf.Max(1, count);

            // Get hashed GUIDs of the entry
            var hashed = GetHash(entry);

            // Check if there is already a duplicate
            if (Hashed.TryGetValue(hashed.A, hashed.B, out var uid))
            {
                UsageCount[uid] += _count;
                return uid;
            }

            // Create and assign UID
            uid = ++EntryUID;
            entry.UID = uid;

            // Register entry under uid
            Hashed[hashed.A, hashed.B] = uid;
            UsageCount[uid] = _count;
            Entries[uid] = entry;
            Hashes[uid] = hashed;

            // Invoke notification
            if (invokeAdded) EntryAdded.Invoke(entry);

            // Awnser with uid after creation
            return uid;
        }

        /// <summary>
        /// Reduce usage count of given uid by given count. Removes card if usage count is below 1.
        /// </summary>
        public void Remove(uint uid, int count)
        {
            if (count == 0 || UsageCount.TryGetValue(uid, out var usageCount) == false) return;
            count = Mathf.Abs(count);

            if (count >= usageCount)
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
                }
            }

            else UsageCount[uid] -= (uint)count;
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
        /// Apply entries for non authorative synchronisation.
        /// </summary>
        public void Apply(params ENTRY[] entries)
        {
            if (entries.IsEmpty()) return;

            foreach (var entry in entries)
                if (entry.NotNull())
                    Entries[entry.UID] = entry;
        }
    }

    /// <summary>
    /// Interface used for CopyOnWrite entries.
    /// </summary>
    public interface ICopyOnWriteEntry
    {
        public uint UID { get; set; }

        public object[] GetHashableData();

        public T DuplicateEntry<T>() where T : ICopyOnWriteEntry;
    }
}