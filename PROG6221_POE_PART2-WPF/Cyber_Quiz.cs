using PROG6221_POE_PART2_WPF;
using System;
using System.Collections.Generic;

namespace PROG6221_POE_PART2_WPF
{
    /* Cyber_Quiz handles all chatbot logic:
     *   - Keyword recognition (6 cybersecurity topics + general queries)
     *   - Random responses using List<string> pools per topic
     *   - Sentiment detection using string[] keyword arrays
     *   - Conversation flow: follow-up phrases re-trigger the last topic
     *   - Memory integration: personalises responses using stored user data
     *   - Error handling: graceful default for unrecognised input
     *
     * All UI output is returned as a plain string — ChatWindow is responsible
     * for rendering it, keeping logic and presentation fully separated (OOP).
     *
     * Code completion assisted by Visual Studio's IntelliCode (Microsoft Corporation, 2022). Version 17.8.
     */
    public static class Cyber_Quiz
    {
        // ── Automatic properties ──────────────────────────────────────────────────
        // GitHub Copilot (AI code assistant). Date: 2026-03-25.
        // Prompt: "Implement automatic properties in my code"

        /// <summary>Returns the last cybersecurity topic the user discussed.</summary>
        public static string LastTopic { get; private set; } = "";

        /// <summary>Total number of unique topics discussed this session.</summary>
        public static int TopicsDiscussed => Memory.TopicCount;

        // ── Keyword arrays for sentiment detection ────────────────────────────────
        // Using static readonly string[] arrays for O(n) keyword lookup.
        // Arrays chosen here because the keyword sets are fixed-size and never modified.
        // Part 2 requirement: use arrays or lists to manage data effectively.
        // Code completion assisted by Visual Studio's IntelliCode (Microsoft Corporation, 2022). Version 17.8.

        private static readonly string[] _worriedKeywords =
        {
            "worried", "scared", "anxious", "afraid", "nervous", "stressed", "panic"
        };

        private static readonly string[] _frustratedKeywords =
        {
            "frustrated", "angry", "annoyed", "confused", "lost", "irritated", "upset"
        };

        private static readonly string[] _curiousKeywords =
        {
            "curious", "interested", "want to know", "tell me about",
            "how does", "what is", "explain", "wondering"
        };

        private static readonly string[] _happyKeywords =
        {
            "happy", "excited", "glad", "great", "awesome", "love", "fantastic"
        };

        private static readonly string[] _followUpKeywords =
        {
            "another tip", "tell me more", "explain more", "more info",
            "more detail", "go on", "continue", "what else", "keep going"
        };

        // ── Random response pools (List<string> — variable-size collections) ──────
        // Generated with assistance from Anthropic (2026) Claude [AI assistant].
        // Prompt: 'How to store multiple predefined bot responses and pick one randomly in C#', April 2026.

        private static readonly Random _rng = new Random();

        private static readonly List<string> _passwordResponses = new List<string>
        {
            "PASSWORD SAFETY — TIP 1\n\n" +
            "USE LONG, COMPLEX PASSWORDS OR PASSPHRASES.\n" +
            "EXAMPLE: 'BlueSky$River!42' IS FAR STRONGER\n" +
            "THAN 'password123'.\n\n" +
            "ENABLE MULTI-FACTOR AUTHENTICATION (MFA)\n" +
            "ON EVERY ACCOUNT THAT SUPPORTS IT.",

            "PASSWORD SAFETY — TIP 2\n\n" +
            "NEVER REUSE PASSWORDS ACROSS DIFFERENT SITES.\n" +
            "IF ONE SITE IS BREACHED, ALL YOUR ACCOUNTS\n" +
            "BECOME VULNERABLE.\n\n" +
            "USE A PASSWORD MANAGER SUCH AS BITWARDEN\n" +
            "OR 1PASSWORD TO GENERATE AND STORE CREDENTIALS.",

            "PASSWORD SAFETY — TIP 3\n\n" +
            "CHANGE YOUR PASSWORDS REGULARLY —\n" +
            "ESPECIALLY AFTER A DATA BREACH ALERT.\n\n" +
            "CHECK IF YOUR EMAIL HAS BEEN COMPROMISED AT\n" +
            "HAVEIBEENPWNED.COM AND ACT IMMEDIATELY.",

            "PASSWORD SAFETY — TIP 4\n\n" +
            "AVOID USING PERSONAL DETAILS IN PASSWORDS:\n" +
            "   YOUR NAME, BIRTHDAY, OR PET'S NAME\n" +
            "   ARE THE FIRST THINGS ATTACKERS TRY.\n\n" +
            "RANDOM PASSPHRASES OF 4+ UNRELATED WORDS\n" +
            "ARE BOTH MEMORABLE AND HIGHLY SECURE."
        };

