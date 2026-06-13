using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using HorseBetting.Data;

namespace HorseBetting.UI
{
    /// <summary>
    /// Controller for the betting screen UI.
    /// Displays horse info, revealed message cards, bet type selector,
    /// amount input, and confirm/done buttons.
    /// </summary>
    public class BettingView
    {
        // UI element references
        private VisualElement _root;
        private Label _balanceLabel;
        private Label[] _horseLabels;
        private VisualElement _messageCardsList;
        private DropdownField _betTypeDropdown;
        private Toggle[] _horseToggles;
        private IntegerField _betAmountInput;
        private Label _errorLabel;
        private Button _confirmBetButton;
        private Button _doneBettingButton;

        // Events
        public event Action<Bet> OnBetPlaced;
        public event Action OnDoneBetting;

        // State
        private int _currentBalance;
        private int _currentBettingRound;
        private float _currentOdds;

        // Bet type options matching BetType enum order
        private static readonly string[] BetTypeNames =
        {
            "Single Win (1st place)",
            "Place (Top 3)",
            "Quinella (Top 2, any order)",
            "Exacta (Top 2, exact order)",
            "Trio (Top 3, any order)",
            "Trifecta (Top 3, exact order)"
        };

        /// <summary>
        /// Initialize the view by binding to a root VisualElement.
        /// Call this after loading the UXML document.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;

            // Query elements
            _balanceLabel = _root.Q<Label>("balance-label");
            _messageCardsList = _root.Q<VisualElement>("message-cards-list");
            _betTypeDropdown = _root.Q<DropdownField>("bet-type-dropdown");
            _betAmountInput = _root.Q<IntegerField>("bet-amount-input");
            _errorLabel = _root.Q<Label>("error-label");
            _confirmBetButton = _root.Q<Button>("confirm-bet-button");
            _doneBettingButton = _root.Q<Button>("done-betting-button");

            // Query horse labels
            _horseLabels = new Label[8];
            for (int i = 0; i < 8; i++)
            {
                _horseLabels[i] = _root.Q<Label>($"horse-{i}");
            }

            // Query horse toggles
            _horseToggles = new Toggle[8];
            for (int i = 0; i < 8; i++)
            {
                _horseToggles[i] = _root.Q<Toggle>($"horse-toggle-{i}");
            }

            // Setup bet type dropdown choices
            _betTypeDropdown.choices = new List<string>(BetTypeNames);
            _betTypeDropdown.index = 0;

            // Wire button callbacks
            _confirmBetButton.clicked += HandleConfirmBet;
            _doneBettingButton.clicked += HandleDoneBetting;

            // Hide error by default
            HideError();
        }

        /// <summary>
        /// Set horse data to display in the horse info section.
        /// </summary>
        public void SetHorseData(HorseData[] horses)
        {
            if (horses == null) return;

            // Load horse sprites for display
            for (int i = 0; i < 8 && i < horses.Length; i++)
            {
                if (_horseLabels[i] != null)
                {
                    _horseLabels[i].text = horses[i].displayName;
                }
                if (_horseToggles[i] != null)
                {
                    _horseToggles[i].label = horses[i].displayName;

                    // Add horse sprite as background image on the toggle
                    var sprite = HorseBetting.Core.SpriteLoader.LoadHorseSprite(i);
                    if (sprite != null)
                    {
                        _horseToggles[i].style.backgroundImage = new StyleBackground(sprite);
                        _horseToggles[i].style.height = 50;
                    }
                }
            }
        }

        /// <summary>
        /// Set the revealed message cards to display.
        /// </summary>
        public void SetRevealedCards(MessageCard[] cards)
        {
            _messageCardsList.Clear();

            if (cards == null) return;

            foreach (var card in cards)
            {
                var cardLabel = new Label($"{GetHorseName(card.horseIndex)}: {card.description}");
                cardLabel.AddToClassList("message-card-item");
                _messageCardsList.Add(cardLabel);
            }
        }

        /// <summary>
        /// Update the displayed player balance.
        /// </summary>
        public void UpdateBalance(int balance)
        {
            _currentBalance = balance;
            if (_balanceLabel != null)
            {
                _balanceLabel.text = $"Balance: ${balance}";
            }
        }

        /// <summary>
        /// Set the current betting round (1, 2, or 3).
        /// </summary>
        public void SetBettingRound(int round)
        {
            _currentBettingRound = round;
        }

        /// <summary>
        /// Set the current odds value (used when constructing the Bet struct).
        /// </summary>
        public void SetCurrentOdds(float odds)
        {
            _currentOdds = odds;
        }

        /// <summary>
        /// Show an error message to the player.
        /// </summary>
        public void ShowError(string message)
        {
            if (_errorLabel == null) return;
            _errorLabel.text = message;
            _errorLabel.RemoveFromClassList("hidden");
        }

        /// <summary>
        /// Hide the error message.
        /// </summary>
        public void HideError()
        {
            if (_errorLabel == null) return;
            _errorLabel.text = "";
            _errorLabel.AddToClassList("hidden");
        }

        /// <summary>
        /// Clean up event listeners. Call when the view is being disposed.
        /// </summary>
        public void Dispose()
        {
            if (_confirmBetButton != null)
                _confirmBetButton.clicked -= HandleConfirmBet;
            if (_doneBettingButton != null)
                _doneBettingButton.clicked -= HandleDoneBetting;
        }

        private void HandleConfirmBet()
        {
            HideError();

            // Get selected bet type
            int betTypeIndex = _betTypeDropdown.index;
            if (betTypeIndex < 0 || betTypeIndex >= 6)
            {
                ShowError("Please select a bet type.");
                return;
            }

            BetType selectedType = (BetType)betTypeIndex;

            // Get selected horses
            int[] selectedHorses = GetSelectedHorses();
            if (!ValidateHorseSelection(selectedType, selectedHorses))
            {
                return; // Error already shown in validation
            }

            // Get bet amount
            int amount = _betAmountInput.value;
            if (amount <= 0)
            {
                ShowError("Bet amount must be greater than zero.");
                return;
            }

            if (amount > _currentBalance)
            {
                ShowError("Insufficient balance.");
                return;
            }

            // Construct and emit the bet
            var bet = new Bet
            {
                type = selectedType,
                amount = amount,
                selectedHorses = selectedHorses,
                bettingRound = _currentBettingRound,
                oddsAtBet = _currentOdds
            };

            OnBetPlaced?.Invoke(bet);
        }

        private void HandleDoneBetting()
        {
            OnDoneBetting?.Invoke();
        }

        private int[] GetSelectedHorses()
        {
            var selected = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                if (_horseToggles[i] != null && _horseToggles[i].value)
                {
                    selected.Add(i);
                }
            }
            return selected.ToArray();
        }

        private bool ValidateHorseSelection(BetType type, int[] selectedHorses)
        {
            int required;
            switch (type)
            {
                case BetType.SingleWin:
                case BetType.Place:
                    required = 1;
                    break;
                case BetType.Quinella:
                case BetType.Exacta:
                    required = 2;
                    break;
                case BetType.Trio:
                case BetType.Trifecta:
                    required = 3;
                    break;
                default:
                    required = 1;
                    break;
            }

            if (selectedHorses.Length != required)
            {
                ShowError($"Please select exactly {required} horse(s) for this bet type.");
                return false;
            }

            return true;
        }

        private string GetHorseName(int horseIndex)
        {
            if (horseIndex >= 0 && horseIndex < 8)
                return $"Horse {horseIndex + 1}";
            return $"Horse ?";
        }
    }
}
