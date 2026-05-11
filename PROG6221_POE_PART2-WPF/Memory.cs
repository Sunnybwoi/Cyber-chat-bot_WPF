using System.Collections.Generic;

namespace PROG6221_POE_PART2_WPF
{
    /* Memory class to store and retrieve user-specific data across the conversation session.
     * Implements a simple key-value store using a Dictionary for O(1) lookups.
     * Designed for future expansion in Part 3/POE.
     * Code completion assisted by Visual Studio's IntelliCode (Microsoft Corporation, 2022). Version 17.8.
     */
    public static class Memory
    {
        // Read-only automatic property
        public static int TopicCount => _topicHistory.Count;  

        // Stores arbitrary user facts e.g. "name" -> "ALICE", "interest" -> "PHISHING"
        private static readonly Dictionary<string, string> _store = new Dictionary<string, string>();

        // Ordered log of every topic the user has discussed this session
        private static readonly List<string> _topicHistory = new List<string>();

        /// <summary>Saves or overwrites a value for the given key.</summary>
        public static void Set(string key, string value)
        {
            _store[key.ToLower()] = value;
        }

        /// <summary>Returns the stored value, or null if the key does not exist.</summary>
        public static string Get(string key)
        {
            _store.TryGetValue(key.ToLower(), out string value);
            return value;
        }

        /// <summary>Returns true if the key exists in memory.</summary>
        public static bool Has(string key)
        {
            return _store.ContainsKey(key.ToLower());
        }

        /// <summary>Records a topic the user interacted with for recall later.</summary>
        public static void LogTopic(string topic)
        {
            if (!string.IsNullOrWhiteSpace(topic) && !_topicHistory.Contains(topic.ToLower()))
                _topicHistory.Add(topic.ToLower());
        }

        /// <summary>Returns a read-only view of the topic history list.</summary>
        public static IReadOnlyList<string> GetTopicHistory() => _topicHistory.AsReadOnly();

        /// <summary>Clears all stored memory — useful for a fresh session.</summary>
        public static void Clear()
        {
            _store.Clear();
            _topicHistory.Clear();
        }
    }
}

/* References:
 * Microsoft Corporation (2022) Visual Studio IntelliSense [Software]. Version 17.8.
 * Available at: https://visualstudio.microsoft.com/services/intellicode/ (Accessed: 11 March 2026).
 */
