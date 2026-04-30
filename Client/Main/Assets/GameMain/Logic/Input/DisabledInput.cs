namespace Dragon.GameInput
{
    public class DisabledInput : InputContextBase, IInputContextLifecycle
    {
        public void Enter(PlayerInputAction.PlayerInputActions actions)
        {
        }

        public void Exit(PlayerInputAction.PlayerInputActions actions)
        {
        }
    }
}
