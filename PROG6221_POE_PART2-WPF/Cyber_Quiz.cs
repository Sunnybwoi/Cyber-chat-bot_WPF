using PROG6221_POE_PART2_WPF;
using System;
using System.Collections.Generic;

namespace PROG6221_POE_PART2_WPF
{
    /* Cyber_Quiz handles all chatbot logic:
     *   - Keyword recognition (6 cybersecurity topics + general queries)
     *   - 30+ total responses across all topics using List<string> pools
     *   - Random response selection per topic for variety
     *   - Sentiment detection using string[] keyword arrays
     *   - Conversation flow: follow-up phrases re-trigger the last topic
     *   - Memory integration: personalises responses using stored user data
     *   - General queries: time, date, bot name, bot purpose, greetings
     *   - Error handling: graceful default for unrecognised input
     *
     * All UI output is returned as a plain string — ChatWindow renders it.
     * This separation follows OOP principles required by Part 2.
     *
     * Code completion assisted by Visual Studio's IntelliCode (Microsoft Corporation, 2022). Version 17.8.
     */
    public static class Cyber_Quiz
    {
        // ── Automatic properties ──────────────────────────────────────────────────

        /// <summary>Returns the last cybersecurity topic the user discussed.</summary>
        public static string LastTopic { get; private set; } = "";

        /// <summary>Total number of unique topics discussed this session.</summary>
        public static int TopicsDiscussed => Memory.TopicCount;

        // ── Keyword arrays for sentiment and conversation detection ───────────────
        // Using static readonly string[] — fixed-size, never modified at runtime.
        // Part 2 requirement: use arrays or lists to manage data effectively.

        private static readonly string[] _worriedKeywords =
        {
            "worried", "scared", "anxious", "afraid", "nervous", "stressed", "panic", "fear"
        };

        private static readonly string[] _frustratedKeywords =
        {
            "frustrated", "angry", "annoyed", "confused", "lost", "irritated", "upset", "overwhelmed"
        };

        private static readonly string[] _curiousKeywords =
        {
            "curious", "interested", "want to know", "tell me about",
            "how does", "what is", "explain", "wondering", "learn"
        };

        private static readonly string[] _happyKeywords =
        {
            "happy", "excited", "glad", "great", "awesome", "love", "fantastic", "pleased"
        };

        private static readonly string[] _followUpKeywords =
        {
            "another tip", "tell me more", "explain more", "more info",
            "more detail", "go on", "continue", "what else", "keep going",
            "give me more", "expand", "elaborate"
        };

        // ── Random response pools ─────────────────────────────────────────────────
        // Lists used here because content may be extended in Part 3.
        // Generated with assistance from Anthropic (2026) Claude [AI assistant].
        // Prompt: 'How to store multiple predefined bot responses and pick one randomly in C#', April 2026.

        private static readonly Random _rng = new Random();

        // PASSWORD — 6 responses
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
            "ARE BOTH MEMORABLE AND HIGHLY SECURE.",

            "PASSWORD SAFETY — TIP 5\n\n" +
            "USE A DIFFERENT PASSWORD FOR YOUR EMAIL\n" +
            "THAN FOR EVERYTHING ELSE.\n\n" +
            "YOUR EMAIL IS THE MASTER KEY — IF IT IS\n" +
            "COMPROMISED, ATTACKERS CAN RESET ALL YOUR\n" +
            "OTHER ACCOUNTS THROUGH IT.",

            "PASSWORD SAFETY — TIP 6\n\n" +
            "ENABLE LOGIN NOTIFICATIONS ON YOUR ACCOUNTS.\n" +
            "MOST BANKS AND EMAIL PROVIDERS CAN ALERT\n" +
            "YOU WHEN A NEW DEVICE SIGNS IN.\n\n" +
            "IF YOU GET AN ALERT YOU DIDN'T TRIGGER,\n" +
            "CHANGE YOUR PASSWORD IMMEDIATELY."
        };

