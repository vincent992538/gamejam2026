using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using HorseBetting.Data;

namespace HorseBetting.UI
{
    /// <summary>
    /// Controller for the main game screen.
    /// Displays player balance, horse list with odds, round number, and held protection cards.
    /// Validates: Requirements 13.1
    /// </summary>
    public class MainView
    {
        private const int HorseCount = 8;

        // ─── UI Element References ──────────────────────────────────────────────

        private VisualElement _root;
        private Label _gameTitleLabel;
        private Label _roundLabel;
        private Label _balanceValueLabel;
        private Label _cardsCountValueLabel;
        private Label _noCardsLabel;
        private VisualElement _protectionCardsList;

        private Label[] _horseNameLabels;
        private Label[] _horseOddsLabels;

        // ─── Initialization ─────────────────────────────────────────────────────

        /// <summary>
        /// Initializes the MainView by querying all UI elements from the provided root.
        /// Call this once after loading the UXML document.
        /// </summary>
        /// <param name="root">The root VisualElement from the loaded UXML document.</param>
        public void Initialize(VisualElement root)
        {
            _root = root;

            // Header
            _gameTitleLabel = root.Q<Label>("game-title");
            _roundLabel = root.Q<Label>("round-label");

            // HUD Bar
            _balanceValueLabel = root.Q<Label>("balance-value");
            _cardsCountValueLabel = root.Q<Label>("cards-count-value");

            // Horse list labels
            _horseNameLabels = new Label[HorseCount];
            _horseOddsLabels = new Label[HorseCount];

            for (int i = 0; i < HorseCount; i++)
            {
                _horseNameLabels[i] = root.Q<Label>($"horse-name-{i}");
                _horseOddsLabels[i] = root.Q<Label>($"horse-odds-{i}");
            }

            // Protection cards section
            _protectionCardsList = root.Q<VisualElement>("protection-cards-list");
            _noCardsLabel = root.Q<Label>("no-cards-label");
        }

        // ─── Update Methods ─────────────────────────────────────────────────────

        /// <summary>
        /// Updates the displayed player balance.
        /// </summary>
        /// <param name="balance">Current player balance.</param>
        public void UpdateBalance(int balance)
        {
            if (_balanceValueLabel != null)
            {
                _balanceValueLabel.text = $"${balance}";
            }
        }

        /// <summary>
        /// Updates the round number display.
        /// </summary>
        /// <param name="round">Current round number.</param>
        public void UpdateRound(int round)
        {
            if (_roundLabel != null)
            {
                _roundLabel.text = $"Round {round}";
            }
        }

        /// <summary>
        /// Updates the horse list display with names and current odds.
        /// </summary>
        /// <param name="horses">Array of 8 HorseData structs.</param>
        /// <param name="odds">Array of 8 odds values corresponding to each horse by index.</param>
        public void UpdateHorseList(HorseData[] horses, float[] odds)
        {
            if (horses == null || odds == null)
                return;

            int count = Mathf.Min(horses.Length, HorseCount);
            for (int i = 0; i < count; i++)
            {
                if (_horseNameLabels[i] != null)
                {
                    // Add horse avatar before name if not already added
                    var parent = _horseNameLabels[i].parent;
                    if (parent != null && parent.Q("horse-avatar-" + i) == null)
                    {
                        var avatar = new VisualElement();
                        avatar.name = "horse-avatar-" + i;
                        avatar.style.width = 32;
                        avatar.style.height = 32;
                        avatar.style.marginRight = 8;
                        var sprite = HorseBetting.Core.SpriteLoader.LoadHorseSprite(i);
                        if (sprite != null)
                        {
                            avatar.style.backgroundImage = new StyleBackground(sprite);
                        }
                        parent.Insert(0, avatar);
                    }

                    _horseNameLabels[i].text = horses[i].displayName;
                }

                if (_horseOddsLabels[i] != null)
                {
                    _horseOddsLabels[i].text = $"x{odds[i]:F1}";
                }
            }
        }

        /// <summary>
        /// Updates the protection cards section to show currently held cards.
        /// </summary>
        /// <param name="cards">Read-only list of the player's held protection cards.</param>
        public void UpdateProtectionCards(IReadOnlyList<ProtectionCard> cards)
        {
            if (_protectionCardsList == null)
                return;

            // Clear existing card items (keep the no-cards label reference)
            _protectionCardsList.Clear();

            if (cards == null || cards.Count == 0)
            {
                // Show "no cards" label
                var noCardsLabel = new Label("No protection cards held");
                noCardsLabel.name = "no-cards-label";
                noCardsLabel.AddToClassList("no-cards-label");
                _protectionCardsList.Add(noCardsLabel);
                _noCardsLabel = noCardsLabel;
            }
            else
            {
                // Add a card item for each held card
                for (int i = 0; i < cards.Count; i++)
                {
                    var cardItem = new VisualElement();
                    cardItem.AddToClassList("protection-card-item");

                    var cardNameLabel = new Label(cards[i].cardName);
                    cardNameLabel.AddToClassList("protection-card-name");

                    cardItem.Add(cardNameLabel);
                    _protectionCardsList.Add(cardItem);
                }
            }

            // Update count in HUD
            UpdateCardsCount(cards?.Count ?? 0);
        }

        // ─── Private Helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Updates the protection cards count display in the HUD bar.
        /// </summary>
        /// <param name="count">Number of cards currently held.</param>
        private void UpdateCardsCount(int count)
        {
            if (_cardsCountValueLabel != null)
            {
                _cardsCountValueLabel.text = $"{count} / 3";
            }
        }
    }
}
