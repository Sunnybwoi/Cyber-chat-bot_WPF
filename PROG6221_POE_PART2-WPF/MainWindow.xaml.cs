using System;
using System.Collections.Generic;
using System.Media;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;
using System.Threading.Tasks;

namespace PROG6221_POE_PART2_WPF
{
    /* ChatWindow is the main WPF window for Part 2 of the Cybersecurity Awareness Chat Bot.
     * It handles:
     *   - Greeting flow (name input, welcome speech)
     *   - Rendering chat messages with colour-coded bubbles per sender type
     *   - Routing user input to Cyber_Quiz.GetResponse()
     *   - Voice toggle (text-to-speech on/off)
     *   - Chat history clear
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
        private delegate void BotSpeechDelegate(string text);
        private delegate Task BotMessageRenderDelegate(string text);

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _nameEntered = false;   // True once the user has provided their name
        private bool _voiceEnabled = true;    // Tracks text-to-speech toggle state
        private bool _isBotTyping = false;   // Prevents double-sends while bot is responding
        private const int BaseTypingDelayMs = 18;
        private readonly BotSpeechDelegate _speakBotDelegate;
        private readonly BotMessageRenderDelegate _renderBotMessageDelegate;

        // ── Colour constants (match XAML brushes) ─────────────────────────────────
        private static readonly SolidColorBrush BotColour = new SolidColorBrush(Color.FromRgb(0, 229, 255)); // Cyan
        private static readonly SolidColorBrush UserColour = new SolidColorBrush(Color.FromRgb(255, 214, 0)); // Yellow
        private static readonly SolidColorBrush SystemColour = new SolidColorBrush(Color.FromRgb(0, 255, 65)); // Green
        private static readonly SolidColorBrush ErrorColour = new SolidColorBrush(Color.FromRgb(255, 61, 61)); // Red
        private static readonly SolidColorBrush MutedColour = new SolidColorBrush(Color.FromRgb(42, 90, 42)); // Muted green
        private static readonly SolidColorBrush BackgroundColour = new SolidColorBrush(Color.FromRgb(13, 13, 13));

        // ══════════════════════════════════════════════════════════════════════════
        //  Initialisation
        // ══════════════════════════════════════════════════════════════════════════

        public ChatWindow()
        {
            InitializeComponent();
            _speakBotDelegate = Visuals_UI.SpeakAsync;
            _renderBotMessageDelegate = AppendBotMessageWithTypingAsync;
            Loaded += ChatWindow_Loaded;
        }

        /* Runs after all XAML elements are initialised.
         * Displays the ASCII banner, plays the greeting WAV (if present),
         * then asks the user for their name.
         */
        private async void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Render ASCII banner
            TxtBanner.Text = Visuals_UI.GetASCIIBanner();

            // Play greeting WAV on a background thread — non-blocking
            string wavPath = "Greeting.wav";
            if (File.Exists(wavPath))
            {
                Thread audioThread = new Thread(() =>
                {
                    try
                    {
                        SoundPlayer player = new SoundPlayer(wavPath);
                        player.PlaySync();
                    }
                    catch { /* Audio failure is non-critical */ }
                });
                audioThread.IsBackground = true;
                audioThread.Start();
            }

            // Show initial bot greeting in the chat panel
            _isBotTyping = true;
            BtnSend.IsEnabled = false;
            string startupGreeting =
                "HI THERE!, ITS YOUR CYBERSECURITY AWARENESS CHATBOT.\n" +
                " HERE TO HELP YOU STAY SAFE ONLINE.\n" +
                " NEED HELP WITH ANYTHING, JUST ASK AWAY.\n\n" +
                "WHAT IS YOUR NAME?";
            await RenderBotOutputAsync(startupGreeting, "What is your name?");
            _isBotTyping = false;

            SetStatus("WAITING FOR USER NAME...");
            TxtInput.Focus();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Input handling
        // ══════════════════════════════════════════════════════════════════════════

        /* Fires when the user presses a key in the input box.
         * Enter submits the message; Escape clears the input field.
         */
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

