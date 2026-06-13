# Implementation Plan: Horse Betting Simulator

## Overview

Implement a modular horse betting simulator in Unity (C#) with 10 game systems, ScriptableObject-driven configuration, UI Toolkit for UI, and a 21-step round state machine. Each system is a plain C# class behind an interface, orchestrated by a single GameEngine MonoBehaviour.

## Tasks

- [x] 1. Set up project structure, core interfaces, and data models
  - [x] 1.1 Create folder structure and assembly definitions
    - Create `Assets/Scripts/Core/`, `Assets/Scripts/Systems/`, `Assets/Scripts/Config/`, `Assets/Scripts/UI/`, `Assets/Scripts/Data/`
    - Create `Assets/Tests/EditMode/`, `Assets/Tests/EditMode/Properties/`, `Assets/Tests/PlayMode/`
    - Add assembly definition files for main code and test assemblies
    - Add FsCheck and FsCheck.NUnit NuGet packages to the project for property-based testing
    - _Requirements: 14.1, 14.2_

  - [x] 1.2 Define IGameSystem interface and all system interfaces
    - Create `IGameSystem` with `Initialize()` and `Reset()` methods
    - Create `IHorseSystem`, `IMessageCardSystem`, `IOddsSystem`, `ITrackSystem`, `IAnalystSystem`, `IEventSystem`, `IRaceSimulationSystem`, `IBettingSystem`, `IShopSystem`, `ISettlementSystem` as defined in the design
    - _Requirements: 14.2, 14.3, 14.4_

  - [x] 1.3 Define all enums and data structs
    - Create `TrackType`, `BetType`, `AnalystType` enums
    - Create all structs: `HorseData`, `MessageCard`, `Bet`, `BetResult`, `StageEventResult`, `RaceResult`, `SettlementResult`, `BetSettlement`, `AnalystIntel`, `PurchaseResult`, `ProtectionCard`, `ShopItem`
    - _Requirements: 2.1, 5.1, 9.1, 6.1_

  - [x] 1.4 Create all ScriptableObject config classes
    - Create `GameConfig`, `OddsConfig`, `MessageCardConfig`, `TrackConfig`, `AnalystConfig`, `EventConfig`, `ShopConfig`, `BettingConfig` ScriptableObjects with `[CreateAssetMenu]` attributes
    - Create serializable helper classes: `MessageCardEntry`, `TrackPreference`, `RaceEvent`, `ProtectionCardData`, `BetTypeConfig`
    - _Requirements: 12.1, 12.2_

  - [x] 1.5 Create ScriptableObject asset instances with default values
    - Create asset instances in `Assets/Resources/Config/` for each ScriptableObject type
    - Populate with default values from the design document (baseSpeed=30, startingBalance=1000, etc.)
    - _Requirements: 12.1, 12.3_

- [x] 2. Implement HorseSystem and MessageCardSystem
  - [x] 2.1 Implement HorseSystem
    - Implement `IHorseSystem` as a plain C# class accepting `GameConfig`
    - `GenerateHorses()`: create 8 horses with baseSpeed=30, shuffle {0..7} for hiddenBonus
    - `GetHiddenBonus(int horseIndex)`: return the hidden bonus for a given horse
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ]* 2.2 Write property test for HorseSystem - Horse Generation Invariant
    - **Property 1: Horse Generation Invariant**
    - Test that for any random seed, GenerateHorses produces exactly 8 horses with baseSpeed=30 and hiddenBonus values forming a permutation of {0,1,2,3,4,5,6,7}
    - **Validates: Requirements 2.1, 2.2, 2.3**

  - [x] 2.3 Implement MessageCardSystem
    - Implement `IMessageCardSystem` as a plain C# class accepting `MessageCardConfig`
    - `RevealNextCard()`: randomly select from unrevealed cards, track revealed state
    - `GetRevealedCards()`: return all revealed cards so far
    - Map hiddenBonus to description via config lookup
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

  - [ ]* 2.4 Write property tests for MessageCardSystem
    - **Property 2: Message Card Reveal Uniqueness** - 3 revealed cards are distinct, each from original 8, no duplicates
    - **Property 3: Message Card Config Mapping** - description matches config entry for given bonus value
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