        private static readonly List<string> _phishingResponses = new List<string>
        {
            "PHISHING — TIP 1\n\n" +
            "BE CAUTIOUS OF UNSOLICITED EMAILS OR MESSAGES.\n" +
            "SCAMMERS DISGUISE THEMSELVES AS BANKS,\n" +
            "DELIVERY COMPANIES, OR EVEN YOUR EMPLOYER.\n\n" +
            "WHEN IN DOUBT, CONTACT THE ORGANISATION\n" +
            "DIRECTLY USING THEIR OFFICIAL WEBSITE.",

            "PHISHING — TIP 2\n\n" +
            "CHECK THE SENDER'S EMAIL ADDRESS CAREFULLY.\n" +
            "FAKE DOMAINS LOOK SIMILAR — E.G.\n" +
            "'support@amaz0n.com' IS NOT AMAZON.\n\n" +
            "HOVER OVER LINKS BEFORE CLICKING TO PREVIEW\n" +
            "THE REAL DESTINATION URL.",

            "PHISHING — TIP 3\n\n" +
            "NEVER ENTER YOUR PASSWORD ON A SITE\n" +
            "YOU REACHED VIA AN EMAIL LINK.\n\n" +
            "INSTEAD, OPEN A NEW BROWSER TAB AND\n" +
            "NAVIGATE TO THE OFFICIAL SITE MANUALLY.\n\n" +
            "LOOK FOR HTTPS AND A PADLOCK ICON.",

            "PHISHING — TIP 4\n\n" +
            "URGENCY IS A RED FLAG.\n" +
            "PHRASES LIKE 'ACT NOW', 'YOUR ACCOUNT WILL\n" +
            "BE SUSPENDED', OR 'LIMITED TIME OFFER'\n" +
            "ARE COMMON PHISHING TACTICS.\n\n" +
            "TAKE YOUR TIME — LEGITIMATE COMPANIES\n" +
            "DO NOT PRESSURE YOU TO ACT IMMEDIATELY."
        };

        private static readonly List<string> _safeBrowsingResponses = new List<string>
        {
            "SAFE BROWSING — TIP 1\n\n" +
            "ONLY VISIT TRUSTED WEBSITES WITH HTTPS.\n" +
            "THE PADLOCK ICON IN YOUR BROWSER BAR\n" +
            "INDICATES AN ENCRYPTED CONNECTION.\n\n" +
            "AVOID ENTERING PERSONAL DATA ON HTTP SITES.",

            "SAFE BROWSING — TIP 2\n\n" +
            "USE A VPN (VIRTUAL PRIVATE NETWORK)\n" +
            "WHEN CONNECTING TO PUBLIC WI-FI.\n\n" +
            "PUBLIC HOTSPOTS IN CAFÉS AND AIRPORTS\n" +
            "CAN BE MONITORED BY ATTACKERS.",

            "SAFE BROWSING — TIP 3\n\n" +
            "KEEP YOUR BROWSER AND ITS EXTENSIONS\n" +
            "UP TO DATE AT ALL TIMES.\n\n" +
            "OUTDATED BROWSERS CONTAIN KNOWN\n" +
            "VULNERABILITIES THAT ATTACKERS EXPLOIT.\n\n" +
            "CONSIDER USING UBLOCK ORIGIN TO BLOCK\n" +
            "MALICIOUS ADS AND TRACKERS.",

            "SAFE BROWSING — TIP 4\n\n" +
            "BE CAREFUL WHAT YOU DOWNLOAD.\n" +
            "FREE SOFTWARE FROM UNOFFICIAL SOURCES\n" +
            "OFTEN BUNDLES MALWARE OR SPYWARE.\n\n" +
            "ALWAYS DOWNLOAD FROM THE OFFICIAL\n" +
            "DEVELOPER'S WEBSITE OR A TRUSTED STORE."
        };

