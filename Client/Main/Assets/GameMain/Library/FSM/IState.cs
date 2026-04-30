namespace Dragon.Library
{
    public interface IState
    {
        //状态ID
        int Id { get; }
        //名字
        string Name { get; }

        //进入参数对象
        object Data { get; }

        //状态机
        FiniteStateMachine FSM { get; set; }
        //进入状态
        bool Enter(object data);
        //退出状态
        bool Exit();
        //更新
        bool Update(float dt);

        //尝试切换状态
        bool IsCanTransTo(int id);

        //状态是否有效
        bool IsValid();

        //销毁
        void Destroy();
    }
}
