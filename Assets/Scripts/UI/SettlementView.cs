using System;
using UnityEngine.UIElements;
using HorseBetting.Data;

namespace HorseBetting.UI
{
    /// <summary>
    /// Controller for the Settlement/Results screen.
    /// Displays final ranking, speed breakdown, bet results, and net profit/loss.
    /// </summary>
    public class SettlementView
    {
        private VisualElement _root;
        private VisualElement _rankingTable;
        private VisualElement _breakdownTable;
        private VisualElement _betResultsTable;
        private Label _totalWinningsLabel;
        private Label _totalLossLabel;
        private Label _netProfitLabel;
        private Button _goToShopButton;

        /// <summary>
        /// Fired when the player clicks "Go to Shop".
        /// </summary>
        public event Action OnGoToShopClicked;

        /// <summary>
        /// Initializes the view by querying UI elements from the provided root.
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;

            _rankingTable = _root.Q<VisualElement>("ranking-table");
            _breakdownTable = _root.Q<VisualElement>("breakdown-table");
            _betResultsTable = _root.Q<VisualElement>("bet-results-table");
            _totalWinningsLabel = _root.Q<Label>("total-winnings-label");
            _totalLossLabel = _root.Q<Label>("total-loss-label");
            _netProfitLabel = _root.Q<Label>("net-profit-label");
            _goToShopButton = _root.Q<Button>("go-to-shop-button");

            _goToShopButton.clicked += HandleGoToShopClicked;
        }

