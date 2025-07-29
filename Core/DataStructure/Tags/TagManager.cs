namespace Cutulu.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// <para>Tags tagables to make them easier to find</para>
    /// <para>Tagables can have multiple tags</para>
    /// </summary>
    public partial class TagManager<Tagable> where Tagable : ITagable
    {
        private readonly Dictionary<object, HashSet<Tagable>> TagablesByTag = [];
        private readonly Dictionary<long, TagReference> TagablesByUID = [];
        private readonly Dictionary<Tagable, int> UniqueTagables = [];

        public IReadOnlyCollection<Tagable> TagEntries => UniqueTagables.Keys;

        /// <summary>
        /// Event called when a tagable is tagged
        /// </summary>
        public readonly Notification<(Tagable, object)> TagUpdated = new();

        /// <summary>
        /// Adds a tag to a tagable
        /// </summary>
        public void Tag(Tagable _tagable, object _tag)
        {
            Add(_tagable, _tag);
        }

        /// <summary>
        /// Removes a tag from a tagable
        /// </summary>
        public void Untag(Tagable _tagable, object _tag)
        {
            Remove(_tagable, _tag);
        }

        /// <summary>
        /// Removes all of a tagable's tags
        /// </summary>
        public void ClearTags(Tagable _tagable)
        {
            Remove(_tagable);
        }

        /// <summary>
        /// Return all tagables with the given tag
        /// </summary>
        public HashSet<Tagable> Get(object _tag)
        {
            return TagablesByTag.TryGetValue(_tag, out var _tagables) ? _tagables : [];
        }

        /// <summary>
        /// Returns all tags of given tagable
        /// </summary>
        public HashSet<object> GetTagsOf(Tagable _tagable)
        {
            return _tagable != null && TagablesByUID.TryGetValue(_tagable.GetUniqueTagID(), out var _data) && _data.Tags != null ? _data.Tags : [];
        }

        /// <summary>
        /// Returns true if tagable has given tag
        /// </summary>
        public bool HasTag(Tagable _tagable, object _tag)
        {
            if (_tagable == null || _tag == null) return false;

            return GetTagsOf(_tagable).Contains(_tag);
        }

        /// <summary>
        /// Clears all tags
        /// </summary>
        public void Clear()
        {
            var cache = new Dictionary<object, HashSet<Tagable>>(TagablesByTag);

            TagablesByTag.Clear();
            TagablesByUID.Clear();

            // Notify about taged values
            foreach (var pair in cache)
                foreach (var tag in pair.Value)
                    TagUpdated.Invoke((tag, pair.Key));
        }

        private void Add(Tagable _tagable, params object[] _tags)
        {
            if (_tagable == null) return;

            // Add tagable to dictionary
            if (TagablesByUID.TryGetValue(_tagable.GetUniqueTagID(), out var _data) == false)
                TagablesByUID[_tagable.GetUniqueTagID()] = _data = new(_tagable);

            // Assign unique tag count
            if (UniqueTagables.ContainsKey(_tagable) == false) UniqueTagables[_tagable] = 1;
            else UniqueTagables[_tagable]++;

            // Add tags to tagable
            if (_tags.NotEmpty())
            {
                foreach (var _tag in _tags)
                {
                    if (_tag == null) continue;

                    // Get tag tagables
                    if (TagablesByTag.TryGetValue(_tag, out var _tagables) == false)
                        TagablesByTag[_tag] = _tagables = [];

                    // Add to tag tagables
                    if (_tagables.Contains(_tagable) == false)
                        _tagables.Add(_tagable);

                    // Add tag to data
                    if (_data.Tags.Contains(_tag) == false)
                        _data.Tags.Add(_tag);

                    TagUpdated.Invoke((_tagable, _tag));
                }
            }
        }

        private void Remove(Tagable _tagable, params object[] _tags)
        {
            // Return if not registered
            if (_tagable == null || TagablesByUID.TryGetValue(_tagable.GetUniqueTagID(), out var _data) == false) return;

            // Assign unique tag count
            if (UniqueTagables.ContainsKey(_tagable) && UniqueTagables[_tagable]-- < 1)
                UniqueTagables.Remove(_tagable);

            // Delete all data of tagable
            if (_tags.IsEmpty())
            {
                // Remove self
                TagablesByUID.Remove(_tagable.GetUniqueTagID());

                // Remove from tags
                if (_data.Tags.NotEmpty())
                {
                    foreach (var _tag in _data.Tags)
                    {
                        _remove(_tag);
                    }
                }
            }

            // Remove tags from tagable
            else
            {
                foreach (var _tag in _tags)
                {
                    _remove(_tag);
                }
            }

            void _remove(object _tag)
            {
                if (_tag == null) return;

                if (TagablesByTag.TryGetValue(_tag, out var _tagables) && _tagables.Contains(_tagable))
                {
                    _tagables.Remove(_tagable);

                    if (_tagables.Count < 1)
                        TagablesByTag.Remove(_tag);

                    TagUpdated.Invoke((_tagable, _tag));
                }
            }
        }

        private struct TagReference(Tagable _tagable)
        {
            public readonly Tagable Tagable = _tagable;
            public HashSet<object> Tags = [];
        }
    }
}