        /* Enables / disables the Send button based on whether there is text in the box.
         * Prevents sending empty messages without showing an error dialog.
         */
        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            BtnSend.IsEnabled = !string.IsNullOrWhiteSpace(TxtInput.Text) && !_isBotTyping;
        }

        /* Called when the user clicks the SEND button. */
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            ProcessInput();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Core input processing pipeline
        // ══════════════════════════════════════════════════════════════════════════

        /* Central method: validates input, routes to the correct handler,
         * and updates the UI and status bar.
         * Generated with assistance from Anthropic (2026) Claude [AI assistant].
         * Prompt: 'How to route user input through a state machine in a WPF chatbot', April 2026.
         */
        private void ProcessInput()
        {
            string raw = TxtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            TxtInput.Clear();
            BtnSend.IsEnabled = false;

            // ── Phase 1: Name capture ─────────────────────────────────────────────
            if (!_nameEntered)
            {
                _ = HandleNameInputAsync(raw);
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
                if (_voiceEnabled) Visuals_UI.SpeakAsync("Here is the help menu.");
                SetStatus("HELP MENU DISPLAYED.");
                return;
            }
            if (lower == "clear")
            {
                ClearChatSession();
                return;
            }

            // ── Phase 3: Route to Cyber_Quiz ──────────────────────────────────────
            _isBotTyping = true;
            SetStatus("BOT IS THINKING...");

            // Run response generation on a background thread to keep UI responsive
            // then marshal the result back to the UI thread via Dispatcher.
            // Generated with assistance from Anthropic (2026) Claude [AI assistant].
            // Prompt: 'How to use Dispatcher.Invoke to update WPF UI from a background thread', April 2026.
            Thread workerThread = new Thread(() =>
            {
                string response = Cyber_Quiz.GetResponse(raw);

                _ = Dispatcher.InvokeAsync(async () => await HandleBotResponseAsync(response));
            });
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private async Task HandleBotResponseAsync(string response)
        {
            // Special sentinel: "thank you" exits after showing goodbye
            if (response == "THANK_YOU_EXIT")
            {
                string goodbyeMessage =
                    $"YOU'RE WELCOME, {Visuals_UI.UserName}! STAY SAFE ONLINE!\n" +
                    "THE SESSION WILL NOW END. GOODBYE!";
                await RenderBotOutputAsync(
                    goodbyeMessage,
                    $"You're welcome, {Visuals_UI.UserName}. Stay safe online. Goodbye!");
                SetStatus("SESSION ENDED.");
                await Task.Delay(2000);
                Application.Current.Shutdown();
                return;
            }

            await RenderBotOutputAsync(response, response);
            UpdateTopicHistoryBar();
            SetStatus($"LAST TOPIC: {GetLastTopicLabel()}");
            _isBotTyping = false;
            BtnSend.IsEnabled = !string.IsNullOrWhiteSpace(TxtInput.Text);
            TxtInput.Focus();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Greeting / name flow
        // ══════════════════════════════════════════════════════════════════════════

        private async Task HandleNameInputAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                AppendErrorMessage("PLEASE ENTER A VALID NAME TO CONTINUE.");
                if (_voiceEnabled) Visuals_UI.SpeakAsync("Please enter a valid name.");
                return;
            }

            // Store name in both Visuals_UI (for display prefix) and Memory (for personalisation)
            Visuals_UI.UserName = name.ToUpper();
            Memory.Set("name", name.ToUpper());
            _nameEntered = true;

            // Update the prompt label to show the user's name
            TxtPromptLabel.Text = $"[ {Visuals_UI.UserName} ] ▶ ";

            AppendUserMessage(name);
            _isBotTyping = true;
            BtnSend.IsEnabled = false;

            string welcome =
                $"HELLO, {Visuals_UI.UserName}!\n" +
                "WELCOME TO THE CYBERSECURITY AWARENESS CHAT BOT!\n\n" +
                "I'M HERE TO HELP YOU STAY SAFE ONLINE.\n" +
                "TYPE 'help' OR 'menu' TO SEE WHAT YOU CAN ASK ME.";

            await RenderBotOutputAsync(
                welcome,
                $"Hello, {name}! Welcome to the Cybersecurity Awareness Chat Bot! I'm here to help you stay safe online. Type 'help' or 'menu' to see what you can ask me.");

            // Show the help menu automatically after greeting, just like Part 1
            AppendSystemMessage(Visuals_UI.GetHelpMenuText());
            SetStatus($"SESSION ACTIVE  ●  USER: {Visuals_UI.UserName}");
            _isBotTyping = false;
            TxtInput.Focus();
            BtnSend.IsEnabled = !string.IsNullOrWhiteSpace(TxtInput.Text);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Exit flow
        // ══════════════════════════════════════════════════════════════════════════

        private void HandleExitCommand()
        {
            AppendBotMessage(
                $"THANK YOU FOR USING THE CYBERSECURITY AWARENESS CHAT BOT, {Visuals_UI.UserName}!\n" +
                "STAY SAFE ONLINE! GOODBYE!");

            if (_voiceEnabled)
                Visuals_UI.SpeakSync(
                    $"Thank you for using the Cybersecurity Awareness Chat Bot, {Visuals_UI.UserName}. Stay safe online. Goodbye!");

            SetStatus("SHUTTING DOWN...");
            System.Threading.Tasks.Task.Delay(1500)
                .ContinueWith(_ => Dispatcher.Invoke(() => Application.Current.Shutdown()));
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Message rendering helpers
        //  Each method appends a styled Border+TextBlock bubble to ChatPanel.
        // ══════════════════════════════════════════════════════════════════════════

        /* Appends a bot message bubble — cyan text, left-anchored.
         * Code completion assisted by Visual Studio's IntelliCode (Microsoft Corporation, 2022). Version 17.8.
         */
        private void AppendBotMessage(string text)
        {
            AppendMessage("BOT", text, BotColour, new SolidColorBrush(Color.FromRgb(0, 20, 30)),
                          new SolidColorBrush(Color.FromRgb(0, 40, 55)), HorizontalAlignment.Left);
            ScrollToBottom();
        }

        private async Task AppendBotMessageWithTypingAsync(string text)
        {
            TextBlock body = AppendMessage("BOT", string.Empty, BotColour, new SolidColorBrush(Color.FromRgb(0, 20, 30)),
                                           new SolidColorBrush(Color.FromRgb(0, 40, 55)), HorizontalAlignment.Left);
            await TypeTextAsync(body, text);
            ScrollToBottom();
        }

        private async Task RenderBotOutputAsync(string displayText, string speechText)
        {
            // Start TTS first, then immediately render typed text so both channels run together.
            if (_voiceEnabled && !string.IsNullOrWhiteSpace(speechText))
                _speakBotDelegate(speechText);

            await _renderBotMessageDelegate(displayText);
        }

        /* Appends a user message bubble — yellow text, right-anchored. */
        private void AppendUserMessage(string text)
        {
            string label = _nameEntered ? Visuals_UI.UserName : "USER";
            AppendMessage(label, text, UserColour, new SolidColorBrush(Color.FromRgb(30, 25, 0)),
                          new SolidColorBrush(Color.FromRgb(50, 40, 0)), HorizontalAlignment.Right);
            ScrollToBottom();
        }

        /* Appends a system/info message — green text, centred (used for help menu). */
        private void AppendSystemMessage(string text)
        {
            AppendMessage("SYSTEM", text, SystemColour, new SolidColorBrush(Color.FromRgb(0, 15, 0)),
                          new SolidColorBrush(Color.FromRgb(0, 30, 0)), HorizontalAlignment.Stretch);
            ScrollToBottom();
        }

        /* Appends a red error message bubble. */
        private void AppendErrorMessage(string text)
        {
            AppendMessage("ERROR", text, ErrorColour, new SolidColorBrush(Color.FromRgb(30, 0, 0)),
                          new SolidColorBrush(Color.FromRgb(60, 0, 0)), HorizontalAlignment.Left);
            ScrollToBottom();
        }

        /* Core rendering method — builds a styled message block and appends it to ChatPanel.
         * Generated with assistance from Anthropic (2026) Claude [AI assistant].
         * Prompt: 'How to dynamically add styled TextBlock elements to a WPF StackPanel in C#', April 2026.
         */
        private TextBlock AppendMessage(string senderLabel, string text,
                                        SolidColorBrush textColor,
                                        SolidColorBrush bgColor,
                                        SolidColorBrush borderColor,
                                        HorizontalAlignment alignment)
        {
            // Timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Outer container
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

            // Header row: sender label + timestamp
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

            // Thin divider line
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

            // Fade-in animation for new messages
            bubble.Opacity = 0;
            ChatPanel.Children.Add(bubble);

            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)));
            bubble.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            return body;
        }

        private async Task TypeTextAsync(TextBlock target, string fullText)
        {
            target.Text = string.Empty;
            foreach (char c in fullText)
            {
                target.Text += c;
                ScrollToBottom();
                await Task.Delay(GetDelayForCharacter(c));
            }
        }

        private static int GetDelayForCharacter(char c)
        {
            if (c == '.' || c == '!' || c == '?') return 110;
            if (c == ',' || c == ';' || c == ':') return 70;
            if (c == '\n') return 120;
            if (char.IsWhiteSpace(c)) return 8;
            return BaseTypingDelayMs;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Button event handlers
        // ══════════════════════════════════════════════════════════════════════════

        /* Clears the chat panel and resets Memory for a fresh session. */
        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            ClearChatSession();
        }

        private void ClearChatSession()
        {
            ChatPanel.Children.Clear();
            Memory.Clear();
            AppendSystemMessage("CHAT HISTORY CLEARED.  NEW SESSION STARTED.");
            SetStatus("CHAT CLEARED  ●  MEMORY RESET.");
            TxtTopicHistory.Text = "";
            TxtInput.Focus();
        }

        /* Toggles the text-to-speech voice on or off. */
        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            _voiceEnabled = !_voiceEnabled;
            BtnMute.Content = _voiceEnabled ? "[ VOICE: ON ]" : "[ VOICE: OFF ]";
            if (!_voiceEnabled) Visuals_UI.StopSpeech();
            SetStatus(_voiceEnabled ? "VOICE ENABLED." : "VOICE DISABLED.");
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

        private string GetLastTopicLabel()
        {
            return !string.IsNullOrEmpty(Cyber_Quiz.LastTopic)
                ? Cyber_Quiz.LastTopic.ToUpper()
                : "NONE";
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  Scroll helper
        // ══════════════════════════════════════════════════════════════════════════

        private void ScrollToBottom()
        {
            // Defer scroll until the layout has updated so the new item is measured
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
 * Prompt: 'How to use Dispatcher.Invoke to update WPF UI from a background thread'.
 * Prompt: 'How to dynamically add styled TextBlock elements to a WPF StackPanel in C#'.
 * Prompt: 'How to route user input through a state machine in a WPF chatbot'.
 *
 * OpenAI (2026) ChatGPT [AI assistant]. Available at: https://chat.openai.com (Accessed: 24 April 2026).
 * Prompt: 'How to coordinate WPF typing animation with text-to-speech and implement delegate-based message rendering'.
 */