- [x] 3. Implement OddsSystem and TrackSystem
  - [x] 3.1 Implement OddsSystem
    - Implement `IOddsSystem` as a plain C# class accepting `OddsConfig`
    - `CalculateOdds()`: sort horses by Final_Score, assign odds from config rankOdds array
    - `UpdateOddsAfterBetting()`: apply round2Penalty (0.8×) or round3Penalty (0.6×) multiplier
    - Ensure odds degrade each round (lower payout for later rounds)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [ ]* 3.2 Write property tests for OddsSystem
    - **Property 4: Odds Strength Ordering** - highest Final_Score gets lowest odds, lowest gets highest
    - **Property 5: Odds Monotonic Degradation** - odds at round N+1 strictly less favorable than round N
    - **Validates: Requirements 4.1, 4.3, 4.4, 4.5**

  - [x] 3.3 Implement TrackSystem
    - Implement `ITrackSystem` as a plain C# class accepting `TrackConfig`
    - `SelectTrack()`: randomly choose from Grass/Mud/Snow
    - `GetTrackModifier()`: look up config for (horseIndex, trackType) pair
    - Expose `CurrentTrack` property
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [ ]* 3.4 Write property test for TrackSystem
    - **Property 6: Track Modifier Config Lookup** - GetTrackModifier returns exact value from TrackConfig for any valid (horseIndex, trackType)
    - **Validates: Requirements 5.5**

- [x] 4. Implement AnalystSystem and EventSystem
  - [x] 4.1 Implement AnalystSystem
    - Implement `IAnalystSystem` as a plain C# class accepting `AnalystConfig`
    - `GenerateIntel()`: for each analyst type, generate intel with accuracy-based truth/false determination
    - `BuyIntel()`: validate balance, deduct price, return intel content
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

  - [ ]* 4.2 Write property tests for AnalystSystem
    - **Property 7: Analyst Pricing Hierarchy** - Senior price > Junior price AND Senior accuracy > Junior accuracy
    - **Property 8: Purchase Balance Deduction** - balance >= price → success with balance - price; balance < price → fail with unchanged balance
    - **Validates: Requirements 6.2, 6.3, 6.5, 6.7**

  - [x] 4.3 Implement EventSystem
    - Implement `IEventSystem` as a plain C# class accepting `EventConfig`
    - `ProcessStageEvents()`: iterate each horse, for each event check triggerChance, apply speedModifier
    - Handle Protection_Card cancellation: if matching card held with successRate check, cancel event
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [ ]* 4.4 Write property test for EventSystem
    - **Property 9: Event Application Correctness** - triggered event applies exact configured speedModifier; matching ProtectionCard with successRate 1.0 fully cancels event
    - **Validates: Requirements 7.3, 7.5**

- [x] 5. Checkpoint - Core systems verification
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement RaceSimulationSystem
  - [x] 6.1 Implement RaceSimulationSystem
    - Implement `IRaceSimulationSystem` as a plain C# class accepting `EventConfig` and `GameConfig`
    - `SimulateRace()`: execute 3 stages of event processing, calculate Final_Speed per horse
    - `GetFinalRanking()`: sort by Final_Speed descending, break ties by lower horse index
    - Final_Speed = baseSpeed + hiddenBonus + trackModifier + stage1Mod + stage2Mod + stage3Mod
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

  - [ ]* 6.2 Write property tests for RaceSimulationSystem
    - **Property 10: Final Speed Formula** - computed Final_Speed equals sum of all components
    - **Property 11: Ranking Correctness** - ranking sorted by Final_Speed desc, ties broken by lower index, all 8 horses present, no duplicates
    - **Validates: Requirements 8.3, 8.4, 8.5**

