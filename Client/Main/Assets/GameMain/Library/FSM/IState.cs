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
        void Enter(object data);
        //退出状态
        void Exit();
        //更新
        void Update(float dt);

        //尝试切换状态
        bool IsCanTransTo(int id);

        //销毁
        void Destroy();
    }
}
