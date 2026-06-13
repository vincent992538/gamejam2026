using System.Collections.Generic;
using HorseBetting.Data;

namespace HorseBetting.Core
{
    /// <summary>
    /// Manages the player's runtime state: balance, protection cards, and current bets.
    /// Validates: Requirements 9.6, 10.6, 11.6
    /// </summary>
    public class PlayerState
    {
        private readonly int _maxProtectionCards;
        private readonly List<ProtectionCard> _protectionCards;
        private readonly List<Bet> _currentBets;

        /// <summary>
        /// Current player balance.
        /// </summary>
        public int Balance { get; private set; }

        /// <summary>
        /// Currently held protection cards (read-only view).
        /// </summary>
        public IReadOnlyList<ProtectionCard> ProtectionCards => _protectionCards;

        /// <summary>
        /// Number of protection cards currently held.
        /// </summary>
        public int CardCount => _protectionCards.Count;

        /// <summary>
        /// Current active bets for this round (read-only view).
        /// </summary>
        public IReadOnlyList<Bet> CurrentBets => _currentBets;

        /// <summary>
        /// Maximum number of protection cards the player can hold.
        /// </summary>
        public int MaxProtectionCards => _maxProtectionCards;

        /// <summary>
        /// Creates a new PlayerState with the given starting balance and card limit.
        /// </summary>
        /// <param name="startingBalance">Initial balance for the player.</param>
        /// <param name="maxProtectionCards">Maximum number of protection cards allowed (default 3).</param>
        public PlayerState(int startingBalance, int maxProtectionCards = 3)
        {
            Balance = startingBalance;
            _maxProtectionCards = maxProtectionCards;
            _protectionCards = new List<ProtectionCard>();
            _currentBets = new List<Bet>();
        }

        /// <summary>
        /// Deducts the specified amount from the player's balance.
        /// Used by BettingSystem (Req 9.6) and ShopSystem (Req 10.6).
        /// </summary>
        /// <param name="amount">Amount to deduct. Must be positive and not exceed current balance.</param>
        /// <returns>True if deduction was successful, false if insufficient funds or invalid amount.</returns>
        public bool DeductBalance(int amount)
        {
            if (amount <= 0 || amount > Balance)
                return false;

            Balance -= amount;
            return true;
        }

        /// <summary>
        /// Adds the specified amount to the player's balance.
        /// Used by SettlementSystem for winnings (Req 11.6).
        /// </summary>
        /// <param name="amount">Amount to add. Must be positive.</param>
        /// <returns>True if addition was successful, false if invalid amount.</returns>
        public bool AddBalance(int amount)
        {
            if (amount <= 0)
                return false;

            Balance += amount;
            return true;
        }

        /// <summary>
        /// Adds a protection card to the player's inventory.
        /// Enforces the maximum card limit.
        /// </summary>
        /// <param name="card">The protection card to add.</param>
        /// <returns>True if the card was added, false if at maximum capacity.</returns>
        public bool AddProtectionCard(ProtectionCard card)
        {
            if (_protectionCards.Count >= _maxProtectionCards)
                return false;

            _protectionCards.Add(card);
            return true;
        }

        /// <summary>
        /// Removes the first protection card that matches the specified event name.
        /// Used when a card is consumed to protect against an event.
        /// </summary>
        /// <param name="protectsAgainst">The event name the card protects against.</param>
        /// <returns>True if a matching card was found and removed, false otherwise.</returns>
        public bool RemoveProtectionCard(string protectsAgainst)
        {
            for (int i = 0; i < _protectionCards.Count; i++)
            {
                if (_protectionCards[i].protectsAgainst == protectsAgainst)
                {
                    _protectionCards.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Records a bet placed by the player.
        /// </summary>
        /// <param name="bet">The bet to record.</param>
        public void AddBet(Bet bet)
        {
            _currentBets.Add(bet);
        }

        /// <summary>
        /// Clears all current bets (called after settlement).
        /// </summary>
        public void ClearBets()
        {
            _currentBets.Clear();
        }

        /// <summary>
        /// Resets the player state for a new game.
        /// Restores balance and clears all cards and bets.
        /// </summary>
        /// <param name="startingBalance">The balance to reset to.</param>
        public void Reset(int startingBalance)
        {
            Balance = startingBalance;
            _protectionCards.Clear();
            _currentBets.Clear();
        }
    }
}