- [x] 7. Implement BettingSystem
  - [x] 7.1 Implement BettingSystem
    - Implement `IBettingSystem` as a plain C# class accepting `BettingConfig`
    - `PlaceBet()`: validate amount ≤ balance, deduct from balance, store bet with current odds
    - `GetActiveBets()`: return all bets for current round
    - `ClearBets()`: reset bets after settlement
    - Support all 6 bet types: SingleWin, Place, Quinella, Exacta, Trio, Trifecta
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

  - [ ]* 7.2 Write property tests for BettingSystem
    - **Property 12: Bet Validation** - amount ≤ balance → accepted with new balance = balance - amount; amount > balance → rejected, balance unchanged
    - **Property 15: Bet Type Win Conditions** - verify all 6 bet type win condition formulas against random rankings
    - **Validates: Requirements 9.1, 9.3, 9.4, 9.6**

- [x] 8. Implement ShopSystem and SettlementSystem
  - [x] 8.1 Implement ShopSystem
    - Implement `IShopSystem` as a plain C# class accepting `ShopConfig`
    - `GetAvailableItems()`: return shop items with canAfford flag
    - `BuyProtectionCard()`: validate card count < 3 and balance >= price, deduct balance, add card
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

  - [ ]* 8.2 Write property test for ShopSystem
    - **Property 13: Protection Card Limit** - player with 3 cards → purchase rejected; player with <3 cards and sufficient balance → purchase succeeds, count increases by 1
    - **Validates: Requirements 10.3, 10.4, 10.6**

  - [x] 8.3 Implement SettlementSystem
    - Implement `ISettlementSystem` as a plain C# class accepting `BettingConfig`
    - `CalculateSettlement()`: evaluate each bet against final ranking using bet type win conditions
    - Winning bet payout = amount × oddsMultiplier
    - Calculate totalWinnings, totalLoss, netProfit
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7_

  - [ ]* 8.4 Write property test for SettlementSystem
    - **Property 14: Settlement Arithmetic** - winning bets receive payout = amount × odds, netProfit = sum(payouts) - sum(bet amounts)
    - **Validates: Requirements 11.3, 11.4, 11.6, 11.7**

- [x] 9. Checkpoint - All systems implemented
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Implement GameEngine and Round State Machine
  - [x] 10.1 Create GameEngine MonoBehaviour
    - Create `GameEngine` as the single MonoBehaviour entry point
    - Inject all ScriptableObject configs via Inspector references
    - Instantiate all 10 system classes in `Awake()`, passing configs
    - Expose system references for UI binding
    - _Requirements: 1.2, 12.1, 14.1_

  - [x] 10.2 Implement RoundStateMachine with 21 steps
    - Define enum `RoundStep` with all 21 steps from the design
    - Implement step execution logic: each step calls appropriate system methods
    - Enforce sequential execution: step N must complete before step N+1 begins
    - Implement `AdvanceStep()` to transition between steps
    - Handle round completion → auto-start next round
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 10.3 Implement PlayerState management
    - Create `PlayerState` class to hold: balance, protectionCards list, current bets
    - Initialize with startingBalance from GameConfig
    - Wire balance deductions/additions through BettingSystem, ShopSystem, SettlementSystem
    - _Requirements: 9.6, 10.6, 11.6_

  - [x] 10.4 Implement Config validation on startup
    - Validate all config assets are assigned (not null)
    - Validate MessageCardConfig has exactly 8 entries
    - Validate EventConfig triggerChance values in [0,1] (clamp if invalid)
    - Validate AnalystConfig prices and accuracy (no negatives, accuracy ≤ 1.0)
    - Log warnings/errors per error handling strategy
    - _Requirements: 12.3, 12.4_