        private static readonly List<string> _scamResponses = new List<string>
        {
            "ONLINE SCAMS — TIP 1\n\n" +
            "IF AN OFFER SEEMS TOO GOOD TO BE TRUE,\n" +
            "IT ALMOST CERTAINLY IS.\n\n" +
            "LOTTERY WINS, INHERITANCE EMAILS, AND\n" +
            "'GUARANTEED INVESTMENT RETURNS' ARE SCAMS.\n\n" +
            "NEVER SEND MONEY TO SOMEONE YOU HAVEN'T\n" +
            "MET IN PERSON AND FULLY VERIFIED.",

            "ONLINE SCAMS — TIP 2\n\n" +
            "ROMANCE SCAMS ARE ON THE RISE.\n" +
            "ATTACKERS BUILD FAKE RELATIONSHIPS ONLINE\n" +
            "BEFORE REQUESTING MONEY OR GIFT CARDS.\n\n" +
            "DO A REVERSE IMAGE SEARCH ON PROFILE PHOTOS\n" +
            "TO CHECK IF THEY ARE STOLEN IMAGES.",

            "ONLINE SCAMS — TIP 3\n\n" +
            "TECH SUPPORT SCAMS ARE COMMON.\n" +
            "MICROSOFT, APPLE, AND GOOGLE WILL NEVER\n" +
            "CALL YOU UNSOLICITED ABOUT YOUR DEVICE.\n\n" +
            "HANG UP ON SUCH CALLS IMMEDIATELY AND\n" +
            "REPORT THEM TO YOUR LOCAL AUTHORITIES."
        };

        private static readonly List<string> _privacyResponses = new List<string>
        {
            "PRIVACY — TIP 1\n\n" +
            "REVIEW THE PERMISSIONS OF EVERY APP\n" +
            "INSTALLED ON YOUR PHONE OR COMPUTER.\n\n" +
            "APPS THAT REQUEST ACCESS TO YOUR CAMERA,\n" +
            "MICROPHONE, OR CONTACTS WITHOUT REASON\n" +
            "SHOULD BE TREATED WITH SUSPICION.",

            "PRIVACY — TIP 2\n\n" +
            "LIMIT PERSONAL INFORMATION SHARED ON\n" +
            "SOCIAL MEDIA PLATFORMS.\n\n" +
            "YOUR FULL NAME, BIRTHDAY, PHONE NUMBER,\n" +
            "AND LOCATION ARE VALUABLE TO SCAMMERS.\n\n" +
            "SET YOUR PROFILES TO PRIVATE AND REVIEW\n" +
            "YOUR FRIEND/FOLLOWER LIST REGULARLY.",

            "PRIVACY — TIP 3\n\n" +
            "USE PRIVATE BROWSING (INCOGNITO) MODE\n" +
            "FOR SENSITIVE SEARCHES.\n\n" +
            "ALSO CONSIDER USING PRIVACY-FOCUSED\n" +
            "SEARCH ENGINES LIKE DUCKDUCKGO INSTEAD\n" +
            "OF GOOGLE TO REDUCE DATA TRACKING."
        };