        // PHISHING — 6 responses
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
            "DO NOT PRESSURE YOU TO ACT IMMEDIATELY.",

            "PHISHING — TIP 5\n\n" +
            "SMISHING IS PHISHING VIA SMS TEXT MESSAGE.\n" +
            "NEVER CLICK LINKS IN UNEXPECTED TEXTS FROM\n" +
            "UNKNOWN NUMBERS, EVEN IF THEY CLAIM TO BE\n" +
            "FROM YOUR BANK OR A DELIVERY SERVICE.\n\n" +
            "CALL THE COMPANY DIRECTLY TO VERIFY.",

            "PHISHING — TIP 6\n\n" +
            "SPEAR PHISHING TARGETS YOU SPECIFICALLY.\n" +
            "ATTACKERS RESEARCH YOUR NAME, JOB, AND\n" +
            "COLLEAGUES TO CRAFT CONVINCING MESSAGES.\n\n" +
            "ALWAYS VERIFY UNUSUAL REQUESTS — EVEN\n" +
            "IF THEY APPEAR TO COME FROM YOUR BOSS."
        };

        // SAFE BROWSING — 5 responses
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
            "DEVELOPER'S WEBSITE OR A TRUSTED STORE.",

            "SAFE BROWSING — TIP 5\n\n" +
            "CLEAR YOUR BROWSER COOKIES AND CACHE\n" +
            "REGULARLY TO REDUCE TRACKING.\n\n" +
            "USE BROWSER EXTENSIONS LIKE PRIVACY BADGER\n" +
            "TO BLOCK INVISIBLE TRACKERS THAT FOLLOW\n" +
            "YOU ACROSS DIFFERENT WEBSITES."
        };

        // SCAMS — 5 responses
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
            "REPORT THEM TO YOUR LOCAL AUTHORITIES.",

            "ONLINE SCAMS — TIP 4\n\n" +
            "GIFT CARD SCAMS ARE A FAVOURITE TACTIC.\n" +
            "NO LEGITIMATE PERSON OR ORGANISATION\n" +
            "WILL EVER ASK YOU TO PAY WITH GIFT CARDS.\n\n" +
            "IF SOMEONE ASKS FOR ITUNES OR GOOGLE PLAY\n" +
            "CARDS AS PAYMENT — IT IS A SCAM.",

            "ONLINE SCAMS — TIP 5\n\n" +
            "INVESTMENT SCAMS PROMISE HIGH RETURNS\n" +
            "WITH LITTLE OR NO RISK.\n\n" +
            "ALWAYS VERIFY THAT ANY INVESTMENT PLATFORM\n" +
            "IS REGISTERED WITH YOUR COUNTRY'S FINANCIAL\n" +
            "REGULATORY AUTHORITY BEFORE INVESTING."
        };

        // PRIVACY — 5 responses
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
            "OF GOOGLE TO REDUCE DATA TRACKING.",

            "PRIVACY — TIP 4\n\n" +
            "READ THE PRIVACY POLICY BEFORE SIGNING\n" +
            "UP FOR ANY NEW APP OR SERVICE.\n\n" +
            "PAY ATTENTION TO WHAT DATA IS COLLECTED,\n" +
            "HOW IT IS USED, AND WHETHER IT IS SOLD\n" +
            "TO THIRD PARTIES.",

            "PRIVACY — TIP 5\n\n" +
            "USE A SEPARATE EMAIL ADDRESS FOR ONLINE\n" +
            "SHOPPING AND NEWSLETTERS.\n\n" +
            "THIS LIMITS SPAM AND KEEPS YOUR PRIMARY\n" +
            "EMAIL CLEANER AND LESS EXPOSED TO\n" +
            "DATA BREACHES FROM RETAIL SITES."
        };

        // MALWARE — 5 responses
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
            "ENABLE AUTOMATIC UPDATES WHERE POSSIBLE.",

            "MALWARE PROTECTION — TIP 4\n\n" +
            "RANSOMWARE ENCRYPTS YOUR FILES AND DEMANDS\n" +
            "PAYMENT TO RESTORE THEM.\n\n" +
            "PROTECT YOURSELF BY BACKING UP YOUR DATA\n" +
            "REGULARLY TO AN EXTERNAL DRIVE OR CLOUD\n" +
            "STORAGE THAT IS NOT ALWAYS CONNECTED.",

            "MALWARE PROTECTION — TIP 5\n\n" +
            "AVOID PIRATED SOFTWARE AND MEDIA.\n" +
            "CRACKED PROGRAMS ARE A LEADING DELIVERY\n" +
            "MECHANISM FOR TROJANS AND SPYWARE.\n\n" +
            "THE MONEY SAVED IS NOT WORTH THE RISK\n" +
            "OF HAVING YOUR SYSTEM COMPROMISED."
        };

        // ══════════════════════════════════════════════════════════════════════════
        //  Public entry point
        // ══════════════════════════════════════════════════════════════════════════

        /* Processes the user's raw input string and returns the bot's response.
         * Order of checks:
         *   1. Sentiment prefix injection (uses string[] arrays + ContainsAny helper)
         *   2. Follow-up / conversation-flow phrases
         *   3. Cybersecurity keyword matching
         *   4. General queries (greetings, time, date, bot name, purpose)
         *   5. Default / error fallback
         */
        public static string GetResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "PLEASE TYPE A MESSAGE SO I CAN HELP YOU.";

            string lower = input.ToLower().Trim();

            // 1. Detect sentiment
            string sentimentPrefix = BuildSentimentPrefix(lower);
            bool hasSentiment = !string.IsNullOrEmpty(sentimentPrefix);

            // 2. Follow-up phrases — re-trigger last topic
            if (IsFollowUp(lower))
            {
                if (!string.IsNullOrEmpty(LastTopic))
                    return sentimentPrefix + PickTopicResponse(LastTopic);
                else
                    return "I DON'T HAVE A PREVIOUS TOPIC TO EXPAND ON.\n" +
                           "TRY ASKING ABOUT PASSWORD SAFETY, PHISHING,\n" +
                           "SCAMS, PRIVACY, MALWARE, OR SAFE BROWSING.";
            }

            // ── REQUIREMENT 5 FIX ────────────────────────────────────────────────
            // Detect "I'm interested in X" as a STANDALONE statement.
            // Respond with acknowledgement immediately, then append a tip on that topic.
            // This fires BEFORE the general keyword checks so the memory is saved
            // and acknowledged even if the topic word is also a keyword.
            if (lower.Contains("interested in") || lower.Contains("i care about") ||
                lower.Contains("i want to learn about"))
            {
                string[] topics = { "password", "phishing", "scam", "privacy", "malware", "browsing" };
                foreach (string topic in topics)
                {
                    if (lower.Contains(topic))
                    {
                        Memory.Set("interest", topic);
                        Memory.LogTopic(topic);
                        LastTopic = topic;

                        // Acknowledgement + immediate tip — no second input required
                        string ack =
                            $"GREAT! I'LL REMEMBER THAT YOU'RE INTERESTED IN {topic.ToUpper()}.\n" +
                            "IT'S A CRUCIAL PART OF STAYING SAFE ONLINE.\n\n" +
                            "HERE'S YOUR FIRST TIP ON THAT TOPIC:\n\n";

                        return sentimentPrefix + ack + PickTopicResponse(topic);
                    }
                }
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
                return sentimentPrefix + PersonaliseResponse(PickRandom(_privacyResponses));
            }
            if (lower.Contains("malware") || lower.Contains("virus") ||
                lower.Contains("antivirus") || lower.Contains("ransomware") || lower.Contains("trojan"))
            {
                LastTopic = "malware";
                Memory.LogTopic("malware");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_malwareResponses));
            }
            if (lower.Contains("safe browsing") || lower.Contains("browsing") ||
                lower.Contains("vpn") || lower.Contains("https") || lower.Contains("browser"))
            {
                LastTopic = "safe browsing";
                Memory.LogTopic("safe browsing");
                return sentimentPrefix + PersonaliseResponse(PickRandom(_safeBrowsingResponses));
            }

            // ── REQUIREMENT 6 FIX ────────────────────────────────────────────────
            // Sentiment detected but NO cybersecurity keyword present.
            // Proactively share a tip so the user does NOT have to type again.
            // Example: "I'm worried" → empathy prefix + scam tip (most relatable to worry).
            // Example: "I'm curious"  → empathy prefix + random tip from any topic.
            if (hasSentiment)
            {
                return sentimentPrefix + ProactiveTipForSentiment(lower);
            }

            // 4. General / conversational queries
            return HandleGeneral(lower);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Sentiment detection — uses string[] arrays + ContainsAny()
        // ══════════════════════════════════════════════════════════════════════════

        /* Iterates over each keyword array to detect the user's emotional tone
         * and returns an empathetic opening line prepended to the main response.
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
                return "GREAT CURIOSITY! HERE'S WHAT YOU NEED:\n\n";

            if (ContainsAny(lower, _happyKeywords))
                return "LOVE THE POSITIVE ENERGY! LET'S KEEP YOU\n" +
                       "SAFE AND SECURE ONLINE.\n\n";

            return ""; // Neutral — no prefix needed
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Conversation flow helpers
        // ══════════════════════════════════════════════════════════════════════════

        private static bool IsFollowUp(string lower)
        {
            return ContainsAny(lower, _followUpKeywords);
        }

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
        //  Includes: time, date, bot name, greetings, purpose, capability, memory
        // ══════════════════════════════════════════════════════════════════════════

        private static string HandleGeneral(string lower)
        {
            // ── Greetings ─────────────────────────────────────────────────────────
            if (lower.Contains("hello") || lower.Contains("hi") ||
                lower.Contains("hey") || lower.Contains("howdy") || lower.Contains("greetings"))
                return $"HELLO, {Visuals_UI.UserName}! HOW CAN I HELP YOU TODAY?\n" +
                       "TYPE 'help' TO SEE WHAT I CAN DO.";

            // ── How are you ───────────────────────────────────────────────────────
            if (lower.Contains("how are you") || lower.Contains("how r you") ||
                lower.Contains("are you okay") || lower.Contains("you good"))
                return "I'M JUST A PROGRAM, BUT I'M RUNNING PERFECTLY!\n" +
                       $"THANKS FOR ASKING, {Visuals_UI.UserName}!";

            // ── What is the time ──────────────────────────────────────────────────
            if ((lower.Contains("what") && lower.Contains("time")) ||
                lower.Contains("current time") || lower.Contains("what time"))
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                return $"THE CURRENT TIME IS: {time}\n\n" +
                       "REMEMBER — CYBERCRIMINALS OPERATE AT ALL HOURS.\n" +
                       "STAY ALERT ONLINE AT ANY TIME OF DAY!";
            }

            // ── What is the date ──────────────────────────────────────────────────
            if ((lower.Contains("what") && lower.Contains("date")) ||
                lower.Contains("today's date") || lower.Contains("what day"))
            {
                string date = DateTime.Now.ToString("dddd, dd MMMM yyyy");
                return $"TODAY IS: {date}\n\n" +
                       "A GOOD DAY TO REVIEW YOUR SECURITY SETTINGS\n" +
                       "AND MAKE SURE YOUR PASSWORDS ARE UP TO DATE!";
            }

            // ── What is your name / who are you ──────────────────────────────────
            if ((lower.Contains("what") && lower.Contains("your name")) ||
                lower.Contains("who are you") || lower.Contains("what are you called") ||
                lower.Contains("your name"))
                return "MY NAME IS CYBERBOT!\n\n" +
                       "I'M YOUR CYBERSECURITY AWARENESS CHAT BOT,\n" +
                       "DESIGNED TO HELP YOU STAY SAFE ONLINE.\n\n" +
                       "TYPE 'help' TO SEE EVERYTHING I CAN HELP WITH.";

            // ── What can you do / capabilities ───────────────────────────────────
            if (lower.Contains("what can you do") || lower.Contains("what do you do") ||
                lower.Contains("capabilities") || lower.Contains("your features"))
                return "I CAN ANSWER QUESTIONS ON:\n" +
                       "   PASSWORD SAFETY\n" +
                       "   PHISHING ATTACKS\n" +
                       "   ONLINE SCAMS & FRAUD\n" +
                       "   PRIVACY PROTECTION\n" +
                       "   MALWARE & VIRUSES\n" +
                       "   SAFE BROWSING HABITS\n\n" +
                       "I CAN ALSO TELL YOU THE CURRENT TIME AND DATE,\n" +
                       "AND REMEMBER YOUR NAME AND INTERESTS!\n\n" +
                       "TYPE 'help' FOR THE FULL COMMAND LIST.";

            // ── Purpose ───────────────────────────────────────────────────────────
            if (lower.Contains("purpose") || lower.Contains("why were you made") ||
                lower.Contains("why do you exist"))
                return "MY PURPOSE IS TO HELP YOU LEARN ABOUT\n" +
                       "CYBERSECURITY AWARENESS AND BEST PRACTICES\n" +
                       $"TO STAY SAFE ONLINE, {Visuals_UI.UserName}!\n\n" +
                       "ASK ME ANYTHING ABOUT PASSWORDS, PHISHING,\n" +
                       "SCAMS, PRIVACY, MALWARE, OR SAFE BROWSING.";

            // ── Thank you ─────────────────────────────────────────────────────────
            if (lower.Contains("thank you") || lower.Contains("thanks") || lower.Contains("thx"))
                return "THANK_YOU_EXIT"; // Sentinel — ChatWindow handles the exit flow

            // ── User sets their name mid-conversation ─────────────────────────────
            if (lower.Contains("my name is") || lower.Contains("call me") || lower.Contains("i'm "))
            {
                string potentialName = ExtractNameFromInput(lower);
                if (!string.IsNullOrEmpty(potentialName))
                {
                    Memory.Set("name", potentialName.ToUpper());
                    Visuals_UI.UserName = potentialName.ToUpper();
                    return $"NOTED! I'LL NOW CALL YOU {potentialName.ToUpper()}.\n" +
                           "NICE TO MEET YOU (AGAIN)!";
                }
            }

            // ── User states an interest ───────────────────────────────────────────
            if (lower.Contains("interested in") || lower.Contains("i care about") ||
                lower.Contains("i want to learn about"))
            {
                string[] topics = { "password", "phishing", "scam", "privacy", "malware", "browsing" };
                foreach (string topic in topics)
                {
                    if (lower.Contains(topic))
                    {
                        Memory.Set("interest", topic);
                        return $"GREAT! I'LL REMEMBER THAT YOU'RE INTERESTED IN {topic.ToUpper()}.\n" +
                               "IT'S A CRUCIAL PART OF STAYING SAFE ONLINE.\n\n" +
                               "LATER IN OUR CONVERSATION I'LL RELATE TIPS\n" +
                               "BACK TO YOUR INTEREST WHENEVER RELEVANT.";
                    }
                }
            }

            // ── Do you remember my name / what do you know about me ───────────────
            if (lower.Contains("do you remember") || lower.Contains("what do you know about me") ||
                lower.Contains("my details"))
            {
                string name = Memory.Get("name");
                string interest = Memory.Get("interest");
                string topicLog = Memory.TopicCount > 0
                    ? string.Join(", ", Memory.GetTopicHistory()).ToUpper()
                    : "NONE YET";

                return "HERE IS WHAT I REMEMBER ABOUT YOU:\n\n" +
                       $"   NAME:      {(string.IsNullOrEmpty(name) ? "NOT SET" : name)}\n" +
                       $"   INTEREST:  {(string.IsNullOrEmpty(interest) ? "NOT SET" : interest.ToUpper())}\n" +
                       $"   TOPICS DISCUSSED: {topicLog}";
            }

            // ── Default error-handling response — Part 2 requirement 7 ─────────────
            return "I'M NOT SURE I UNDERSTAND THAT.\n" +
                   "CAN YOU TRY REPHRASING YOUR QUESTION?\n\n" +
                   "TYPE 'help' TO SEE THE LIST OF TOPICS\n" +
                   "AND COMMANDS I CAN HELP YOU WITH.";
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Utility helpers
        // ══════════════════════════════════════════════════════════════════════════

        /* Iterates over a string[] array and returns true if the input contains
         * any of the keywords. Core array-usage method — called by sentiment
         * detection and follow-up checks.
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
         * Returns null if no name can be isolated.
         */
        private static string ExtractNameFromInput(string lower)
        {
            string[] markers = { "my name is ", "call me ", "i'm " };
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

        // ══════════════════════════════════════════════════════════════════════════
        //  Sentiment Helper
        // ══════════════════════════════════════════════════════════════════════════
        private static string ProactiveTipForSentiment(string lower)
        {
            // Worried / scared → scam tips are most directly reassuring
            if (ContainsAny(lower, _worriedKeywords))
            {
                LastTopic = "scam";
                Memory.LogTopic("scam");
                return "HERE IS SOMETHING THAT MIGHT HELP EASE YOUR CONCERN:\n\n" +
                       PickRandom(_scamResponses);
            }

            // Frustrated / confused → password tips are the most actionable
            if (ContainsAny(lower, _frustratedKeywords))
            {
                LastTopic = "password";
                Memory.LogTopic("password");
                return "LET'S START WITH SOMETHING PRACTICAL:\n\n" +
                       PickRandom(_passwordResponses);
            }

            // Curious / wants to learn → serve a random tip from any topic
            if (ContainsAny(lower, _curiousKeywords))
            {
                // Pick a random topic pool using an array of pool references
                List<string>[] allPools =
                {
                    _passwordResponses, _phishingResponses, _safeBrowsingResponses,
                    _scamResponses,     _privacyResponses,  _malwareResponses
                };
                string[] topicNames =
                {
                    "password", "phishing", "safe browsing",
                    "scam",     "privacy",  "malware"
                };
                int idx = _rng.Next(allPools.Length);
                LastTopic = topicNames[idx];
                Memory.LogTopic(topicNames[idx]);
                return $"HERE IS AN INTERESTING TIP ON {topicNames[idx].ToUpper()}:\n\n" +
                       PickRandom(allPools[idx]);
            }

            // Happy / excited → positive reinforcement + a random tip
            if (ContainsAny(lower, _happyKeywords))
            {
                LastTopic = "safe browsing";
                Memory.LogTopic("safe browsing");
                return "WHILE YOU'RE IN A GREAT MOOD, HERE'S A QUICK WIN:\n\n" +
                       PickRandom(_safeBrowsingResponses);
            }

            // Fallback — should not normally reach here
            return HandleGeneral(lower);
        }
    }
}

/* References:
 * Microsoft Corporation (2022) Visual Studio IntelliSense [Software]. Version 17.8.
 * Available at: https://visualstudio.microsoft.com/services/intellicode/ (Accessed: 11 March 2026).
 *
 * Anthropic (2026) Claude [AI assistant]. Available at: https://www.anthropic.com (Accessed: April 2026).
 * Prompt: 'How to store multiple predefined bot responses and pick one randomly in C#'.
 * Prompt: 'How to implement basic sentiment detection by keyword matching in C#'.
 */