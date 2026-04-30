using System;
using UnityEngine;
using System.Collections.Generic;
using Dragon.GameUI;


namespace Dragon.Library
{
    public class TerminalManager
    {
        #region //静态变量

        //静态变量
        protected static TerminalManager _instance = null;

        //静态属性
        public static TerminalManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TerminalManager();
                }
                return _instance;
            }
        }
        #endregion

        private TerminalCommandExecuter _terminalCommandExecuter = null;
        private PopupTerminal _terminalPopup = null;
        private bool _isTerminalOpen = false;
        private List<string> _savedHistory;
        private struct CommandRegistration
        {
            public int ArgCount;
            public string SuccessMessageFormat;
        }

        private readonly Dictionary<string, CommandRegistration> _registeredCommands =
            new Dictionary<string, CommandRegistration>(StringComparer.OrdinalIgnoreCase);

        public void Init()
        {
            _terminalCommandExecuter = new TerminalCommandExecuter();
            _savedHistory = new List<string>();
            _registeredCommands.Clear();

            RegisterCommand("clear", 0, "Clear Terminal History");
            RegisterCommand("ls", 0, "List Registered Commands");
            RegisterCommand("test", 3, "Custom Test Command arg1:{0}, arg2:{1}, arg3:{2}");
        }

        /// <summary>
        /// 注册终端命令：argumentCount 为按空格拆分后的参数个数；successMessageFormat 为成功时显示的文案（支持 string.Format，占位符 {0}、{1}…对应各参数）。
        /// </summary>
        public void RegisterCommand(string commandName, int argumentCount, string successMessageFormat)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                return;
            }

            if (argumentCount < 0)
            {
                argumentCount = 0;
            }

            _registeredCommands[commandName.Trim()] = new CommandRegistration
            {
                ArgCount = argumentCount,
                SuccessMessageFormat = successMessageFormat ?? string.Empty
            };
        }

        /// <summary>
        /// 将整行输入拆成：第一个空格前为命令，其后为参数字符串（再由 <see cref="ProcessInput"/> 按空格拆成固定个数）。
        /// </summary>
        public static void SplitCommandLine(string trimmedLine, out string command, out string argumentsTail)
        {
            if (string.IsNullOrEmpty(trimmedLine))
            {
                command = string.Empty;
                argumentsTail = string.Empty;
                return;
            }

            int spaceIndex = trimmedLine.IndexOf(' ');
            if (spaceIndex < 0)
            {
                command = trimmedLine;
                argumentsTail = string.Empty;
                return;
            }

            command = trimmedLine.Substring(0, spaceIndex);
            argumentsTail = trimmedLine.Substring(spaceIndex + 1).TrimStart();
        }

        private static string[] SplitArgumentTokens(string argumentsTail)
        {
            if (string.IsNullOrWhiteSpace(argumentsTail))
            {
                return Array.Empty<string>();
            }

            return argumentsTail.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 解析并校验命令；无效时返回错误文案，成功时返回已注册的成功文案（均会追加到终端历史）。
        /// </summary>
        public string ProcessInput(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            string trimmed = line.Trim();
            SplitCommandLine(trimmed, out string command, out string argumentsTail);

            if (string.IsNullOrEmpty(command))
            {
                return "> Invalid input";
            }

            if (!_registeredCommands.TryGetValue(command, out CommandRegistration registration))
            {
                return $"> Invalid command: {command}";
            }

            int expectedArgCount = registration.ArgCount;
            string[] tokens = SplitArgumentTokens(argumentsTail);
            if (tokens.Length < expectedArgCount)
            {
                return $"> Insufficient parameters: need {expectedArgCount}, current {tokens.Length}";
            }

            string[] args;
            if (expectedArgCount == 0)
            {
                args = Array.Empty<string>();
            }
            else
            {
                args = new string[expectedArgCount];
                Array.Copy(tokens, 0, args, 0, expectedArgCount);
            }

            bool suppressFeedback = _terminalCommandExecuter.ExecuteCommand(command, args, out string customSuccessMessage);
            if (suppressFeedback)
            {
                return null;
            }

            if (customSuccessMessage != null)
            {
                return PrefixFeedbackLines(customSuccessMessage);
            }

            return "> " + FormatRegisteredSuccessMessage(registration.SuccessMessageFormat, args);
        }

        /// <summary>
        /// 除 ls 外的已注册命令，每行「命令名 — 注册时的说明文案」。
        /// </summary>
        public string BuildRegisteredCommandsListText()
        {
            var names = new List<string>(_registeredCommands.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            var lines = new List<string>();
            foreach (string name in names)
            {
                if (!_registeredCommands.TryGetValue(name, out CommandRegistration reg))
                {
                    continue;
                }

                string desc = string.IsNullOrEmpty(reg.SuccessMessageFormat)
                    ? "(No description)"
                    : reg.SuccessMessageFormat;
                lines.Add($"{name} - {desc}");
            }

            if (lines.Count == 0)
            {
                return "(No other registered commands)";
            }

            return string.Join("\n", lines);
        }

        private static string PrefixFeedbackLines(string multilineBody)
        {
            if (string.IsNullOrEmpty(multilineBody))
            {
                return "> ";
            }

            string[] lines = multilineBody.Split(new[] { '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = "> " + lines[i];
            }

            return string.Join("\n", lines);
        }

        private static string FormatRegisteredSuccessMessage(string template, string[] args)
        {
            if (string.IsNullOrEmpty(template))
            {
                return "Execution completed";
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(template, args);
                }
                catch (FormatException ex)
                {
                    Debug.LogWarning($"[Terminal] Success message does not match parameters: {template}\n{ex.Message}");
                }
            }

            return template;
        }

        public void ClearTerminalHistory()
        {
            _savedHistory?.Clear();
            _terminalPopup?.ClearDisplayHistory();
        }

        public void Update(float deltaTime)
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                if (_terminalPopup != null && _isTerminalOpen)
                {
                    CloseTerminal();
                }
                else if (_terminalPopup == null && !_isTerminalOpen)
                {
                    OpenTerminal();
                }
            }
        }

        public void OpenTerminal()
        {
            _isTerminalOpen = true;
            PopupTerminalData data = new PopupTerminalData();
            data.inputHistory = new List<string>(_savedHistory);
            PopupManager.Instance.OpenPopup<PopupTerminalData>("popup_terminal", data, (popup) =>
            {
                _terminalPopup = popup as PopupTerminal;
            });
        }

        public void CloseTerminal()
        {
            _savedHistory = _terminalPopup != null ? _terminalPopup.InputHistory : new List<string>();
            _terminalPopup?.Close();
            _terminalPopup = null;
            _isTerminalOpen = false;
        }
    }
}