        private static readonly List<string> _malwareResponses = new List<string>
        {
            "MALWARE PROTECTION — TIP 1\n\n" +
            "INSTALL REPUTABLE ANTIVIRUS SOFTWARE\n" +
            "AND KEEP IT UPDATED.\n\n" +
            "FREE OPTIONS LIKE WINDOWS DEFENDER ARE\n" +
            "SOLID BASELINES, BUT A DEDICATED SOLUTION\n" +
            "SUCH AS MALWAREBYTES ADDS AN EXTRA LAYER.",

            "MALWARE PROTECTION — TIP 2\n\n" +
            "NEVER OPEN EMAIL ATTACHMENTS FROM\n" +
            "UNKNOWN SENDERS — EVEN .PDF OR .DOCX\n" +
            "FILES CAN CONTAIN MALICIOUS MACROS.\n\n" +
            "SCAN ALL DOWNLOADS WITH YOUR ANTIVIRUS\n" +
            "BEFORE OPENING THEM.",

            "MALWARE PROTECTION — TIP 3\n\n" +
            "KEEP YOUR OPERATING SYSTEM AND ALL\n" +
            "SOFTWARE UP TO DATE.\n\n" +
            "SECURITY PATCHES ARE RELEASED SPECIFICALLY\n" +
            "TO FIX VULNERABILITIES THAT MALWARE EXPLOITS.\n\n" +
            "ENABLE AUTOMATIC UPDATES WHERE POSSIBLE."
        };

        // ══════════════════════════════════════════════════════════════════════════
        //  Public entry point
        // ══════════════════════════════════════════════════════════════════════════

        /* Processes the user's raw input string and returns the bot's response.
         * Order of checks:
         *   1. Sentiment prefix injection (uses string[] arrays + ContainsAny helper)
         *   2. Follow-up / conversation-flow phrases
         *   3. Cybersecurity keyword matching
         *   4. General queries (greetings, purpose, capability)
         *   5. Default / error fallback
         */
        public static string GetResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "PLEASE TYPE A MESSAGE SO I CAN HELP YOU.";

            string lower = input.ToLower().Trim();

            // 1. Detect sentiment and build an optional empathy prefix
            string sentimentPrefix = BuildSentimentPrefix(lower);

            // 2. Follow-up phrases — re-trigger last topic using _followUpKeywords array
            if (IsFollowUp(lower))
            {
                if (!string.IsNullOrEmpty(LastTopic))
                    return sentimentPrefix + PickTopicResponse(LastTopic);
                else
                    return "I DON'T HAVE A PREVIOUS TOPIC TO EXPAND ON.\n" +
                           "TRY ASKING ABOUT PASSWORD SAFETY, PHISHING,\n" +
                           "SCAMS, PRIVACY, MALWARE, OR SAFE BROWSING.";
            }

