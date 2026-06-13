# Requirements Document

## Introduction

賽馬投注模擬器是一款單人策略型賽馬投注網頁遊戲。玩家透過有限資訊、分析師情報以及賽道條件推測馬匹實力，並在比賽開始前進行多輪下注。遊戲核心樂趣來自資訊不完全下的推理、風險管理與資金成長。所有馬匹、消息卡、分析師情報、賽道效果與隨機事件皆由系統依據 Config 設定生成。玩家目標是在多場賽事中持續獲利並累積資金。

技術堆疊：Unity (C#)，單機遊戲，無後端資料庫。所有可調整數值皆由 ScriptableObject 或 JSON Config 檔案管理。

## Glossary

- **Game_Engine**: 賽馬投注模擬器的核心遊戲引擎，負責協調各子系統並管理遊戲回合流程
- **Horse_System**: 負責馬匹生成、基礎速度與隱藏加成分配的子系統
- **Message_Card_System**: 負責將隱藏速度加成轉換為模糊文字資訊並分批揭露給玩家的子系統
- **Odds_System**: 負責計算與動態更新賠率的子系統
- **Track_System**: 負責賽道類型選擇與馬匹賽道偏好修正的子系統
- **Analyst_System**: 負責產生付費分析師情報的子系統
- **Event_System**: 負責隨機事件生成與觸發判定的子系統
- **Race_Simulation_System**: 負責執行三階段賽事模擬並計算最終名次的子系統
- **Betting_System**: 負責管理玩家投注類型、金額與驗證的子系統
- **Shop_System**: 負責賽後商店與防禦卡管理的子系統
- **Settlement_System**: 負責賽後結算、獎金派發與資金更新的子系統
- **Config**: 外部設定檔案，用於管理所有可調整數值，包含賠率倍率、分析師價格與正確率、消息卡內容、事件資料、商店商品等
- **Hidden_Speed_Bonus**: 每場賽事隨機分配給八匹馬的隱藏速度加成值（+0 至 +7，不重複）
- **Message_Card**: 將 Hidden_Speed_Bonus 轉換為模糊文字描述的資訊卡
- **Protection_Card**: 玩家可購買的防禦卡，用於抵消特定隨機事件的負面效果
- **Track_Type**: 賽道類型，包含 Grass（草地）、Mud（泥地）、Snow（雪地）
- **Track_Modifier**: 根據馬匹對特定賽道的偏好所給予的速度修正值
- **Random_Event**: 賽事中可能發生的隨機事件，包含觸發機率與速度修正值
- **Final_Speed**: 馬匹最終速度，計算公式為 Base_Speed + Hidden_Speed_Bonus + Track_Modifier + 各階段 Event_Modifier 總和
- **Base_Speed**: 所有馬匹共用的基礎速度值，固定為 30
- **Betting_Round**: 玩家可進行投注的階段，每回合共有三次下注機會
- **Player_Balance**: 玩家當前持有的資金總額

## Requirements

### Requirement 1: 遊戲回合流程管理

**User Story:** 作為玩家，我希望遊戲按照固定順序執行每回合流程，以便我能在適當時機獲取資訊並做出投注決策。

#### Acceptance Criteria

1. WHEN 新回合開始時, THE Game_Engine SHALL 依照以下固定順序執行 21 個回合步驟：(1)產生馬匹能力 → (2)計算初始賠率 → (3)發放第一張消息卡 → (4)發放第二張消息卡 → (5)第一次下注 → (6)發放第三張消息卡 → (7)更新賠率 → (8)第二次下注 → (9)決定賽道條件 → (10)產生意外事件 → (11)產生分析師情報 → (12)開放購買分析師情報 → (13)第三次下注 → (14)開始比賽 → (15)公布賽道條件 → (16)執行賽事動畫 → (17)三階段判定事件 → (18)產生最終名次 → (19)派發獎金 → (20)開放商店 → (21)開始下一回合
2. THE Game_Engine SHALL 確保每個步驟完成後才進入下一步驟
3. WHEN 回合結束後, THE Game_Engine SHALL 自動開始下一回合

### Requirement 2: 馬匹系統

**User Story:** 作為玩家，我希望每場比賽有八匹馬參賽，且每匹馬具有隱藏的速度差異，以便我可以透過資訊推理出實力差距。

#### Acceptance Criteria

1. THE Horse_System SHALL 每場比賽產生固定 8 匹馬（Horse 1 至 Horse 8）
2. THE Horse_System SHALL 設定所有馬匹的 Base_Speed 為 30
3. WHEN 新賽事開始時, THE Horse_System SHALL 將 +0、+1、+2、+3、+4、+5、+6、+7 的 Hidden_Speed_Bonus 隨機分配給八匹馬，每個數值只出現一次且每匹馬只取得一個加成
4. THE Horse_System SHALL 確保玩家無法直接查看任何馬匹的 Hidden_Speed_Bonus 實際數值
5. THE Horse_System SHALL 以 2D 精靈圖（Sprite）表示馬匹，並支援未來替換為任意圖片

### Requirement 3: 消息卡系統

**User Story:** 作為玩家，我希望收到關於馬匹狀態的模糊資訊，以便我能推測哪匹馬的隱藏加成較高。

#### Acceptance Criteria

1. THE Message_Card_System SHALL 根據 Config 中定義的對應表，將每匹馬的 Hidden_Speed_Bonus 轉換為一張 Message_Card
2. WHEN 第一輪消息卡發放時, THE Message_Card_System SHALL 從八張 Message_Card 中隨機選取一張揭露給玩家
3. WHEN 第二輪消息卡發放時, THE Message_Card_System SHALL 從剩餘七張 Message_Card 中隨機選取一張揭露給玩家
4. WHEN 第三輪消息卡發放時, THE Message_Card_System SHALL 從剩餘六張 Message_Card 中隨機選取一張揭露給玩家
5. THE Message_Card_System SHALL 確保玩家每回合總共收到三張 Message_Card
6. THE Message_Card_System SHALL 從 Config 讀取 Hidden_Speed_Bonus 與文字描述的對應關係
7. THE Message_Card_System SHALL 支援管理者透過 Config 自由編輯消息卡文字內容

### Requirement 4: 賠率系統

**User Story:** 作為玩家，我希望能看到每匹馬的賠率，且賠率會隨著下注階段動態調整，以便我在最佳時機下注獲取最高報酬。

#### Acceptance Criteria

1. WHEN 馬匹能力產生後, THE Odds_System SHALL 根據 Final_Score（Base_Speed + Hidden_Speed_Bonus）排序計算初始賠率
2. THE Odds_System SHALL 從 Config 讀取賠率計算公式與參數
3. WHEN 玩家完成第一次下注後, THE Odds_System SHALL 重新計算並更新賠率，使賠率變差
4. WHEN 玩家完成第二次下注後, THE Odds_System SHALL 再次重新計算並更新賠率，使賠率進一步變差
5. WHILE 第三次下注階段, THE Odds_System SHALL 提供最差的賠率給玩家
6. THE Odds_System SHALL 確保賠率計算邏輯完全由 Config 管理，不得寫死於程式碼中

### Requirement 5: 賽道系統

**User Story:** 作為玩家，我希望賽道類型在比賽開始才公布，讓每場比賽多一層不確定性，增加推理與策略深度。

#### Acceptance Criteria

1. THE Track_System SHALL 支援三種 Track_Type：Grass（草地）、Mud（泥地）、Snow（雪地）
2. WHEN 賽道條件決定步驟執行時, THE Track_System SHALL 從三種 Track_Type 中隨機選取一種作為本場賽道
3. WHILE 下注階段, THE Track_System SHALL 隱藏賽道類型，不向玩家揭露
4. WHEN 比賽開始時, THE Track_System SHALL 公開本場賽道類型
5. THE Track_System SHALL 根據 Config 中定義的馬匹賽道偏好表，為每匹馬計算 Track_Modifier
6. THE Track_System SHALL 從 Config 讀取每匹馬對每種賽道的偏好修正值

### Requirement 6: 分析師系統

**User Story:** 作為玩家，我希望能付費購買分析師提供的額外情報，以便在資訊不足時獲得更多參考依據（但情報不一定正確）。

#### Acceptance Criteria

1. THE Analyst_System SHALL 支援兩種分析師類型：Senior Analyst（資深分析師）與 Junior Analyst（初級分析師）
2. THE Analyst_System SHALL 確保 Senior Analyst 的價格高於 Junior Analyst
3. THE Analyst_System SHALL 確保 Senior Analyst 的正確率高於 Junior Analyst 的正確率
4. WHEN 分析師情報產生時, THE Analyst_System SHALL 根據分析師正確率決定每條情報為真實資訊或誤導資訊
5. WHEN 玩家選擇購買分析師情報時, THE Analyst_System SHALL 扣除對應價格並揭露該分析師的情報內容
6. THE Analyst_System SHALL 從 Config 讀取分析師價格與正確率參數
7. IF 玩家資金不足以購買分析師情報, THEN THE Analyst_System SHALL 阻止購買並顯示資金不足提示

### Requirement 7: 隨機事件系統

**User Story:** 作為玩家，我希望比賽中會發生隨機事件影響賽果，以便增加比賽的變數與觀賞樂趣。

#### Acceptance Criteria

1. THE Event_System SHALL 從 Config 讀取所有事件資料，每個事件包含：名稱、描述、觸發機率、影響目標、速度修正值
2. WHEN 賽事每個階段判定事件時, THE Event_System SHALL 針對每匹馬獨立判定每個事件是否觸發
3. WHEN 事件觸發時, THE Event_System SHALL 將該事件的速度修正值套用至目標馬匹
4. THE Event_System SHALL 支援管理者透過 Config 新增、修改或移除事件
5. WHEN 玩家持有對應的 Protection_Card 且事件觸發時, THE Event_System SHALL 根據 Protection_Card 的成功率判定是否抵消該事件效果

### Requirement 8: 賽事模擬系統

**User Story:** 作為玩家，我希望看到賽事分三階段進行並產生最終名次，以便體驗完整的比賽過程。

#### Acceptance Criteria

1. THE Race_Simulation_System SHALL 將賽事分為三個階段（Stage 1、Stage 2、Stage 3）依序執行
2. WHEN 每個階段執行時, THE Race_Simulation_System SHALL 獨立判定隨機事件是否發生並套用效果
3. WHEN 三階段全部完成後, THE Race_Simulation_System SHALL 計算每匹馬的 Final_Speed，公式為：Base_Speed + Hidden_Speed_Bonus + Track_Modifier + Stage_1_Event_Modifier + Stage_2_Event_Modifier + Stage_3_Event_Modifier
4. THE Race_Simulation_System SHALL 依據 Final_Speed 由高至低排列產生最終名次
5. IF 兩匹或多匹馬的 Final_Speed 相同, THEN THE Race_Simulation_System SHALL 以較低的馬匹編號優先作為同分判定規則（例如 Horse 2 優先於 Horse 5）
6. THE Race_Simulation_System SHALL 提供 2D 橫向卷軸賽事動畫展示八匹馬的移動位置與賽事進度

### Requirement 9: 投注系統

**User Story:** 作為玩家，我希望能選擇多種投注類型並在三個不同時機下注，以便靈活運用策略最大化獲利。

#### Acceptance Criteria

1. THE Betting_System SHALL 支援以下投注類型：Single Win（預測第一名）、Place（預測進前三名）、Quinella（預測前兩名不分順序）、Exacta（預測前兩名順序正確）、Trio（預測前三名不分順序）、Trifecta（預測前三名順序正確）
2. THE Betting_System SHALL 從 Config 讀取每種投注類型的賠率倍數
3. WHEN 玩家提交投注時, THE Betting_System SHALL 驗證投注金額不超過玩家當前 Player_Balance
4. IF 玩家投注金額超過 Player_Balance, THEN THE Betting_System SHALL 拒絕該筆投注並顯示餘額不足提示
5. THE Betting_System SHALL 允許玩家在每個 Betting_Round 中對不同馬匹進行多筆投注
6. WHEN 玩家確認投注時, THE Betting_System SHALL 立即從 Player_Balance 扣除投注金額

### Requirement 10: 商店系統

**User Story:** 作為玩家，我希望在賽後能購買防禦卡來降低未來比賽中不利事件的影響，以便提升長期資金管理能力。

#### Acceptance Criteria

1. WHEN 賽事結算完成後, THE Shop_System SHALL 開放商店供玩家購買 Protection_Card
2. THE Shop_System SHALL 從 Config 讀取商店商品清單，每張 Protection_Card 包含：名稱、防禦的事件類型、成功率、價格
3. THE Shop_System SHALL 限制玩家同時持有的 Protection_Card 數量上限為 3 張
4. IF 玩家已持有 3 張 Protection_Card, THEN THE Shop_System SHALL 阻止購買並顯示持有上限提示
5. IF 玩家資金不足以購買商品, THEN THE Shop_System SHALL 阻止購買並顯示資金不足提示
6. WHEN 玩家成功購買 Protection_Card 時, THE Shop_System SHALL 從 Player_Balance 扣除對應價格並將卡片加入玩家持有清單

### Requirement 11: 結算系統

**User Story:** 作為玩家，我希望比賽結束後能清楚看到完整結算資訊，以便了解本場的盈虧狀況。

#### Acceptance Criteria

1. WHEN 賽事名次確定後, THE Settlement_System SHALL 公布所有八匹馬的完整名次
2. WHEN 結算開始時, THE Settlement_System SHALL 顯示每匹馬的 Final_Speed 分解（Base_Speed、Hidden_Speed_Bonus、Track_Modifier、各階段事件修正）
3. THE Settlement_System SHALL 逐筆計算玩家所有投注的勝負結果
4. WHEN 投注命中時, THE Settlement_System SHALL 根據該投注類型的 Config 賠率倍數計算獎金（投注金額 × 賠率倍數）
5. THE Settlement_System SHALL 依據同分判定規則（較低馬匹編號優先）確定唯一名次，不產生並列情況
6. THE Settlement_System SHALL 將所有獎金加入 Player_Balance
7. THE Settlement_System SHALL 顯示本場總獲利或總虧損金額
8. WHEN 結算完成後, THE Settlement_System SHALL 提供進入商店的入口

### Requirement 12: Config 管理系統

**User Story:** 作為管理者，我希望所有可調整的遊戲數值皆由 Config 檔案管理，以便無需修改程式碼即可調整遊戲平衡。

#### Acceptance Criteria

1. THE Game_Engine SHALL 從 Config 讀取以下所有設定：賠率計算參數、分析師價格、分析師正確率、消息卡文字內容與對應關係、隨機事件資料、商店商品清單、Protection_Card 成功率、賽道偏好修正值、投注類型賠率倍數
2. THE Game_Engine SHALL 確保所有上述數值不寫死於程式碼中
3. WHEN Config 內容變更時, THE Game_Engine SHALL 在下一回合開始時載入最新 Config 設定
4. THE Game_Engine SHALL 在啟動時驗證 Config 格式正確性，確保所有必要欄位皆存在

### Requirement 13: 使用者介面

**User Story:** 作為玩家，我希望有清晰直覺的介面來查看遊戲資訊並進行操作，以便流暢地體驗完整遊戲流程。

#### Acceptance Criteria

1. THE Game_Engine SHALL 在主畫面顯示：玩家資金、馬匹列表、當前賠率、已持有的 Protection_Card
2. THE Game_Engine SHALL 在投注畫面顯示：馬匹資訊、已揭露的 Message_Card 內容、可用投注類型、投注金額輸入、確認下注按鈕
3. THE Game_Engine SHALL 在賽事畫面顯示：賽道動畫、八匹馬移動位置、賽事進度條、事件觸發通知
4. THE Game_Engine SHALL 在結果畫面顯示：最終名次、投注結果、資金變化、商店入口
5. THE Game_Engine SHALL 使用 Unity UI Toolkit 建構 HUD、投注、結算與商店介面，並使用 Unity 2D Sprite 系統建構賽事動畫畫面
6. THE Game_Engine SHALL 採用 2D 橫向卷軸視角呈現賽事，馬匹以 2D Sprite 表示
7. THE Game_Engine SHALL 採用模組化設計，每個子系統獨立封裝為可維護的模組

### Requirement 14: 模組化架構

**User Story:** 作為開發者，我希望系統採用模組化架構設計，以便每個子系統可獨立開發、測試與維護。

#### Acceptance Criteria

1. THE Game_Engine SHALL 將以下系統獨立封裝為模組：Horse_System、Odds_System、Betting_System、Analyst_System、Track_System、Event_System、Race_Simulation_System、Shop_System、Settlement_System
2. THE Game_Engine SHALL 確保每個模組具有明確定義的介面（Input/Output）
3. THE Game_Engine SHALL 確保模組間透過定義好的介面溝通，降低耦合度
4. THE Game_Engine SHALL 使用 C# 強型別與介面（Interface）確保模組間資料傳遞的正確性
