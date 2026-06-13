using UnityEngine;
using HorseBetting.Config;
using HorseBetting.Systems;

namespace HorseBetting.Core
{
    /// <summary>
    /// GameEngine is the single MonoBehaviour entry point for the Horse Betting Simulator.
    /// It holds all ScriptableObject config references (injected via Inspector)
    /// and instantiates all 10 game systems in Awake().
    /// Validates: Requirements 1.2, 12.1, 14.1
    /// </summary>
    public class GameEngine : MonoBehaviour
    {
        // ─── Config References (Inspector-injected ScriptableObjects) ───────────

        [Header("Game Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private OddsConfig oddsConfig;
        [SerializeField] private MessageCardConfig messageCardConfig;
        [SerializeField] private TrackConfig trackConfig;
        [SerializeField] private AnalystConfig analystConfig;
        [SerializeField] private EventConfig eventConfig;
        [SerializeField] private ShopConfig shopConfig;
        [SerializeField] private BettingConfig bettingConfig;

        // ─── Player State ───────────────────────────────────────────────────────

        public PlayerState PlayerState { get; private set; }

        // ─── System Instances ───────────────────────────────────────────────────

        public HorseSystem HorseSystem { get; private set; }
        public MessageCardSystem MessageCardSystem { get; private set; }
        public OddsSystem OddsSystem { get; private set; }
        public TrackSystem TrackSystem { get; private set; }
        public AnalystSystem AnalystSystem { get; private set; }
        public EventSystem EventSystem { get; private set; }
        public RaceSimulationSystem RaceSimulationSystem { get; private set; }
        public BettingSystem BettingSystem { get; private set; }
        public ShopSystem ShopSystem { get; private set; }
        public SettlementSystem SettlementSystem { get; private set; }

        // ─── Round State Machine ────────────────────────────────────────────────

        public RoundStateMachine RoundStateMachine { get; private set; }

        // ─── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (!ConfigValidator.ValidateAll(this))
            {
                Debug.LogError("[GameEngine] Critical config validation failed. Disabling GameEngine.");
                enabled = false;
                return;
            }

            InitializeSystems();
            InitializeStateMachine();
        }

        /// <summary>
        /// Instantiates all 10 game systems with their respective configs.
        /// Each system is an independent module receiving only its required config.
        /// </summary>
        private void InitializeSystems()
        {
            // Initialize PlayerState with config values
            PlayerState = new PlayerState(gameConfig.startingBalance, gameConfig.maxProtectionCards);

            HorseSystem = new HorseSystem(gameConfig);
            MessageCardSystem = new MessageCardSystem(messageCardConfig);
            OddsSystem = new OddsSystem(oddsConfig);
            TrackSystem = new TrackSystem(trackConfig);
            AnalystSystem = new AnalystSystem(analystConfig);
            EventSystem = new EventSystem(eventConfig);
            RaceSimulationSystem = new RaceSimulationSystem(trackConfig);
            BettingSystem = new BettingSystem(bettingConfig);
            ShopSystem = new ShopSystem(shopConfig);
            SettlementSystem = new SettlementSystem(bettingConfig);

            // Call Initialize on each system
            HorseSystem.Initialize();
            MessageCardSystem.Initialize();
            OddsSystem.Initialize();
            TrackSystem.Initialize();
            AnalystSystem.Initialize();
            EventSystem.Initialize();
            RaceSimulationSystem.Initialize();
            BettingSystem.Initialize();
            ShopSystem.Initialize();
            SettlementSystem.Initialize();
        }

        /// <summary>
        /// Creates and configures the RoundStateMachine.
        /// </summary>
        private void InitializeStateMachine()
        {
            RoundStateMachine = new RoundStateMachine();
            RoundStateMachine.StartFirstRound();
        }

        // ─── Config Accessors (for UI binding or other systems) ─────────────────

        public GameConfig GameConfig => gameConfig;
        public OddsConfig OddsConfig => oddsConfig;
        public MessageCardConfig MessageCardConfig => messageCardConfig;
        public TrackConfig TrackConfig => trackConfig;
        public AnalystConfig AnalystConfig => analystConfig;
        public EventConfig EventConfig => eventConfig;
        public ShopConfig ShopConfig => shopConfig;
        public BettingConfig BettingConfig => bettingConfig;
    }
}
