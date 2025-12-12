using ErmakovMemoryChallenge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace ErmakovMemoryChallenge
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer gameTimer;
        private DispatcherTimer levelTransitionTimer;
        private List<MemoryCard> cards;
        private MemoryCard firstSelectedCard;
        private MemoryCard secondSelectedCard;
        private bool isMemorizationPhase;
        private bool isGameActive;
        private int moves;
        private int pairsFound;
        private int totalPairs;
        private GameSettings currentSettings;
        private Stopwatch stopwatch;
        private TimeSpan memorizationTime;
        private TimeSpan gameTime;
        private DateTime memorizationStartTime;
        private DateTime gameStartTime;

        private int currentChallengeLevel;
        private bool isChallengeCompleted;
        private bool isLevelTransition;

        private readonly List<(string fieldSize, int time)> challengeLevels = new List<(string, int)>
        {
            ("2x3", 60),  
            ("3x4", 90), 
            ("4x4", 120),  
            ("4x5", 150),  
            ("5x6", 180),
            ("6x6", 240)   
        };

        private readonly Dictionary<string, int> memorizationTimes = new Dictionary<string, int>
        {
            {"Easy", 5}, {"Medium", 3}, {"Hard", 2}, {"Expert", 1}
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimers();
            InitializeDefaultSettings();
            UpdateUI();
        }

        private void InitializeTimers()
        {
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameTimer_Tick;

            levelTransitionTimer = new DispatcherTimer();
            levelTransitionTimer.Interval = TimeSpan.FromSeconds(2);
            levelTransitionTimer.Tick += LevelTransitionTimer_Tick;

            stopwatch = new Stopwatch();
        }

        private void InitializeDefaultSettings()
        {
            currentSettings = new GameSettings
            {
                GameMode = "Normal",
                FieldSize = "3x4",
                Difficulty = "Medium",
                ContentType = "Numbers"
            };

            currentChallengeLevel = 1;
            isChallengeCompleted = false;
            isLevelTransition = false;

            FieldSizeComboBox.SelectedIndex = 1;
            DifficultyComboBox.SelectedIndex = 1;
            ContentTypeComboBox.SelectedIndex = 0;
        }

        private void StartNewGame()
        {
            ResetGame();
            CreateGameField();
            StartMemorizationPhase();
        }

        private void StartNextLevel()
        {
            if (currentChallengeLevel < challengeLevels.Count)
            {
                currentChallengeLevel++;
                isLevelTransition = true;
                StatusText.Text = $"🎉 Уровень {currentChallengeLevel - 1} пройден! Загружаем уровень {currentChallengeLevel}...";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(105, 240, 174));

                levelTransitionTimer.Start();
            }
            else
            {
                isChallengeCompleted = true;
                StatusText.Text = "🏆 Поздравляем! Вы прошли все уровни испытания!";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(105, 240, 174));
                UpdateUI();
            }
        }

        private void LevelTransitionTimer_Tick(object sender, EventArgs e)
        {
            levelTransitionTimer.Stop();
            isLevelTransition = false;
            StartNewGame();
        }

        private void CreateGameField()
        {
            if (currentSettings == null) return;

            cards = new List<MemoryCard>();

            string fieldSize;
            if (currentSettings.GameMode == "Challenge")
            {
                if (currentChallengeLevel <= challengeLevels.Count)
                {
                    fieldSize = challengeLevels[currentChallengeLevel - 1].fieldSize;
                }
                else
                {
                    fieldSize = "3x4"; 
                }
            }
            else
            {
                fieldSize = currentSettings.FieldSize;
            }

            var fieldSizeParts = fieldSize.Split('x');
            int rows = int.Parse(fieldSizeParts[0]);
            int cols = int.Parse(fieldSizeParts[1]);

            FieldGrid.Children.Clear();
            FieldGrid.Rows = rows;
            FieldGrid.Columns = cols;

            totalPairs = (rows * cols) / 2;
            var contents = GenerateCardContents(totalPairs);

            for (int i = 0; i < totalPairs; i++)
            {
                cards.Add(new MemoryCard(contents[i]));
                cards.Add(new MemoryCard(contents[i]));
            }

            var rnd = new Random();
            cards = cards.OrderBy(x => rnd.Next()).ToList();

            foreach (var card in cards)
            {
                var button = new Button
                {
                    Content = card.DisplayContent,
                    Style = (Style)FindResource("ModernCardButton"),
                    Tag = card,
                    Background = card.CardColor
                };
                button.Click += Card_Click;
                FieldGrid.Children.Add(button);
            }

            PairsText.Text = $"0/{totalPairs}";
        }

        private List<string> GenerateCardContents(int pairsCount)
        {
            var contents = new List<string>();
            var rnd = new Random();

            if (currentSettings.ContentType == "Numbers")
            {
                var numbers = Enumerable.Range(10, 90).OrderBy(x => rnd.Next()).Take(pairsCount).ToList();
                foreach (var number in numbers)
                {
                    contents.Add(number.ToString());
                }
            }
            else
            {
                string symbols = "★♠♥♦♣♫☀☁⚡☂♨✂✈✉✌❀❁❂❄❅❆❇❈❉❊";
                var symbolList = symbols.ToCharArray().OrderBy(x => rnd.Next()).Take(pairsCount).ToList();
                foreach (var symbol in symbolList)
                {
                    contents.Add(symbol.ToString());
                }
            }

            return contents;
        }

        private void StartMemorizationPhase()
        {
            isMemorizationPhase = true;
            isGameActive = false;

            if (currentSettings.GameMode == "Challenge")
            {
                StatusText.Text = $"👀 Уровень {currentChallengeLevel} - Запоминайте карточки...";
            }
            else
            {
                StatusText.Text = "👀 Запоминайте карточки...";
            }

            NormalModeButton.IsEnabled = false;
            ChallengeModeButton.IsEnabled = false;

            foreach (var card in cards)
            {
                card.IsRevealed = true;
            }
            UpdateCardAppearance();

            memorizationTime = TimeSpan.FromSeconds(memorizationTimes[currentSettings.Difficulty]);
            memorizationStartTime = DateTime.Now;
            TimerText.Text = $"0:{memorizationTime.Seconds:D2}";

            gameTimer.Start();
        }

        private void StartGamePhase()
        {
            isMemorizationPhase = false;
            isGameActive = true;

            if (currentSettings.GameMode == "Challenge")
            {
                StatusText.Text = $"🎯 Уровень {currentChallengeLevel} - Найдите все пары!";
            }
            else
            {
                StatusText.Text = "🎯 Найдите все пары!";
            }

            foreach (var card in cards)
            {
                card.IsRevealed = false;
            }
            UpdateCardAppearance();

            if (currentSettings.GameMode == "Challenge")
            {
                if (currentChallengeLevel <= challengeLevels.Count)
                {
                    gameTime = TimeSpan.FromSeconds(challengeLevels[currentChallengeLevel - 1].time);
                    gameStartTime = DateTime.Now;
                    UpdateTimerDisplay();
                }
            }
            else
            {
                TimerText.Text = "--:--";
                gameTimer.Stop();
            }
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            if (!isGameActive || isMemorizationPhase || isLevelTransition) return;

            var button = (Button)sender;
            var card = (MemoryCard)button.Tag;

            if (card.IsRevealed || card.IsMatched) return;

            if (firstSelectedCard != null && secondSelectedCard != null) return;

            card.IsRevealed = true;
            UpdateCardAppearance();

            if (firstSelectedCard == null)
            {
                firstSelectedCard = card;
            }
            else
            {
                secondSelectedCard = card;
                moves++;
                MovesText.Text = moves.ToString();
                CheckForMatch();
            }
        }

        private void CheckForMatch()
        {
            if (firstSelectedCard.Content == secondSelectedCard.Content)
            {
                firstSelectedCard.IsMatched = true;
                secondSelectedCard.IsMatched = true;
                pairsFound++;
                PairsText.Text = $"{pairsFound}/{totalPairs}";

                firstSelectedCard = null;
                secondSelectedCard = null;
                UpdateCardAppearance();

                if (pairsFound == totalPairs)
                {
                    EndGame(true);
                }
            }
            else
            {
                var tempFirst = firstSelectedCard;
                var tempSecond = secondSelectedCard;

                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(800);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    if (!tempFirst.IsMatched) tempFirst.IsRevealed = false;
                    if (!tempSecond.IsMatched) tempSecond.IsRevealed = false;
                    firstSelectedCard = null;
                    secondSelectedCard = null;
                    UpdateCardAppearance();
                };
                timer.Start();
            }
        }

        private void EndGame(bool isWin)
        {
            isGameActive = false;
            isMemorizationPhase = false;
            gameTimer.Stop();

            NormalModeButton.IsEnabled = true;
            ChallengeModeButton.IsEnabled = true;

            if (isWin)
            {
                if (currentSettings.GameMode == "Challenge")
                {
                    StartNextLevel();
                }
                else
                {
                    StatusText.Text = "🎉 Поздравляем! Вы выиграли!";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(105, 240, 174));
                }
            }
            else
            {
                if (currentSettings.GameMode == "Challenge")
                {
                    currentChallengeLevel = 1;
                    StatusText.Text = "⏰ Время вышло! Испытание провалено. Прогресс сброшен.";
                }
                else
                {
                    StatusText.Text = "⏰ Время вышло! Игра окончена.";
                }
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 82, 82));
            }

            UpdateUI();
        }

        private void ResetGame()
        {
            gameTimer.Stop();
            levelTransitionTimer.Stop();

            NormalModeButton.IsEnabled = true;
            ChallengeModeButton.IsEnabled = true;

            isLevelTransition = false;

            cards?.Clear();
            FieldGrid.Children.Clear();
            firstSelectedCard = null;
            secondSelectedCard = null;
            moves = 0;
            pairsFound = 0;

            MovesText.Text = "0";
            PairsText.Text = "0/0";
            TimerText.Text = "00:00";
            TimerText.Foreground = new SolidColorBrush(Color.FromRgb(105, 240, 174));

            if (currentSettings.GameMode == "Challenge" && currentChallengeLevel > 1 && !isChallengeCompleted)
            {
                StatusText.Text = $"Готов к уровню {currentChallengeLevel}";
            }
            else
            {
                StatusText.Text = "Готов к игре";
            }
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 213, 79));

            UpdateUI();
        }

        private void ClearGameField()
        {
            gameTimer.Stop();
            levelTransitionTimer.Stop();

            cards?.Clear();
            FieldGrid.Children.Clear();
            firstSelectedCard = null;
            secondSelectedCard = null;
            moves = 0;
            pairsFound = 0;

            MovesText.Text = "0";
            PairsText.Text = "0/0";
            TimerText.Text = "00:00";
            StatusText.Text = "Готов к игре";

            UpdateUI();
        }

        private void UpdateCardAppearance()
        {
            foreach (Button button in FieldGrid.Children)
            {
                var card = (MemoryCard)button.Tag;
                button.Content = card.DisplayContent;
                button.Background = card.CardColor;
                button.IsEnabled = !card.IsMatched;
            }
        }

        private void UpdateUI()
        {
            if (currentSettings == null) return;

            bool isChallenge = currentSettings.GameMode == "Challenge";

            if (isChallenge)
            {
                if (isChallengeCompleted)
                {
                    LevelText.Text = "Завершено";
                }
                else
                {
                    LevelText.Text = currentChallengeLevel.ToString();
                }
                NormalModeSettings.Visibility = Visibility.Collapsed;
                ChallengeModeInfo.Visibility = Visibility.Visible;
            }
            else
            {
                LevelText.Text = "-";
                NormalModeSettings.Visibility = Visibility.Visible;
                ChallengeModeInfo.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateTimerDisplay()
        {
            if (currentSettings.GameMode == "Challenge" && isGameActive)
            {
                var elapsed = DateTime.Now - gameStartTime;
                var remaining = gameTime - elapsed;

                if (remaining.TotalSeconds > 0)
                {
                    TimerText.Text = $"{(int)remaining.TotalMinutes}:{remaining.Seconds:D2}";

                    if (remaining.TotalSeconds <= 10)
                    {
                        TimerText.Foreground = new SolidColorBrush(Color.FromRgb(255, 82, 82));
                    }
                    else
                    {
                        TimerText.Foreground = new SolidColorBrush(Color.FromRgb(105, 240, 174));
                    }
                }
                else
                {
                    TimerText.Text = "0:00";
                    EndGame(false);
                }
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (isMemorizationPhase)
            {
                var elapsed = DateTime.Now - memorizationStartTime;
                var remaining = memorizationTime - elapsed;

                if (remaining.TotalSeconds > 0)
                {
                    TimerText.Text = $"0:{remaining.Seconds:D2}";
                }
                else
                {
                    TimerText.Text = "0:00";
                    StartGamePhase();
                }
            }
            else if (isGameActive && currentSettings.GameMode == "Challenge")
            {
                UpdateTimerDisplay();
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSettings.GameMode == "Challenge")
            {
                currentChallengeLevel = 1;
                isChallengeCompleted = false;
            }
            StartNewGame();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void ApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (FieldSizeComboBox.SelectedItem == null ||
                DifficultyComboBox.SelectedItem == null ||
                ContentTypeComboBox.SelectedItem == null)
                return;

            currentSettings.FieldSize = ((ComboBoxItem)FieldSizeComboBox.SelectedItem).Tag.ToString();
            currentSettings.Difficulty = ((ComboBoxItem)DifficultyComboBox.SelectedItem).Tag.ToString();
            currentSettings.ContentType = ((ComboBoxItem)ContentTypeComboBox.SelectedItem).Tag.ToString();

            UpdateUI();
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void NormalModeButton_Checked(object sender, RoutedEventArgs e)
        {
            if (currentSettings != null && NormalModeButton.IsChecked == true)
            {
                currentSettings.GameMode = "Normal";
                ClearGameField();
                UpdateUI();
            }
        }

        private void ChallengeModeButton_Checked(object sender, RoutedEventArgs e)
        {
            if (currentSettings != null && ChallengeModeButton.IsChecked == true)
            {
                currentSettings.GameMode = "Challenge";
                currentChallengeLevel = 1;
                isChallengeCompleted = false;
                ClearGameField();
                UpdateUI();
            }
        }

        private void NormalModeButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ChallengeModeButton.IsChecked != true)
            {
                NormalModeButton.IsChecked = true;
            }
        }

        private void ChallengeModeButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (NormalModeButton.IsChecked != true)
            {
                ChallengeModeButton.IsChecked = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TopPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }

    public class MemoryCard
    {
        public string Content { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsMatched { get; set; }

        public string DisplayContent => IsRevealed || IsMatched ? Content : "?";
        public Brush CardColor
        {
            get
            {
                if (IsMatched)
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                if (IsRevealed)
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243));
                return new SolidColorBrush(Color.FromRgb(123, 31, 162));
            }
        }

        public MemoryCard(string content)
        {
            Content = content;
            IsRevealed = false;
            IsMatched = false;
        }
    }

    public class GameSettings
    {
        public string GameMode { get; set; }
        public string FieldSize { get; set; }
        public string Difficulty { get; set; }
        public string ContentType { get; set; }
    }
}