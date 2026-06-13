using System;
using UnityEngine.UIElements;
using HorseBetting.Data;

namespace HorseBetting.UI
{
    /// <summary>
    /// UI controller for the shop screen.
    /// Displays available protection cards, handles buy interactions,
    /// shows validation errors, and provides a "Start Next Round" button.
    /// Validates: Requirements 10.1, 10.4, 10.5, 13.4
    /// </summary>
    public class ShopView
    {
        private VisualElement _root;
        private Label _balanceLabel;
        private Label _cardsCountLabel;
        private VisualElement _itemsList;
        private Label _errorMessage;
        private Button _startNextRoundButton;

        /// <summary>
        /// Invoked when the player clicks a buy button. Passes the item index.
        /// </summary>
        public event Action<int> OnBuyClicked;

        /// <summary>
        /// Invoked when the player clicks "Start Next Round".
        /// </summary>
        public event Action OnStartNextRound;

        /// <summary>
        /// Initializes the view by querying UI elements from the given root.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;
            _balanceLabel = _root.Q<Label>("shop-balance-value");
            _cardsCountLabel = _root.Q<Label>("shop-cards-count");
            _itemsList = _root.Q<VisualElement>("shop-items-list");
            _errorMessage = _root.Q<Label>("shop-error-message");
            _startNextRoundButton = _root.Q<Button>("start-next-round-button");

            _startNextRoundButton.clicked += HandleStartNextRound;

            HideError();
        }

        /// <summary>
        /// Populates the shop item list with available protection cards.
        /// Each item shows name, protects-against info, success rate, price, and a buy button.
        /// </summary>
        public void SetAvailableItems(ShopItem[] items)
        {
            _itemsList.Clear();

            if (items == null || items.Length == 0)
            {
                var emptyLabel = new Label("No items available");
                emptyLabel.AddToClassList("shop-item-details");
                _itemsList.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                int index = i;

                var itemContainer = new VisualElement();
                itemContainer.AddToClassList("shop-item");

                // Info column
                var infoContainer = new VisualElement();
                infoContainer.AddToClassList("shop-item-info");

                var nameLabel = new Label(item.data.cardName);
                nameLabel.AddToClassList("shop-item-name");

                var detailsText = $"Protects: {item.data.protectsAgainst} | Success: {item.data.successRate * 100f:0}%";
                var detailsLabel = new Label(detailsText);
                detailsLabel.AddToClassList("shop-item-details");

                infoContainer.Add(nameLabel);
                infoContainer.Add(detailsLabel);

                // Price label
                var priceLabel = new Label($"${item.data.price}");
                priceLabel.AddToClassList("shop-item-price");

                // Buy button
                var buyButton = new Button(() => HandleBuyClicked(index));
                buyButton.text = "Buy";
                buyButton.AddToClassList("shop-buy-button");

                itemContainer.Add(infoContainer);
                itemContainer.Add(priceLabel);
                itemContainer.Add(buyButton);

                _itemsList.Add(itemContainer);
            }
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
        /// Updates the displayed card count (e.g., "2 / 3").
        /// </summary>
        public void UpdateCardCount(int count, int max)
        {
            if (_cardsCountLabel != null)
            {
                _cardsCountLabel.text = $"{count} / {max}";
            }
        }

        /// <summary>
        /// Shows an error message to the player (e.g., card limit reached, insufficient funds).
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

        private void HandleBuyClicked(int itemIndex)
        {
            HideError();
            OnBuyClicked?.Invoke(itemIndex);
        }

        private void HandleStartNextRound()
        {
            OnStartNextRound?.Invoke();
        }
    }
}