        /// <summary>
        /// Populates the view with race results and settlement data.
        /// </summary>
        /// <param name="raceResult">The race result containing ranking and event data.</param>
        /// <param name="settlement">The settlement result with bet outcomes.</param>
        /// <param name="horses">Array of 8 HorseData with revealed hidden bonuses and base speeds.</param>
        public void ShowResults(RaceResult raceResult, SettlementResult settlement, HorseData[] horses)
        {
            PopulateRankingTable(raceResult, horses);
            PopulateSpeedBreakdown(raceResult, horses);
            PopulateBetResults(settlement);
            PopulateSummary(settlement);
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_goToShopButton != null)
            {
                _goToShopButton.clicked -= HandleGoToShopClicked;
            }
        }

        private void HandleGoToShopClicked()
        {
            OnGoToShopClicked?.Invoke();
        }

        private void PopulateRankingTable(RaceResult raceResult, HorseData[] horses)
        {
            _rankingTable.Clear();

            for (int rank = 0; rank < raceResult.finalRanking.Length; rank++)
            {
                int horseIndex = raceResult.finalRanking[rank];
                string horseName = horses[horseIndex].displayName;
                int finalSpeed = raceResult.finalSpeeds[horseIndex];

                var row = CreateRankingRow(rank + 1, horseName, finalSpeed);
                _rankingTable.Add(row);
            }
        }

        private VisualElement CreateRankingRow(int position, string horseName, int finalSpeed)
        {
            var row = new VisualElement();
            row.AddToClassList("ranking-row");

            var posLabel = new Label($"{position}");
            posLabel.AddToClassList("rank-col");

            var nameLabel = new Label(horseName);
            nameLabel.AddToClassList("horse-col");

            var speedLabel = new Label($"{finalSpeed}");
            speedLabel.AddToClassList("speed-col");

            row.Add(posLabel);
            row.Add(nameLabel);
            row.Add(speedLabel);

            return row;
        }

        private void PopulateSpeedBreakdown(RaceResult raceResult, HorseData[] horses)
        {
            _breakdownTable.Clear();

            for (int rank = 0; rank < raceResult.finalRanking.Length; rank++)
            {
                int horseIndex = raceResult.finalRanking[rank];
                var horse = horses[horseIndex];

                int baseSpeed = horse.baseSpeed;
                int hiddenBonus = horse.hiddenBonus;
                int finalSpeed = raceResult.finalSpeeds[horseIndex];

                // Calculate total event modifiers across all stages
                int totalEventModifier = CalculateTotalEventModifier(raceResult, horseIndex);

                // Track modifier = finalSpeed - baseSpeed - hiddenBonus - totalEventModifier
                int trackModifier = finalSpeed - baseSpeed - hiddenBonus - totalEventModifier;

                var row = CreateBreakdownRow(
                    horse.displayName,
                    baseSpeed,
                    hiddenBonus,
                    trackModifier,
                    totalEventModifier,
                    finalSpeed
                );
                _breakdownTable.Add(row);
            }
        }

        private int CalculateTotalEventModifier(RaceResult raceResult, int horseIndex)
        {
            int total = 0;

            if (raceResult.stageEvents == null)
                return total;

            for (int stage = 0; stage < raceResult.stageEvents.Length; stage++)
            {
                if (raceResult.stageEvents[stage] == null)
                    continue;

                for (int e = 0; e < raceResult.stageEvents[stage].Length; e++)
                {
                    var evt = raceResult.stageEvents[stage][e];
                    if (evt.horseIndex == horseIndex && !evt.wasProtected)
                    {
                        total += evt.speedModifier;
                    }
                }
            }

            return total;
        }

        private VisualElement CreateBreakdownRow(string horseName, int baseSpeed, int hiddenBonus, int trackModifier, int eventModifier, int total)
        {
            var row = new VisualElement();
            row.AddToClassList("breakdown-row");

            var nameLabel = new Label(horseName);
            nameLabel.AddToClassList("breakdown-horse-col");

            var baseLabel = new Label($"{baseSpeed}");
            baseLabel.AddToClassList("breakdown-data-col");

            var hiddenLabel = new Label($"+{hiddenBonus}");
            hiddenLabel.AddToClassList("breakdown-data-col");

            var trackLabel = new Label(FormatModifier(trackModifier));
            trackLabel.AddToClassList("breakdown-data-col");

            var eventLabel = new Label(FormatModifier(eventModifier));
            eventLabel.AddToClassList("breakdown-data-col");

            var totalLabel = new Label($"{total}");
            totalLabel.AddToClassList("breakdown-data-col");

            row.Add(nameLabel);
            row.Add(baseLabel);
            row.Add(hiddenLabel);
            row.Add(trackLabel);
            row.Add(eventLabel);
            row.Add(totalLabel);

            return row;
        }

        private void PopulateBetResults(SettlementResult settlement)
        {
            _betResultsTable.Clear();

            if (settlement.betResults == null || settlement.betResults.Length == 0)
            {
                var emptyLabel = new Label("No bets placed this round.");
                emptyLabel.AddToClassList("bet-type-col");
                _betResultsTable.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < settlement.betResults.Length; i++)
            {
                var betSettlement = settlement.betResults[i];
                var row = CreateBetResultRow(betSettlement);
                _betResultsTable.Add(row);
            }
        }

        private VisualElement CreateBetResultRow(BetSettlement betSettlement)
        {
            var row = new VisualElement();
            row.AddToClassList("bet-row");

            var typeLabel = new Label(FormatBetType(betSettlement.bet.type));
            typeLabel.AddToClassList("bet-type-col");

            var amountLabel = new Label($"${betSettlement.bet.amount}");
            amountLabel.AddToClassList("bet-amount-col");

            var resultLabel = new Label(betSettlement.won ? "WIN" : "LOSE");
            resultLabel.AddToClassList("bet-result-col");
            resultLabel.AddToClassList(betSettlement.won ? "bet-win" : "bet-lose");

            var payoutLabel = new Label(betSettlement.won ? $"+${betSettlement.payout}" : "$0");
            payoutLabel.AddToClassList("bet-payout-col");
            if (betSettlement.won)
                payoutLabel.AddToClassList("bet-win");

            row.Add(typeLabel);
            row.Add(amountLabel);
            row.Add(resultLabel);
            row.Add(payoutLabel);

            return row;
        }

        private void PopulateSummary(SettlementResult settlement)
        {
            _totalWinningsLabel.text = $"${settlement.totalWinnings}";
            _totalLossLabel.text = $"${settlement.totalLoss}";

            int netProfit = settlement.netProfit;
            string sign = netProfit >= 0 ? "+" : "";
            _netProfitLabel.text = $"{sign}${netProfit}";

            // Remove previous profit styling
            _netProfitLabel.RemoveFromClassList("profit-positive");
            _netProfitLabel.RemoveFromClassList("profit-negative");

            // Apply color based on profit/loss
            if (netProfit >= 0)
                _netProfitLabel.AddToClassList("profit-positive");
            else
                _netProfitLabel.AddToClassList("profit-negative");
        }

        private string FormatBetType(BetType type)
        {
            switch (type)
            {
                case BetType.SingleWin: return "Single Win";
                case BetType.Place: return "Place";
                case BetType.Quinella: return "Quinella";
                case BetType.Exacta: return "Exacta";
                case BetType.Trio: return "Trio";
                case BetType.Trifecta: return "Trifecta";
                default: return type.ToString();
            }
        }

        private string FormatModifier(int value)
        {
            if (value >= 0)
                return $"+{value}";
            return $"{value}";
        }
    }
}
