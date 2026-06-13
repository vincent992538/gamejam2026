using HorseBetting.Data;

namespace HorseBetting.Core
{
    public interface IMessageCardSystem : IGameSystem
    {
        MessageCard RevealNextCard();
        MessageCard[] GetRevealedCards();
        int GetRemainingCardCount();
    }
}