            // 3. Cybersecurity keyword recognition
            if (lower.Contains("password"))
            {
                LastTopic = "password";
                Memory.LogTopic("password");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_passwordResponses));
            }
            if (lower.Contains("phish"))
            {
                LastTopic = "phishing";
                Memory.LogTopic("phishing");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_phishingResponses));
            }
            if (lower.Contains("scam") || lower.Contains("fraud"))
            {
                LastTopic = "scam";
                Memory.LogTopic("scam");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_scamResponses));
            }
            if (lower.Contains("privacy") || lower.Contains("personal data"))
            {
                LastTopic = "privacy";
                Memory.LogTopic("privacy");
                if (lower.Contains("interested in") || lower.Contains("i care about"))
                    Memory.Set("interest", "privacy");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_privacyResponses));
            }
            if (lower.Contains("malware") || lower.Contains("virus") || lower.Contains("antivirus"))
            {
                LastTopic = "malware";
                Memory.LogTopic("malware");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_malwareResponses));
            }
            if (lower.Contains("safe browsing") || lower.Contains("browsing") ||
                lower.Contains("safe") || lower.Contains("vpn"))
            {
                LastTopic = "safe browsing";
                Memory.LogTopic("safe browsing");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_safeBrowsingResponses));
            }

            // 4. General / conversational queries
            return sentimentPrefix + HandleGeneral(lower);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Sentiment detection — uses string[] arrays
        // ══════════════════════════════════════════════════════════════════════════

        /* Iterates over each keyword array using ContainsAny() to detect the user's
         * emotional tone and returns an empathetic opening line.
         * Part 2 requirement: detect sentiments such as "worried", "curious", "frustrated".
         * Generated with assistance from Anthropic (2026) Claude [AI assistant].
         * Prompt: 'How to implement basic sentiment detection by keyword matching in C#', April 2026.
         */
        private static string BuildSentimentPrefix(string lower)
        {
            if (ContainsAny(lower, _worriedKeywords))
                return "IT'S COMPLETELY UNDERSTANDABLE TO FEEL THAT WAY.\n" +
                       "CYBERSECURITY CAN FEEL OVERWHELMING, BUT YOU'RE\n" +
                       "TAKING THE RIGHT STEP BY LEARNING ABOUT IT.\n\n";

            if (ContainsAny(lower, _frustratedKeywords))
                return "I UNDERSTAND YOUR FRUSTRATION — CYBER THREATS\n" +
                       "CAN BE STRESSFUL. LET'S WORK THROUGH THIS\n" +
                       "TOGETHER, ONE STEP AT A TIME.\n\n";

            if (ContainsAny(lower, _curiousKeywords))
                return "GREAT CURIOSITY! STAYING INFORMED IS YOUR\n" +
                       "BEST DEFENCE ONLINE. HERE'S WHAT YOU NEED:\n\n";

            if (ContainsAny(lower, _happyKeywords))
                return "LOVE THE POSITIVE ENERGY! LET'S KEEP YOU\n" +
                       "SAFE AND SECURE ONLINE.\n\n";

            return ""; // Neutral — no prefix needed
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Conversation flow helpers
        // ══════════════════════════════════════════════════════════════════════════

        /* Uses the _followUpKeywords string[] array to detect follow-up requests.
         * Part 2 requirement: handle "give me another tip", "explain more", "tell me more".
         */
        private static bool IsFollowUp(string lower)
        {
            return ContainsAny(lower, _followUpKeywords);
        }

        /* Picks a random response from the correct pool for the given topic keyword. */
        private static string PickTopicResponse(string topic)
        {
            return topic switch
            {
                "password" => PersonaliseResponse(PickRandom(_passwordResponses)),
                "phishing" => PersonaliseResponse(PickRandom(_phishingResponses)),
                "scam" => PersonaliseResponse(PickRandom(_scamResponses)),
                "privacy" => PersonaliseResponse(PickRandom(_privacyResponses)),
                "malware" => PersonaliseResponse(PickRandom(_malwareResponses)),
                "safe browsing" => PersonaliseResponse(PickRandom(_safeBrowsingResponses)),
                _ => "I'M NOT SURE HOW TO EXPAND ON THAT TOPIC.\n" +
                                   "TYPE 'help' TO SEE AVAILABLE TOPICS."
            };
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Memory personalisation
        // ══════════════════════════════════════════════════════════════════════════

        /* Optionally appends a personalised memory-based follow-up line to a response.
         * Part 2 requirement: use stored user data to make responses more engaging.
         */
        private static string PersonaliseResponse(string response)
        {
            string interest = Memory.Get("interest");

            if (!string.IsNullOrEmpty(interest) &&
                !response.ToLower().Contains(interest.ToLower()))
            {
                response += $"\n\nAS SOMEONE INTERESTED IN {interest.ToUpper()},\n" +
                             "YOU MIGHT ALSO WANT TO REVIEW YOUR ACCOUNT\n" +
                             "SECURITY SETTINGS REGULARLY.";
            }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  General / conversational responses
        // ══════════════════════════════════════════════════════════════════════════

        private static string HandleGeneral(string lower)
        {
            if (lower.Contains("how are you") || lower.Contains("how r you"))
                return "I'M JUST A PROGRAM, BUT I'M RUNNING PERFECTLY!\n" +
                       "THANKS FOR ASKING, " + Visuals_UI.UserName + "!";

            if (lower.Contains("what can you do") || lower.Contains("what do you do"))
                return "I CAN ANSWER QUESTIONS ON:\n" +
                       "   PASSWORD SAFETY\n" +
                       "   PHISHING ATTACKS\n" +
                       "   ONLINE SCAMS & FRAUD\n" +
                       "   PRIVACY PROTECTION\n" +
                       "   MALWARE & VIRUSES\n" +
                       "   SAFE BROWSING HABITS\n\n" +
                       "TYPE 'help' FOR THE FULL COMMAND LIST.";

            if (lower.Contains("purpose") || lower.Contains("what are you"))
                return "MY PURPOSE IS TO HELP YOU LEARN ABOUT\n" +
                       "CYBERSECURITY AWARENESS AND BEST PRACTICES\n" +
                       "TO STAY SAFE ONLINE, " + Visuals_UI.UserName + "!";

            if (lower.Contains("thank you") || lower.Contains("thanks") || lower.Contains("thx"))
                return "THANK_YOU_EXIT"; // Sentinel value — ChatWindow handles the exit flow

            if (lower.Contains("hello") || lower.Contains("hi") || lower.Contains("hey"))
                return $"HELLO, {Visuals_UI.UserName}! HOW CAN I HELP YOU TODAY?\n" +
                       "TYPE 'help' TO SEE WHAT I CAN DO.";

            if (lower.Contains("my name is") || lower.Contains("i am") || lower.Contains("call me"))
            {
                string potentialName = ExtractNameFromInput(lower);
                if (!string.IsNullOrEmpty(potentialName))
                {
                    Memory.Set("name", potentialName.ToUpper());
                    return $"NOTED! I'LL REMEMBER YOUR NAME IS {potentialName.ToUpper()}.";
                }
            }

            if (lower.Contains("interested in") || lower.Contains("i care about"))
            {
                // Use an array to check which topic the user mentioned
                string[] topics = { "password", "phishing", "scam", "privacy", "malware", "browsing" };
                foreach (string topic in topics)
                {
                    if (lower.Contains(topic))
                    {
                        Memory.Set("interest", topic);
                        return $"GREAT! I'LL REMEMBER THAT YOU'RE INTERESTED IN {topic.ToUpper()}.\n" +
                               "IT'S A CRUCIAL PART OF STAYING SAFE ONLINE.\n\n" +
                               "LATER IN OUR CONVERSATION, I'LL RELATE TIPS BACK\n" +
                               "TO YOUR INTEREST WHENEVER RELEVANT.";
                    }
                }
            }

            // Default error-handling response — Part 2 requirement 7
            return "I'M NOT SURE I UNDERSTAND THAT.\n" +
                   "CAN YOU TRY REPHRASING YOUR QUESTION?\n\n" +
                   "TYPE 'help' TO SEE THE LIST OF TOPICS\n" +
                   "AND COMMANDS I CAN HELP YOU WITH.";
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Utility helpers
        // ══════════════════════════════════════════════════════════════════════════

        /* Iterates over a string[] array and returns true if the input contains
         * any of the keywords. Used by all sentiment and follow-up checks.
         * This is the core array-usage method in the class.
         */
        private static bool ContainsAny(string input, string[] keywords)
        {
            foreach (string keyword in keywords)
                if (input.Contains(keyword))
                    return true;
            return false;
        }

        /// <summary>Returns a random item from the given List.</summary>
        private static string PickRandom(List<string> pool)
        {
            return pool[_rng.Next(pool.Count)];
        }

        /* Attempts to extract the user's stated name from phrases like
         * "my name is Alice" or "call me Bob".
         */
        private static string ExtractNameFromInput(string lower)
        {
            string[] markers = { "my name is ", "i am ", "call me ", "i'm " };
            foreach (string marker in markers)
            {
                int idx = lower.IndexOf(marker);
                if (idx >= 0)
                {
                    string after = lower.Substring(idx + marker.Length).Trim();
                    string[] parts = after.Split(' ');
                    if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                        return char.ToUpper(parts[0][0]) + parts[0].Substring(1);
                }
            }
            return null;
        }
    }
}

/* References:
 * Microsoft Corporation (2022) Visual Studio IntelliSense [Software]. Version 17.8.
 * Available at: https://visualstudio.microsoft.com/services/intellicode/ (Accessed: 11 March 2026).
 *
 * GitHub Copilot (2026) [AI code assistant]. Available at: https://github.com/features/copilot (Accessed: 25 March 2026).
 * Prompt: "Implement automatic properties in my code".
 *
 * Anthropic (2026) Claude [AI assistant]. Available at: https://www.anthropic.com (Accessed: April 2026).
 * Prompt: 'How to store multiple predefined bot responses and pick one randomly in C#'.
 * Prompt: 'How to implement basic sentiment detection by keyword matching in C#'.
 */