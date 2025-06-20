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
        // General Caches
        public Dictionary<uint, ENTRY> Entries { get; set; }

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

        /// <summary>
        /// Reduce usage count of given uid by given count. Removes card if usage count is below 1.
        /// </summary>
        public void Remove(uint uid, int count)
        {
            if ((count = Mathf.Abs(count)) < 1 || UsageCount.TryGetValue(uid, out var usageCount) == false) return;

            if (usageCount >= count)
            {
                UsageCount.Remove(uid);
                Entries.Remove(uid);

                if (Hashes.TryGetValue(uid, out var hashes))
                {
                    Hashed.Remove(hashes.A, hashes.B);
                    Hashes.Remove(uid);
                }
            }

            else UsageCount[uid] -= (uint)count;
        }

        /// <summary>
        /// Prepares entry of uid for modification. Use returned entry for modifications. Call FinishModification(entry) after modifications.
        /// </summary>
        public ENTRY PrepareForModification(uint uid, int count)
        {
            if (uid < 1 || Entries.TryGetValue(uid, out var entry) == false) return default;

            // Allow modifying the given instance directly
            if (UsageCount.TryGetValue(uid, out var usageCount) == false || usageCount <= count) return entry;

            // Reduce usages by one
            Remove(uid, count);

            var duplicate = entry.DuplicateEntry<ENTRY>();
            duplicate.UID = 0;

            return duplicate;
        }

        /// <summary>
        /// Registers given entry after modification.
        /// </summary>
        public void FinishModification(ENTRY entry) => Add(entry);

        /// <summary>
        /// Registers entry, assigns UID and increases usage count by given count.
        /// </summary>
        public uint Add(ENTRY entry, int count = 1)
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

            // Awnser with uid after creation
            return uid;
        }

        /// <summary>
        /// Returns hashed Guids A and B of given entry.
        /// </summary>
        public static (Guid A, Guid B) GetHash(ENTRY entry)
        {
            var data = entry.GetHashableData();

            using var sha256 = SHA256.Create();
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            foreach (var item in data)
            {
                writer.Encode(item);
                writer.Encode('|');
            }

            var hash = sha256.ComputeHash(stream.ToArray());

            return (new(hash[..16]), new(hash[16..]));
            //return Convert.ToHexString(hash); // -> 32 bytes
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