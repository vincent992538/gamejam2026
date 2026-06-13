using System;
using UnityEngine.UIElements;
using HorseBetting.Data;

namespace HorseBetting.UI
{
    /// <summary>
    /// UI controller for the Race Result page.
    /// Shows final ranking of all 8 horses with speed breakdown,
    /// bet results, and profit/loss summary.
    /// </summary>
    public class RaceResultView
    {
        private VisualElement _root;
        private Label _titleLabel;
        private Label _trackLabel;
        private VisualElement _rankingTable;
        private VisualElement _betResultsList;
        private Label _noBetsLabel;
        private Label _winningsValue;
        private Label _wageredValue;
        private Label _netProfitValue;
        private Label _balanceValue;
        private Button _continueButton;

        public event Action OnContinueClicked;

        public void Initialize(VisualElement root)
        {
            _root = root;
            _titleLabel = root.Q<Label>("title-label");
            _trackLabel = root.Q<Label>("track-label");
            _rankingTable = root.Q<VisualElement>("ranking-table");
            _betResultsList = root.Q<VisualElement>("bet-results-list");
            _noBetsLabel = root.Q<Label>("no-bets-label");
            _winningsValue = root.Q<Label>("winnings-value");
            _wageredValue = root.Q<Label>("wagered-value");
            _netProfitValue = root.Q<Label>("net-profit-value");
            _balanceValue = root.Q<Label>("balance-value");
            _continueButton = root.Q<Button>("continue-button");

            if (_continueButton != null)
                _continueButton.clicked += () => OnContinueClicked?.Invoke();
        }

        /// <summary>
        /// Populate the view with race results and settlement data.
        /// </summary>
        public void ShowResults(RaceResult raceResult, SettlementResult settlement, HorseData[] horses, TrackType track, int currentBalance)
        {
            // Track info
            if (_trackLabel != null)
                _trackLabel.text = $"Track: {track}";

            // Ranking table
            PopulateRanking(raceResult, horses);

            // Bet results
            PopulateBetResults(settlement);

            // Summary
            PopulateSummary(settlement, currentBalance);
        }

        private void PopulateRanking(RaceResult raceResult, HorseData[] horses)
        {
            if (_rankingTable == null) return;
            _rankingTable.Clear();

            for (int rank = 0; rank < raceResult.finalRanking.Length; rank++)
            {
                int horseIndex = raceResult.finalRanking[rank];
                var horse = horses[horseIndex];
                int finalSpeed = raceResult.finalSpeeds[horseIndex];

                // Calculate event modifier for this horse
                int eventMod = 0;
                if (raceResult.stageEvents != null)
                {
                    for (int s = 0; s < raceResult.stageEvents.Length; s++)
                    {
                        if (raceResult.stageEvents[s] == null) continue;
                        for (int e = 0; e < raceResult.stageEvents[s].Length; e++)
                        {
                            if (raceResult.stageEvents[s][e].horseIndex == horseIndex)
                                eventMod += raceResult.stageEvents[s][e].speedModifier;
                        }
                    }
                }

                int trackMod = finalSpeed - horse.baseSpeed - horse.hiddenBonus - eventMod;

                var row = new VisualElement();
                row.AddToClassList("ranking-row");
                if (rank == 0) row.AddToClassList("gold-row");
                else if (rank == 1) row.AddToClassList("silver-row");
                else if (rank == 2) row.AddToClassList("bronze-row");

                string medal = rank == 0 ? "🥇" : rank == 1 ? "🥈" : rank == 2 ? "🥉" : $"{rank + 1}";

                // Horse avatar image
                var avatar = new VisualElement();
                avatar.style.width = 36;
                avatar.style.height = 36;
                var horseSprite = HorseBetting.Core.SpriteLoader.LoadHorseSprite(horseIndex);
                if (horseSprite != null)
                {
                    avatar.style.backgroundImage = new StyleBackground(horseSprite);
                    avatar.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                }

                row.Add(CreateLabel(medal, "rank-col"));
                row.Add(avatar);
                row.Add(CreateLabel(horse.displayName, "horse-col"));
                row.Add(CreateLabel($"{finalSpeed}", "speed-col"));
                row.Add(CreateLabel($"+{horse.hiddenBonus}", "bonus-col"));
                row.Add(CreateLabel(FormatMod(trackMod), "track-col"));
                row.Add(CreateLabel(FormatMod(eventMod), "events-col"));

                _rankingTable.Add(row);
            }
        }

        private void PopulateBetResults(SettlementResult settlement)
        {
            if (_betResultsList == null) return;
            _betResultsList.Clear();

            if (settlement.betResults == null || settlement.betResults.Length == 0)
            {
                if (_noBetsLabel != null)
                    _noBetsLabel.RemoveFromClassList("hidden");
                return;
            }

            if (_noBetsLabel != null)
                _noBetsLabel.AddToClassList("hidden");

            foreach (var betResult in settlement.betResults)
            {
                var row = new VisualElement();
                row.AddToClassList("bet-row");

                string horsesStr = string.Join(", ", System.Array.ConvertAll(betResult.bet.selectedHorses, h => $"H{h + 1}"));
                string info = $"{FormatBetType(betResult.bet.type)} - ${betResult.bet.amount} on [{horsesStr}]";

                var infoLabel = CreateLabel(info, "bet-info");
                row.Add(infoLabel);

                if (betResult.won)
                {
                    var payoutLabel = CreateLabel($"+${betResult.payout}", "bet-payout-win");
                    row.Add(payoutLabel);
                }
                else
                {
                    var loseLabel = CreateLabel("LOSE", "bet-payout-lose");
                    row.Add(loseLabel);
                }

                _betResultsList.Add(row);
            }
        }

        private void PopulateSummary(SettlementResult settlement, int currentBalance)
        {
            if (_winningsValue != null)
                _winningsValue.text = $"${settlement.totalWinnings}";
            if (_wageredValue != null)
                _wageredValue.text = $"${settlement.totalLoss}";

            if (_netProfitValue != null)
            {
                int net = settlement.netProfit;
                _netProfitValue.text = net >= 0 ? $"+${net}" : $"-${-net}";
                _netProfitValue.RemoveFromClassList("win-color");
                _netProfitValue.RemoveFromClassList("lose-color");
                _netProfitValue.AddToClassList(net >= 0 ? "win-color" : "lose-color");
            }

            if (_balanceValue != null)
                _balanceValue.text = $"${currentBalance}";
        }

        private Label CreateLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private string FormatMod(int val)
        {
            return val >= 0 ? $"+{val}" : $"{val}";
        }

        private string FormatBetType(BetType type)
        {
            switch (type)
            {
                case BetType.SingleWin: return "Win";
                case BetType.Place: return "Place";
                case BetType.Quinella: return "Quinella";
                case BetType.Exacta: return "Exacta";
                case BetType.Trio: return "Trio";
                case BetType.Trifecta: return "Trifecta";
                default: return type.ToString();
            }
        }
    }
}
