using System;
using System.Collections.Generic;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class MessageCardSystem : IMessageCardSystem
    {
        private readonly MessageCardConfig _config;
        private readonly Random _random;
        private MessageCard[] _cards;
        private bool[] _revealed;
        private List<MessageCard> _revealedCards;

        public MessageCardSystem(MessageCardConfig config)
        {
            _config = config;
            _random = new Random();
            _revealedCards = new List<MessageCard>();
        }

        public void Initialize()
        {
            // No-op: setup handled via SetHorses
        }

        public void Reset()
        {
            _cards = null;
            _revealed = null;
            _revealedCards = new List<MessageCard>();
        }

        public void SetHorses(HorseData[] horses)
        {
            int count = horses.Length;
            _cards = new MessageCard[count];
            _revealed = new bool[count];
            _revealedCards = new List<MessageCard>();

            for (int i = 0; i < count; i++)
            {
                _cards[i] = new MessageCard
                {
                    horseIndex = horses[i].index,
                    description = LookupDescription(horses[i].hiddenBonus)
                };
                _revealed[i] = false;
            }
        }

        public MessageCard RevealNextCard()
        {
            if (_cards == null || GetRemainingCardCount() == 0)
                throw new InvalidOperationException("No cards remaining to reveal.");

            // Collect indices of unrevealed cards
            List<int> unrevealed = new List<int>();
            for (int i = 0; i < _cards.Length; i++)
            {
                if (!_revealed[i])
                    unrevealed.Add(i);
            }

            // Randomly select one
            int pick = _random.Next(unrevealed.Count);
            int cardIndex = unrevealed[pick];

            _revealed[cardIndex] = true;
            _revealedCards.Add(_cards[cardIndex]);

            return _cards[cardIndex];
        }

        public MessageCard[] GetRevealedCards()
        {
            return _revealedCards.ToArray();
        }

        public int GetRemainingCardCount()
        {
            if (_cards == null)
                return 0;

            int count = 0;
            for (int i = 0; i < _revealed.Length; i++)
            {
                if (!_revealed[i])
                    count++;
            }
            return count;
        }

        private string LookupDescription(int hiddenBonus)
        {
            if (_config.entries != null)
            {
                for (int i = 0; i < _config.entries.Length; i++)
                {
                    if (_config.entries[i].hiddenSpeedBonus == hiddenBonus)
                        return _config.entries[i].description;
                }
            }

            return $"Hidden bonus: {hiddenBonus}";
        }
    }
}
