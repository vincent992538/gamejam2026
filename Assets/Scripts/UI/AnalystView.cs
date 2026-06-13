using System;
using UnityEngine.UIElements;
using HorseBetting.Data;

namespace HorseBetting.UI
{
    /// <summary>
    /// UI controller for the analyst intelligence purchase screen.
    /// Displays Senior/Junior analyst options with prices and buy buttons,
    /// shows purchased intel, and handles validation errors.
    /// Validates: Requirements 6.5, 6.7, 13.2
    /// </summary>
    public class AnalystView
    {
        private VisualElement _root;
        private Label _balanceLabel;
        private Label _seniorPriceLabel;
        private Label _juniorPriceLabel;
        private Button _seniorBuyButton;
        private Button _juniorBuyButton;
        private VisualElement _intelSection;
        private VisualElement _intelList;
        private Label _errorMessage;
        private Button _continueButton;

        /// <summary>
        /// Invoked when the player clicks a buy button. Passes the analyst type.
        /// </summary>
        public event Action<AnalystType> OnBuyIntelClicked;

        /// <summary>
        /// Invoked when the player clicks "Continue".
        /// </summary>
        public event Action OnContinueClicked;

        /// <summary>
        /// Initializes the view by querying UI elements from the given root.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;
            _balanceLabel = _root.Q<Label>("analyst-balance-value");
            _seniorPriceLabel = _root.Q<Label>("senior-analyst-price");
            _juniorPriceLabel = _root.Q<Label>("junior-analyst-price");
            _seniorBuyButton = _root.Q<Button>("senior-buy-button");
            _juniorBuyButton = _root.Q<Button>("junior-buy-button");
            _intelSection = _root.Q<VisualElement>("analyst-intel-section");
            _intelList = _root.Q<VisualElement>("analyst-intel-list");
            _errorMessage = _root.Q<Label>("analyst-error-message");
            _continueButton = _root.Q<Button>("analyst-continue-button");

            _seniorBuyButton.clicked += () => HandleBuyClicked(AnalystType.Senior);
            _juniorBuyButton.clicked += () => HandleBuyClicked(AnalystType.Junior);
            _continueButton.clicked += HandleContinueClicked;

            HideError();
        }

        /// <summary>
        /// Updates the displayed player balance.
        /// </summary>
        public void UpdateBalance(int balance)
        {
            if (_balanceLabel != null)
            {
                _balanceLabel.text = $"${balance}";
            }
        }

        /// <summary>
        /// Sets the displayed prices for each analyst type.
        /// </summary>
        public void SetAnalystPrices(int seniorPrice, int juniorPrice)
        {
            if (_seniorPriceLabel != null)
            {
                _seniorPriceLabel.text = $"${seniorPrice}";
            }
            if (_juniorPriceLabel != null)
            {
                _juniorPriceLabel.text = $"${juniorPrice}";
            }
        }

        /// <summary>
        /// Reveals purchased intel in the intel section.
        /// </summary>
        public void ShowIntel(AnalystType type, string content)
        {
            // Make intel section visible
            if (_intelSection != null)
            {
                _intelSection.RemoveFromClassList("hidden");
            }

            if (_intelList == null) return;

            var entryContainer = new VisualElement();
            entryContainer.AddToClassList("analyst-intel-entry");

            var typeLabel = new Label(type == AnalystType.Senior ? "Senior Analyst" : "Junior Analyst");
            typeLabel.AddToClassList("analyst-intel-type");

            var contentLabel = new Label(content);
            contentLabel.AddToClassList("analyst-intel-content");

            entryContainer.Add(typeLabel);
            entryContainer.Add(contentLabel);
            _intelList.Add(entryContainer);
        }

        /// <summary>
        /// Shows an error message to the player (e.g., insufficient funds).
        /// </summary>
        public void ShowError(string message)
        {
            if (_errorMessage != null)
            {
                _errorMessage.text = message;
                _errorMessage.RemoveFromClassList("hidden");
            }
        }

        /// <summary>
        /// Hides the error message.
        /// </summary>
        public void HideError()
        {
            if (_errorMessage != null)
            {
                _errorMessage.text = "";
                _errorMessage.AddToClassList("hidden");
            }
        }

        private void HandleBuyClicked(AnalystType type)
        {
            HideError();
            OnBuyIntelClicked?.Invoke(type);
        }

        private void HandleContinueClicked()
        {
            OnContinueClicked?.Invoke();
        }
    }
}
