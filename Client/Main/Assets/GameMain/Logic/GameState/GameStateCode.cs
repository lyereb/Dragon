namespace Dragon.Logic
{
    /// <summary>
    /// 进程状态码
    /// </summary>
    public enum GameStateCode
    {
        //初始化
        Init = 0,
        //更新
        Update,
        //预加载
        PreLoad,
        //登录状态
        Login,
        //主界面状态
        Main,
        //场景切换加载
        Loading,
        //战斗状态
        Fight
    }
}
