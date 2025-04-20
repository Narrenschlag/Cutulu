namespace Cutulu.Network
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public class IPv6Phraser
    {
        // Word lists for creating memorable sentences
        private static readonly string[] Subjects = {
            "cat", "dog", "bird", "fish", "fox", "wolf", "frog", "bear", "duck", "king",
            "queen", "knight", "child", "farmer", "doctor", "chef", "pilot", "sailor", "wizard", "giant",
            "girl", "boy", "man", "woman", "player", "driver", "hero", "robot", "monkey", "tiger",
            "lion", "mouse", "horse", "deer", "sheep", "goat", "cow", "pig", "hen", "owl",
            "crow", "hawk", "bat", "snake", "frog", "toad", "fish", "whale", "shark", "seal",
            "ant", "bee", "wasp", "fly", "worm", "snail", "crab", "star", "alien"
        };

        private static readonly string[] Adjectives = {
            "red", "blue", "green", "black", "white", "tall", "short", "big", "small", "old",
            "new", "hot", "cold", "fast", "slow", "wild", "calm", "loud", "quiet", "bright",
            "dark", "sly", "bold", "shy", "wise", "proud", "brave", "kind", "mean", "rich",
            "poor", "young", "thin", "fat", "smart", "silly", "tiny", "huge", "sleepy", "happy",
            "sad", "angry", "lucky", "quick", "lazy", "busy", "clean", "dirty", "crazy", "noble",
            "odd", "even", "fair", "rare", "lone", "dry", "wet", "high", "low", "deep"
        };

        private static readonly string[] Verbs = {
            "finds", "takes", "gives", "eats", "drinks", "sees", "hears", "holds", "drops", "makes",
            "builds", "breaks", "fixes", "moves", "stops", "starts", "likes", "loves", "helps", "meets",
            "greets", "fights", "chases", "races", "beats", "ties", "joins", "leads", "follows", "wins",
            "loses", "seeks", "hides", "shows", "tells", "asks", "calls", "sends", "brings", "leaves",
            "buys", "sells", "pays", "gets", "keeps", "saves", "opens", "closes", "lifts", "draws",
            "pulls", "pushes", "carries", "throws", "catches", "hits", "cuts", "grows", "cooks", "wears"
        };

        private static readonly string[] Objects = {
            "gem", "key", "coin", "ring", "book", "map", "card", "gift", "tool", "crown",
            "cloak", "hat", "mask", "sword", "shield", "wand", "staff", "bow", "dart", "stone",
            "ball", "toy", "game", "door", "lock", "chest", "box", "bag", "cup", "bowl",
            "plate", "cake", "pie", "fruit", "nut", "seed", "leaf", "tree", "rose", "gold",
            "silver", "glass", "wood", "cloth", "silk", "wool", "rope", "net", "web", "drum",
            "flute", "pipe", "bell", "horn", "lyre", "harp", "song", "tale", "joke", "sign",
            "flag", "boat", "ship", "cart", "wheel", "path", "road", "bridge", "gate", "wall"
        };

        private static readonly string[] Locations = {
            "home", "cave", "nest", "den", "hill", "lake", "pond", "sea", "bay", "isle",
            "beach", "coast", "port", "town", "city", "farm", "field", "park", "woods", "camp",
            "fort", "tower", "castle", "palace", "school", "store", "shop", "mall", "bank", "well",
            "spring", "creek", "stream", "river", "falls", "cliff", "peak", "mount", "plain", "marsh",
            "swamp", "bog", "pool", "dock", "yard", "path", "road", "street", "hall", "room",
            "court", "square", "stage", "bridge", "gate", "wall", "shrine", "grove", "glen", "vale"
        };

        private static readonly string[] Times = {
            "dawn", "dusk", "night", "noon", "day", "spring", "summer", "fall", "winter", "now",
            "soon", "later", "never", "always", "often", "once", "twice", "daily", "weekly", "yearly"
        };

        private static readonly string[] Prepositions = {
            "in", "on", "at", "by", "with", "near", "under", "over", "before", "after",
            "during", "through", "across", "along", "behind", "beside", "beyond", "among", "within", "without",
            "outside", "inside", "around", "within"
        };

        // Generate binary value encodings for each part of speech
        private static Dictionary<string, int> GenerateWordEncodings(string[] words, int bitCount)
        {
            var dict = new Dictionary<string, int>();
            int max = 1 << bitCount; // 2^bitCount

            for (int i = 0; i < words.Length && i < max; i++)
            {
                dict[words[i]] = i;
            }

            return dict;
        }

        // Dictionaries to map words to their binary values
        private static readonly Dictionary<string, int> SubjectDict = GenerateWordEncodings(Subjects, 6);    // 6 bits (0-63)
        private static readonly Dictionary<string, int> AdjectiveDict = GenerateWordEncodings(Adjectives, 6); // 6 bits (0-63)
        private static readonly Dictionary<string, int> VerbDict = GenerateWordEncodings(Verbs, 6);          // 6 bits (0-63)
        private static readonly Dictionary<string, int> ObjectDict = GenerateWordEncodings(Objects, 6);      // 6 bits (0-63)
        private static readonly Dictionary<string, int> LocationDict = GenerateWordEncodings(Locations, 6);  // 6 bits (0-63)
        private static readonly Dictionary<string, int> TimeDict = GenerateWordEncodings(Times, 5);          // 5 bits (0-31)
        private static readonly Dictionary<string, int> PrepDict = GenerateWordEncodings(Prepositions, 5);   // 5 bits (0-31)

        // Reverse dictionaries to lookup words by their binary values
        private static readonly Dictionary<int, string> SubjectLookup = SubjectDict.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<int, string> AdjectiveLookup = AdjectiveDict.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<int, string> VerbLookup = VerbDict.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<int, string> ObjectLookup = ObjectDict.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<int, string> LocationLookup = LocationDict.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<int, string> TimeLookup = TimeDict.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<int, string> PrepLookup = PrepDict.ToDictionary(x => x.Value, x => x.Key);

        /// <summary>
        /// Converts an IPv6 address to a memorable sentence
        /// </summary>
        public static string IPv6ToSentence(string ipv6Address)
        {
            // Parse and validate the IPv6 address
            if (!IPAddress.TryParse(ipv6Address, out IPAddress? ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                throw new ArgumentException("Invalid IPv6 address");

            // Convert to bytes
            byte[] bytes = ip.GetAddressBytes();

            // Create bit reader to process the bytes
            var reader = new BitReader(bytes);

            // Read bits for each part of speech
            int subjValue = reader.ReadBits(6);
            int adjValue = reader.ReadBits(6);
            int verbValue = reader.ReadBits(6);
            int objValue = reader.ReadBits(6);
            int prepValue = reader.ReadBits(5);
            int locValue = reader.ReadBits(6);
            int timeValue = reader.ReadBits(5);

            // Use remaining bits for a second part of the sentence
            int subj2Value = reader.ReadBits(6);
            int adj2Value = reader.ReadBits(6);
            int verb2Value = reader.ReadBits(6);
            int obj2Value = reader.ReadBits(6);
            int prep2Value = reader.ReadBits(5);
            int loc2Value = reader.ReadBits(6);

            // The remaining bits can be used as a checksum or indicator
            int remainingBits = reader.ReadRemainingBits();

            // Generate a coherent sentence
            string sentence = $"The {AdjectiveLookup[adjValue]} {SubjectLookup[subjValue]} {VerbLookup[verbValue]} " +
                             $"the {ObjectLookup[objValue]} {PrepLookup[prepValue]} the {LocationLookup[locValue]} " +
                             $"at {TimeLookup[timeValue]} while " +
                             $"the {AdjectiveLookup[adj2Value]} {SubjectLookup[subj2Value]} {VerbLookup[verb2Value]} " +
                             $"the {ObjectLookup[obj2Value]} {PrepLookup[prep2Value]} the {LocationLookup[loc2Value]}";

            // Add indicator for any remaining bits (if needed)
            if (remainingBits > 0)
            {
                sentence += $" ({remainingBits})";
            }

            return sentence;
        }

        /// <summary>
        /// Converts a memorable sentence back to an IPv6 address
        /// </summary>
        public static string SentenceToIPv6(string sentence)
        {
            var parts = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 14)
                throw new ArgumentException("Invalid sentence format");

            // Extract words from the sentence
            string adj1 = parts[1];
            string subj1 = parts[2];
            string verb1 = parts[3];
            string obj1 = parts[5];
            string prep1 = parts[6];
            string loc1 = parts[8];
            string time = parts[10];
            string adj2 = parts[13];
            string subj2 = parts[14];
            string verb2 = parts[15];
            string obj2 = parts[17];
            string prep2 = parts[18];
            string loc2 = parts[20];

            // Get binary values for each word
            if (!AdjectiveDict.TryGetValue(adj1, out int adjValue) ||
                !SubjectDict.TryGetValue(subj1, out int subjValue) ||
                !VerbDict.TryGetValue(verb1, out int verbValue) ||
                !ObjectDict.TryGetValue(obj1, out int objValue) ||
                !PrepDict.TryGetValue(prep1, out int prepValue) ||
                !LocationDict.TryGetValue(loc1, out int locValue) ||
                !TimeDict.TryGetValue(time, out int timeValue) ||
                !AdjectiveDict.TryGetValue(adj2, out int adj2Value) ||
                !SubjectDict.TryGetValue(subj2, out int subj2Value) ||
                !VerbDict.TryGetValue(verb2, out int verb2Value) ||
                !ObjectDict.TryGetValue(obj2, out int obj2Value) ||
                !PrepDict.TryGetValue(prep2, out int prep2Value) ||
                !LocationDict.TryGetValue(loc2, out int loc2Value))
            {
                throw new ArgumentException("The sentence contains invalid words");
            }

            // Create bit writer to build the IPv6 address
            var writer = new BitWriter();

            // Write bits for each part of speech
            writer.WriteBits(subjValue, 6);
            writer.WriteBits(adjValue, 6);
            writer.WriteBits(verbValue, 6);
            writer.WriteBits(objValue, 6);
            writer.WriteBits(prepValue, 5);
            writer.WriteBits(locValue, 6);
            writer.WriteBits(timeValue, 5);
            writer.WriteBits(subj2Value, 6);
            writer.WriteBits(adj2Value, 6);
            writer.WriteBits(verb2Value, 6);
            writer.WriteBits(obj2Value, 6);
            writer.WriteBits(prep2Value, 5);
            writer.WriteBits(loc2Value, 6);

            // Pad with zeros to complete the IPv6 address (128 bits total)
            writer.PadWithZeros();

            // Convert bits to IPv6 address
            var bytes = writer.GetBytes();
            var ipv6 = new IPAddress(bytes);
            return ipv6.ToString();
        }
    }

    // Helper class to read bits from a byte array
    public class BitReader
    {
        private readonly byte[] _bytes;
        private int _byteIndex = 0;
        private int _bitIndex = 0;

        public BitReader(byte[] bytes)
        {
            _bytes = bytes;
        }

        public int ReadBits(int count)
        {
            int result = 0;

            for (int i = 0; i < count; i++)
            {
                if (_byteIndex >= _bytes.Length)
                    return result;

                // Get the current bit
                int bit = (_bytes[_byteIndex] >> (7 - _bitIndex)) & 1;
                result = (result << 1) | bit;

                // Move to the next bit
                _bitIndex++;
                if (_bitIndex == 8)
                {
                    _byteIndex++;
                    _bitIndex = 0;
                }
            }

            return result;
        }

        public int ReadRemainingBits()
        {
            int remainingBits = (_bytes.Length * 8) - (_byteIndex * 8 + _bitIndex);
            int result = 0;

            for (int i = 0; i < remainingBits; i++)
            {
                // Get the current bit
                int bit = (_bytes[_byteIndex] >> (7 - _bitIndex)) & 1;
                result = (result << 1) | bit;

                // Move to the next bit
                _bitIndex++;
                if (_bitIndex == 8)
                {
                    _byteIndex++;
                    _bitIndex = 0;
                }
            }

            return result;
        }
    }

    // Helper class to write bits to a byte array
    public class BitWriter
    {
        private readonly List<byte> _bytes = new List<byte>();
        private byte _currentByte = 0;
        private int _bitIndex = 0;

        public void WriteBits(int value, int count)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                // Get the current bit from the value
                int bit = (value >> i) & 1;

                // Set the bit in the current byte
                _currentByte = (byte)(_currentByte | (bit << (7 - _bitIndex)));

                // Move to the next bit
                _bitIndex++;
                if (_bitIndex == 8)
                {
                    _bytes.Add(_currentByte);
                    _currentByte = 0;
                    _bitIndex = 0;
                }
            }
        }

        public void PadWithZeros()
        {
            // If we have a partial byte, add it
            if (_bitIndex > 0)
            {
                _bytes.Add(_currentByte);
                _currentByte = 0;
                _bitIndex = 0;
            }

            // Pad with zeros to complete 16 bytes (128 bits)
            while (_bytes.Count < 16)
            {
                _bytes.Add(0);
            }
        }

        public byte[] GetBytes()
        {
            return _bytes.ToArray();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Example usage
            string ipv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";

            try
            {
                // Convert IPv6 to memorable sentence
                string sentence = IPv6Phraser.IPv6ToSentence(ipv6);
                Console.WriteLine("IPv6 to Sentence:");
                Console.WriteLine(ipv6);
                Console.WriteLine(sentence);

                // Convert sentence back to IPv6
                string convertedBack = IPv6Phraser.SentenceToIPv6(sentence);
                Console.WriteLine("\nSentence back to IPv6:");
                Console.WriteLine(convertedBack);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}