using PROG6221_POE_PART2_WPF;
using System;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PROG6221_POE_PART2_WPF
{
    /* ChatWindow is the main WPF window for Part 2 of the Cybersecurity Awareness Chat Bot.
     * It handles:
     *   - Greeting flow: plays WAV, speaks "What is your name?", waits for typed name
     *   - Name validation: rejects numeric-only input and blank entries
     *   - Rendering chat messages with colour-coded bubbles per sender type
     *   - Routing user input to Cyber_Quiz.GetResponse()
     *   - Voice toggle (text-to-speech on/off)
     *   - Chat history clear and session reset
     *   - Exit / quit / thank-you flows
     *   - Live status bar and topic-history display
     *
     * All chatbot logic lives in Cyber_Quiz.cs; all UI rendering lives here.
     * This separation follows OOP principles required by Part 2.
     *
     * Generated with assistance from Anthropic (2026) Claude [AI assistant].
     * Prompt: 'How to build a WPF chat window with colour-coded message bubbles in C#', April 2026.
     */
    public partial class ChatWindow : Window
    {
        // ── State ─────────────────────────────────────────────────────────────────
        private bool _nameEntered = false;   // True once the user has provided a valid name
        private bool _isBotTyping = false;   // Prevents double-sends while bot is responding

        // ── Colour constants ──────────────────────────────────────────────────────
        private static readonly SolidColorBrush BotColour = new SolidColorBrush(Color.FromRgb(0, 229, 255));
        private static readonly SolidColorBrush UserColour = new SolidColorBrush(Color.FromRgb(255, 214, 0));
        private static readonly SolidColorBrush SystemColour = new SolidColorBrush(Color.FromRgb(0, 255, 65));
        private static readonly SolidColorBrush ErrorColour = new SolidColorBrush(Color.FromRgb(255, 61, 61));
        private static readonly SolidColorBrush MutedColour = new SolidColorBrush(Color.FromRgb(42, 90, 42));

        // ══════════════════════════════════════════════════════════════════════════
        //  Initialisation
        // ══════════════════════════════════════════════════════════════════════════

        public ChatWindow()
        {
            InitializeComponent();
            Loaded += ChatWindow_Loaded;
        }

        /* Runs after all XAML elements are initialised.
         * 1. Displays the ASCII banner.
         * 2. Plays the greeting WAV on a background thread.
         * 3. After the WAV finishes, speaks "What is your name?" via TTS.
         * 4. Displays the typed greeting message in the chat panel.
         *
         * Fix applied: the "what is your name" prompt is now spoken AFTER the WAV
         * greeting finishes, not simultaneously with it.
         */
        private void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtBanner.Text = Visuals_UI.GetASCIIBanner();

            // Show the initial bot message immediately in the chat
            AppendBotMessage(
                "HI THERE! IT'S YOUR CYBERSECURITY AWARENESS CHAT BOT.\n" +
                "HERE TO HELP YOU STAY SAFE ONLINE!\n\n" +
                "WHAT IS YOUR NAME?");

            SetStatus("WAITING FOR USER NAME...");
            TxtInput.Focus();

            // Play WAV greeting, then speak the name prompt — all on a background thread
            // so the UI never freezes. The name prompt fires only after the WAV finishes.
            // Generated with assistance from Anthropic (2026) Claude [AI assistant].
            // Prompt: 'How to play a wav file then run TTS sequentially on a background thread in WPF', April 2026.
            Thread greetingThread = new Thread(() =>
            {
                // Step 1: Play WAV (blocks this background thread until done)
                string wavPath = "Greeting.wav";
                if (File.Exists(wavPath))
                {
                    try
                    {
                        SoundPlayer player = new SoundPlayer(wavPath);
                        player.PlaySync();
                    }
                    catch (Exception) { /* Non-critical — continue */ }
                }

                // Step 2: Speak the name prompt AFTER the WAV has finished
                if (Visuals_UI.VoiceEnabled)
                    Visuals_UI.SpeakSync(
                        "What is your name?");
            });
            greetingThread.IsBackground = true;
            greetingThread.Start();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Input handling
        // ══════════════════════════════════════════════════════════════════════════

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && BtnSend.IsEnabled)
            {
                e.Handled = true;
                ProcessInput();
            }
            else if (e.Key == Key.Escape)
            {
                TxtInput.Clear();
            }
        }

        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            BtnSend.IsEnabled = !string.IsNullOrWhiteSpace(TxtInput.Text) && !_isBotTyping;
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            ProcessInput();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Core input processing pipeline
        // ══════════════════════════════════════════════════════════════════════════

        private void ProcessInput()
        {
            string raw = TxtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            TxtInput.Clear();
            BtnSend.IsEnabled = false;

            // ── Phase 1: Name capture and validation ──────────────────────────────
            if (!_nameEntered)
            {
                HandleNameInput(raw);
                return;
            }

            // Show the user's message in the chat
            AppendUserMessage(raw);

            string lower = raw.ToLower().Trim();

            // ── Phase 2: System commands ──────────────────────────────────────────
            if (lower == "exit" || lower == "quit" || lower == "bye")
            {
                HandleExitCommand();
                return;
            }
            if (lower == "help" || lower == "menu")
            {
                AppendSystemMessage(Visuals_UI.GetHelpMenuText());
                if (Visuals_UI.VoiceEnabled) Visuals_UI.SpeakAsync("Here is the help menu.");
                SetStatus("HELP MENU DISPLAYED.");
                return;
            }
            if (lower == "clear")
            {
                BtnClearChat_Click(null, null);
                return;
            }

            // ── Phase 3: Route to Cyber_Quiz ──────────────────────────────────────
            _isBotTyping = true;
            SetStatus("BOT IS THINKING...");

            Thread workerThread = new Thread(() =>
            {
                string response = Cyber_Quiz.GetResponse(raw);

                Dispatcher.Invoke(() =>
                {
                    if (response == "THANK_YOU_EXIT")
                    {
                        AppendBotMessage(
                            $"YOU'RE WELCOME, {Visuals_UI.UserName}! STAY SAFE ONLINE!\n" +
                            "THE SESSION WILL NOW END. GOODBYE!");
                        if (Visuals_UI.VoiceEnabled)
                            Visuals_UI.SpeakSync(
                                $"You're welcome, {Visuals_UI.UserName}. Stay safe online. Goodbye!");
                        SetStatus("SESSION ENDED.");
                        Task.Delay(2000).ContinueWith(_ =>
                            Dispatcher.Invoke(() => Application.Current.Shutdown()));
                        return;
                    }

                    AppendBotMessage(response);
                    if (Visuals_UI.VoiceEnabled) Visuals_UI.SpeakAsync(response);
                    UpdateTopicHistoryBar(); 
                    string lastTopic = string.IsNullOrEmpty(Cyber_Quiz.LastTopic) ? "NONE" : Cyber_Quiz.LastTopic.ToUpper();
                    SetStatus($"LAST TOPIC: {lastTopic}");
                    _isBotTyping = false;
                    BtnSend.IsEnabled = !string.IsNullOrWhiteSpace(TxtInput.Text);
                    TxtInput.Focus();
                });
            });
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Name validation
        //  Rules:
        //    - Must not be blank or whitespace only
        //    - Must not be a pure number (e.g. "123", "42")
        //    - Must not contain only digits and symbols
        //    - Must contain at least one letter character
        //
        //  Fix: after validation passes, the bot speaks the welcome message.
        // ══════════════════════════════════════════════════════════════════════════

        /* Validates and stores the user's name.
         * Rejects entries that are purely numeric or contain no alphabetic characters.
         * Part 2 requirement: validate input types appropriately.
         * Generated with assistance from Anthropic (2026) Claude [AI assistant].
         * Prompt: 'How to validate that a string contains at least one letter in C#', April 2026.
         */
        private void HandleNameInput(string raw)
        {
            // Validation 1: empty or whitespace (already prevented by BtnSend.IsEnabled, but double-check)
            if (string.IsNullOrWhiteSpace(raw))
            {
                AppendErrorMessage("PLEASE ENTER YOUR NAME TO CONTINUE.");
                if (Visuals_UI.VoiceEnabled)
                    Visuals_UI.SpeakAsync("Please enter your name to continue.");
                return;
            }

            // Validation 2: must contain at least one letter — rejects pure numbers like "123" or "42"
            if (!Regex.IsMatch(raw, @"[a-zA-Z]"))
            {
                AppendErrorMessage(
                    "INVALID NAME: PLEASE ENTER A NAME CONTAINING LETTERS.\n" +
                    "NUMBERS AND SYMBOLS ALONE ARE NOT ACCEPTED AS A NAME.\n\n" +
                    "EXAMPLE: 'Alice', 'John', 'CyberUser'");
                if (Visuals_UI.VoiceEnabled)
                    Visuals_UI.SpeakAsync(
                        "Invalid name. Please enter a name that contains letters.");
                TxtInput.Focus();
                return;
            }

            // Validation 3: reasonable length — at least 2 characters
            if (raw.Trim().Length < 2)
            {
                AppendErrorMessage(
                    "THAT NAME IS TOO SHORT.\n" +
                    "PLEASE ENTER AT LEAST 2 CHARACTERS.");
                if (Visuals_UI.VoiceEnabled)
                    Visuals_UI.SpeakAsync("That name is too short. Please enter at least 2 characters.");
                TxtInput.Focus();
                return;
            }

            // ── Name is valid — store and proceed ──────────────────────────────────
            // Extract only the first word as the display name (e.g. "Alice Smith" → "ALICE")
            string firstName = raw.Trim().Split(' ')[0];
            firstName = char.ToUpper(firstName[0]) + firstName.Substring(1).ToLower();

            Visuals_UI.UserName = firstName.ToUpper();
            Memory.Set("name", firstName.ToUpper());
            _nameEntered = true;

            // Update the input prompt label
            TxtPromptLabel.Text = $"[ {Visuals_UI.UserName} ] ▶ ";

            // Show the user's input in chat
            AppendUserMessage(raw);

            string welcome =
                $"HELLO, {Visuals_UI.UserName}!\n" +
                "WELCOME TO THE CYBERSECURITY AWARENESS CHAT BOT!\n\n" +
                "I'M HERE TO HELP YOU STAY SAFE ONLINE.\n" +
                "TYPE 'help' OR 'menu' TO SEE WHAT YOU CAN ASK ME.";

            AppendBotMessage(welcome);
            AppendSystemMessage(Visuals_UI.GetHelpMenuText());
            SetStatus($"SESSION ACTIVE  ●  USER: {Visuals_UI.UserName}");

            // Speak the welcome message AFTER validation — fix for sequential audio
            if (Visuals_UI.VoiceEnabled)
                Visuals_UI.SpeakAsync($"Hello, {firstName}! Welcome to the Cybersecurity Awareness Chat Bot!");

            TxtInput.Focus();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Exit flow
        // ══════════════════════════════════════════════════════════════════════════

        private void HandleExitCommand()
        {
            AppendBotMessage(
                $"THANK YOU FOR USING THE CYBERSECURITY AWARENESS CHAT BOT, {Visuals_UI.UserName}!\n" +
                "STAY SAFE ONLINE! GOODBYE!");

            if (Visuals_UI.VoiceEnabled)
                Visuals_UI.SpeakSync(
                    $"Thank you for using the Cybersecurity Awareness Chat Bot, {Visuals_UI.UserName}. " +
                    "Stay safe online. Goodbye!");

            SetStatus("SHUTTING DOWN...");
            Task.Delay(1500).ContinueWith(_ =>
                Dispatcher.Invoke(() => Application.Current.Shutdown()));
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Message rendering helpers
        // ══════════════════════════════════════════════════════════════════════════

        private void AppendBotMessage(string text)
        {
            AppendMessage("BOT", text,
                BotColour,
                new SolidColorBrush(Color.FromRgb(0, 20, 30)),
                new SolidColorBrush(Color.FromRgb(0, 40, 55)),
                HorizontalAlignment.Left);
            ScrollToBottom();
        }

        private void AppendUserMessage(string text)
        {
            string label = _nameEntered ? Visuals_UI.UserName : "USER";
            AppendMessage(label, text,
                UserColour,
                new SolidColorBrush(Color.FromRgb(30, 25, 0)),
                new SolidColorBrush(Color.FromRgb(50, 40, 0)),
                HorizontalAlignment.Right);
            ScrollToBottom();
        }

        private void AppendSystemMessage(string text)
        {
            AppendMessage("SYSTEM", text,
                SystemColour,
                new SolidColorBrush(Color.FromRgb(0, 15, 0)),
                new SolidColorBrush(Color.FromRgb(0, 30, 0)),
                HorizontalAlignment.Stretch);
            ScrollToBottom();
        }

        private void AppendErrorMessage(string text)
        {
            AppendMessage("ERROR", text,
                ErrorColour,
                new SolidColorBrush(Color.FromRgb(30, 0, 0)),
                new SolidColorBrush(Color.FromRgb(60, 0, 0)),
                HorizontalAlignment.Left);
            ScrollToBottom();
        }

        /* Core rendering method — builds a styled message bubble and adds it to ChatPanel.
         * Generated with assistance from Anthropic (2026) Claude [AI assistant].
         * Prompt: 'How to dynamically add styled TextBlock elements to a WPF StackPanel in C#', April 2026.
         */
        private void AppendMessage(string senderLabel, string text,
                                   SolidColorBrush textColor,
                                   SolidColorBrush bgColor,
                                   SolidColorBrush borderColor,
                                   HorizontalAlignment alignment)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            Border bubble = new Border
            {
                Background = bgColor,
                BorderBrush = borderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(4, 3, 4, 3),
                HorizontalAlignment = alignment,
                MaxWidth = 720
            };

            StackPanel inner = new StackPanel();

            // Header: sender label + timestamp
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lblSender = new TextBlock
            {
                Text = $"[ {senderLabel} ]",
                Foreground = textColor,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Opacity = 0.7
            };
            Grid.SetColumn(lblSender, 0);

            TextBlock lblTime = new TextBlock
            {
                Text = timestamp,
                Foreground = MutedColour,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(lblTime, 1);

            header.Children.Add(lblSender);
            header.Children.Add(lblTime);
            inner.Children.Add(header);

            // Divider line
            inner.Children.Add(new Border
            {
                BorderBrush = borderColor,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 4, 0, 6)
            });

            // Message body
            TextBlock body = new TextBlock
            {
                Text = text,
                Foreground = textColor,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            };
            inner.Children.Add(body);

            bubble.Child = inner;

            // Fade-in animation
            bubble.Opacity = 0;
            ChatPanel.Children.Add(bubble);
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)));
            bubble.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Button event handlers
        // ══════════════════════════════════════════════════════════════════════════

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
            Memory.Clear();
            AppendSystemMessage("CHAT HISTORY CLEARED.  NEW SESSION STARTED.");
            SetStatus("CHAT CLEARED  ●  MEMORY RESET.");
            TxtTopicHistory.Text = "";
            TxtInput.Focus();
        }

        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            Visuals_UI.VoiceEnabled = !Visuals_UI.VoiceEnabled;
            BtnMute.Content = Visuals_UI.VoiceEnabled ? "[ VOICE: ON ]" : "[ VOICE: OFF ]";
            if (!Visuals_UI.VoiceEnabled) Visuals_UI.StopSpeech();
            SetStatus(Visuals_UI.VoiceEnabled ? "VOICE ENABLED." : "VOICE DISABLED.");
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Status bar helpers
        // ══════════════════════════════════════════════════════════════════════════

        private void SetStatus(string message)
        {
            TxtStatus.Text = $"[ SYSTEM ]  ●  {message.ToUpper()}";
        }

        private void UpdateTopicHistoryBar()
        {
            var history = Memory.GetTopicHistory();
            if (history.Count > 0)
                TxtTopicHistory.Text = "TOPICS: " + string.Join("  ●  ", history).ToUpper();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Scroll helper
        // ══════════════════════════════════════════════════════════════════════════

        private void ScrollToBottom()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChatScrollViewer.ScrollToBottom();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}

/* References:
 * Microsoft Corporation (2022) Visual Studio IntelliSense [Software]. Version 17.8.
 * Available at: https://visualstudio.microsoft.com/services/intellicode/ (Accessed: 11 March 2026).
 *
 * Anthropic (2026) Claude [AI assistant]. Available at: https://www.anthropic.com (Accessed: April 2026).
 * Prompt: 'How to build a WPF chat window with colour-coded message bubbles in C#'.
 * Prompt: 'How to validate that a string contains at least one letter in C#'.
 * Prompt: 'How to play a wav file then run TTS sequentially on a background thread in WPF'.
 * Prompt: 'How to dynamically add styled TextBlock elements to a WPF StackPanel in C#'.
 */