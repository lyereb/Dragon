using UnityEngine;

namespace Dragon.Library
{
    public class TerminalCommandExecuter
    {
        /// <returns>为 true 时不向终端追加成功反馈行（用于 clear 等）。</returns>
        /// <param name="customSuccessMessage">非 null 时由 <see cref="TerminalManager.ProcessInput"/> 作为反馈内容（可多行），不再使用注册表里的成功文案。</param>
        public bool ExecuteCommand(string command, string[] args, out string customSuccessMessage)
        {
            customSuccessMessage = null;
            switch (command.ToLowerInvariant())
            {
                case "clear":
                    TerminalManager.Instance.ClearTerminalHistory();
                    return true;
                case "ls":
                    customSuccessMessage = TerminalManager.Instance.BuildRegisteredCommandsListText();
                    return false;
                case "test":
                    CustomTest(args);
                    return false;
                default:
                    Debug.LogWarning($"[Terminal] Unimplemented command: {command}");
                    return false;
            }
        }

        private void CustomTest(string[] args)
        {
            //TODO: Custom Test Command
        }
    }
}