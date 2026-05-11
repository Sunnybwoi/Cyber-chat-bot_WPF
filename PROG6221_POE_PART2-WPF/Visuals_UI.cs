using System;
using System.Speech.Synthesis;
using System.Threading;

namespace PROG6221_POE_PART2_WPF
{
    /* Visuals_UI provides shared application-wide state (UserName, Synthesizer) and
     * audio helpers consumed by both ChatWindow and Cyber_Quiz.
     * All console-specific rendering from Part 1 has been removed; the GUI handles
     * visual output directly via ChatWindow.AppendMessage().
     * Code completion assisted by Visual Studio's IntelliCode (Microsoft Corporation, 2022). Version 17.8.
     */
    public static class Visuals_UI
    {
        // ── Automatic properties ──────────────────────────────────────────────────

        /// <summary>Shared speech synthesizer — one instance for the app lifetime.</summary>
        public static SpeechSynthesizer Synthesizer { get; } = new SpeechSynthesizer();

        /// <summary>The current user's display name, set after the greeting prompt.</summary>
        public static string UserName { get; set; } = "USER";

        /// <summary>Whether voice / TTS is currently enabled.</summary>
        public static bool VoiceEnabled { get; set; } = true;

        // ── Static constructor: configure synthesizer defaults once ──────────────
        static Visuals_UI()
        {
            Synthesizer.SelectVoiceByHints(VoiceGender.Male);
            Synthesizer.Volume = 100;
            Synthesizer.Rate = 0;   // Natural reading pace
        }

        // ── Audio helpers ────────────────────────────────────────────────────────

        /* Speaks the supplied text asynchronously on a background thread.
         * Any currently-playing speech is cancelled first to avoid overlap.
         * Generated with assistance from Anthropic (2026) Claude [AI assistant].
         * Prompt: 'How to achieve synchronisation between text output and TTS in C#', March 2026.
         */
        public static void SpeakAsync(string text)
        {
            if (!VoiceEnabled) return;

            Thread speechThread = new Thread(() =>
            {
                try
                {
                    Synthesizer.SpeakAsyncCancelAll();
                    Synthesizer.Speak(text.Replace("\n", " "));
                }
                catch (OperationCanceledException)
                {
                    // Intentional cancellation from SpeakAsyncCancelAll() — safe to ignore
                }
                catch (Exception)
                {
                    // Any other audio failure is non-critical — app continues normally
                }
            });
            speechThread.IsBackground = true;
            speechThread.Start();
        }

        /* Speaks synchronously — blocks the calling thread until speech finishes.
         * Useful for short one-off greetings where order matters.
         */
        public static void SpeakSync(string text)
        {
            if (!VoiceEnabled) return;
            try
            {
                Synthesizer.SpeakAsyncCancelAll();
                Synthesizer.Speak(text.Replace("\n", " "));
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        /// <summary>Stops any currently playing speech immediately.</summary>
        public static void StopSpeech()
        {
            try { Synthesizer.SpeakAsyncCancelAll(); }
            catch (Exception) { }
        }

        // ── ASCII art ────────────────────────────────────────────────────────────

        /* Returns the ASCII banner from Part 1 as a multi-line string.
         * ChatWindow renders this inside a styled TextBlock with a monospace font.
         */
        public static string GetASCIIBanner()
        {
            return
                @"   _____   _____ ___ ___     ___ ___ ___ _   _ ___ ___ _______   __" + "\n" +
                @"  / __\ \ / / _ ) __| _ \___/ __| __/ __| | | | _ \_ _|_   _\ \ / /" + "\n" +
                @" | (__ \ V /| _ \ _||   /___\__ \ _| (__| |_| |   /| |  | |  \ V / " + "\n" +
                @"  \___| |_| |___/___|_|_\   |___/___\___|\___/|_|_\___| |_|   |_|  " + "\n" +
                "\n" +
                @"               ___ _  _   _ _____   ___  ___ _____                 " + "\n" +
                @"              / __| || | /_\_   _| | _ )/ _ \_   _|                " + "\n" +
                @"             | (_ | __ |/ _ \| |   | _ \ (_) || |                  " + "\n" +
                @"              \___|_||_/_/ \_\_|   |___/\___/ |_|                  ";
        }

        // ── Help menu ────────────────────────────────────────────────────────────

        public static string GetHelpMenuText()
        {
            return
                "╔══════════════════════════════════════════════════════════╗\n" +
                "║           HELP MENU  —  AVAILABLE TOPICS                 ║\n" +
                "╠══════════════════════════════════════════════════════════╣\n" +
                "║  CYBERSECURITY TOPICS:                                   ║\n" +
                "║    password      — Password safety tips                  ║\n" +
                "║    phishing      — How to spot phishing attacks          ║\n" +
                "║    safe browsing — Safe internet habits                  ║\n" +
                "║    scam          — How to avoid online scams             ║\n" +
                "║    privacy       — Protecting your personal data         ║\n" +
                "║    malware       — Malware and virus protection          ║\n" +
                "╠══════════════════════════════════════════════════════════╣\n" +
                "║  GENERAL:                                                ║\n" +
                "║    how are you / what can you do / your purpose          ║\n" +
                "║    what is your name / what is the time / what is date   ║\n" +
                "║    tell me more / another tip / explain more             ║\n" +
                "╠══════════════════════════════════════════════════════════╣\n" +
                "║  COMMANDS:                                               ║\n" +
                "║    help / menu  — Show this menu                         ║\n" +
                "║    clear        — Clear chat history                     ║\n" +
                "║    exit / quit  — Close the application                  ║\n" +
                "╚══════════════════════════════════════════════════════════╝";
        }
    }
}

/* References:
 * Microsoft Corporation (2022) Visual Studio IntelliSense [Software]. Version 17.8.
 * Available at: https://visualstudio.microsoft.com/services/intellicode/ (Accessed: 11 March 2026).
 *
 * Anthropic (2026) Claude [AI assistant]. Available at: https://www.anthropic.com (Accessed: 12 March 2026).
 * Prompt: 'How to achieve synchronisation between text output and TTS in C#'.
 */