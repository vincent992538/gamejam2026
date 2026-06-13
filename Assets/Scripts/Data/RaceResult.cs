namespace HorseBetting.Data
{
    public struct RaceResult
    {
        public int[] finalRanking;                // 名次排序 (horseIndex)
        public int[] finalSpeeds;                 // 每匹馬最終速度
        public StageEventResult[][] stageEvents;  // 三階段事件結果
    }
}