- [x] 11. Implement UI Layer with UI Toolkit
  - [x] 11.1 Create MainView (HUD and horse list)
    - Create UXML layout for main game screen showing: player balance, horse list with odds, held Protection_Cards
    - Create USS stylesheet for layout and theming
    - Bind data from GameEngine/PlayerState to UI elements
    - _Requirements: 13.1_

  - [x] 11.2 Create BettingView
    - Create UXML layout for betting screen: horse info, revealed Message_Cards, bet type selector, amount input, confirm button
    - Implement bet type dropdown/selection with all 6 types
    - Wire confirm button to BettingSystem.PlaceBet()
    - Show validation errors (insufficient balance)
    - _Requirements: 13.2_

  - [x] 11.3 Create RaceView with 2D animation
    - Create a 2D race scene using Unity Sprite system: horizontal scrolling track, 8 horse sprites in lanes
    - Implement 2D position-based animation driven by Final_Speed data (horses move left-to-right)
    - Display event trigger notifications during race stages
    - Use simple placeholder sprites (colored rectangles) with support for future sprite replacement
    - _Requirements: 13.3, 13.6, 8.6_

  - [x] 11.4 Create SettlementView
    - Create UXML layout for results screen: final ranking table, speed breakdown per horse, bet results list, net profit/loss display
    - Wire "Go to Shop" button to transition to ShopView
    - _Requirements: 13.4, 11.1, 11.2, 11.7, 11.8_

  - [x] 11.5 Create ShopView
    - Create UXML layout for shop screen: available Protection_Cards list with prices, buy buttons, current card count, "Start Next Round" button
    - Wire buy buttons to ShopSystem.BuyProtectionCard()
    - Show validation errors (card limit, insufficient funds)
    - _Requirements: 10.1, 10.4, 10.5, 13.4_

  - [x] 11.6 Create AnalystView
    - Create UXML layout for analyst purchase screen: Senior/Junior analyst options with prices, buy buttons, revealed intel display
    - Wire purchase buttons to AnalystSystem.BuyIntel()
    - Show validation errors (insufficient funds)
    - _Requirements: 6.5, 6.7, 13.2_

- [x] 12. Checkpoint - UI layer complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 13. Integration and wiring
  - [x] 13.1 Wire GameEngine step transitions to UI view switching
    - Implement view switching logic: show BettingView during betting rounds, RaceView during race, SettlementView after ranking, ShopView after settlement
    - Connect state machine step changes to UI transitions
    - Handle user input gates (betting confirmation, shop exit, analyst purchase)
    - _Requirements: 1.1, 1.2, 13.1, 13.2, 13.3, 13.4_

  - [x] 13.2 Wire all system outputs to UI data binding
    - Connect HorseSystem output to horse list display
    - Connect OddsSystem output to odds display and real-time updates
    - Connect MessageCardSystem reveals to card display
    - Connect RaceSimulationSystem results to animation and ranking
    - Connect SettlementSystem results to settlement view
    - _Requirements: 13.1, 13.2, 13.3, 13.4_

  - [x] 13.3 Implement full round flow end-to-end
    - Test manually: start round → generate horses → cards → betting → race → settlement → shop → next round
    - Ensure balance carries across rounds
    - Ensure Protection_Cards persist across rounds until used
    - _Requirements: 1.1, 1.2, 1.3_

- [x] 14. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- All systems are plain C# classes (not MonoBehaviour) for testability — only GameEngine is a MonoBehaviour
- ScriptableObject assets should be created in `Assets/Resources/Config/` for easy loading
- UI Toolkit UXML/USS files go in `Assets/UI/`
- FsCheck is used for property-based testing via NUnit in Unity Test Framework Edit Mode

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["1.4"] },
    { "id": 3, "tasks": ["1.5"] },
    { "id": 4, "tasks": ["2.1", "3.1", "3.3"] },
    { "id": 5, "tasks": ["2.2", "2.3", "3.2", "3.4", "4.1", "4.3"] },
    { "id": 6, "tasks": ["2.4", "4.2", "4.4", "6.1"] },
    { "id": 7, "tasks": ["6.2", "7.1"] },
    { "id": 8, "tasks": ["7.2", "8.1", "8.3"] },
    { "id": 9, "tasks": ["8.2", "8.4"] },
    { "id": 10, "tasks": ["10.1"] },
    { "id": 11, "tasks": ["10.2", "10.3", "10.4"] },
    { "id": 12, "tasks": ["11.1", "11.2", "11.3", "11.4", "11.5", "11.6"] },
    { "id": 13, "tasks": ["13.1", "13.2"] },
    { "id": 14, "tasks": ["13.3"] }
  ]
}
```
