namespace Dragon.Logic
{
    public struct GameLoadingParam
    {
        //目标状态
        public GameStateCode TargetState { get; set; }
        public float LoadingTime { get; set; }
    }
}
