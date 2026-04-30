using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dragon.Library;
using TMPro;


public class PopupTerminalData : PopupBaseData
{
    public List<string> inputHistory;
}

public class PopupTerminal : PopupBase<PopupTerminalData>
{
    private const int MaxHistoryCount = 16;

    public TextMeshProUGUI terminalText;
    public TMP_InputField terminalInput;

    private readonly List<string> _inputHistory = new List<string>();
    public List<string> InputHistory => new List<string>(_inputHistory);

    // 仅记录“用户输入的命令行”（不包含以 "> " 开头的反馈行），用于方向键回填。
    private readonly List<string> _commandHistory = new List<string>();
    private int _commandHistoryCursor = 0; // 0..Count，Count 表示当前位置在“末尾草稿”
    private string _draftBeforeHistoryNav = string.Empty;

    public void ClearDisplayHistory()
    {
        _inputHistory.Clear();
        RefreshTerminalText();
    }

    private void Awake()
    {
        terminalInput.lineType = TMP_InputField.LineType.SingleLine;
        terminalInput.onSubmit.AddListener(HandleSubmit);
    }

    private void Update()
    {
        if (terminalInput == null || !terminalInput.isFocused)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            NavigateCommandHistory(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            NavigateCommandHistory(1);
        }
    }

    protected override void OnOpen(PopupTerminalData data)
    {
        _inputHistory.Clear();
        _commandHistory.Clear();
        if (data.inputHistory != null)
        {
            foreach (string history in data.inputHistory)
            {
                if (string.IsNullOrWhiteSpace(history))
                {
                    continue;
                }

                string trimmed = history.Trim();
                _inputHistory.Add(trimmed);
                if (!trimmed.StartsWith(">"))
                {
                    _commandHistory.Add(trimmed);
                }
            }
        }

        ResetCommandHistoryCursor();
        RefreshTerminalText();
        terminalInput.text = string.Empty;
        terminalInput.ActivateInputField();
    }

    protected override void OnClose()
    {
        terminalInput.onSubmit.RemoveListener(HandleSubmit);
    }

    private void HandleSubmit(string input)
    {
        SubmitInput(input);
    }

    private void SubmitInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            terminalInput.text = string.Empty;
            terminalInput.ActivateInputField();
            return;
        }

        string trimmed = input.Trim();
        _inputHistory.Add(trimmed);
        _commandHistory.Add(trimmed);
        ResetCommandHistoryCursor();
        if (_inputHistory.Count > MaxHistoryCount)
        {
            _inputHistory.RemoveAt(0);
        }

        string feedback = TerminalManager.Instance.ProcessInput(trimmed);
        if (!string.IsNullOrEmpty(feedback))
        {
            _inputHistory.Add(feedback);
            if (_inputHistory.Count > MaxHistoryCount)
            {
                _inputHistory.RemoveAt(0);
            }
        }

        RefreshTerminalText();
        terminalInput.text = string.Empty;
        terminalInput.ActivateInputField();
    }

    private void ResetCommandHistoryCursor()
    {
        _commandHistoryCursor = _commandHistory.Count;
        _draftBeforeHistoryNav = string.Empty;
    }

    private void NavigateCommandHistory(int direction)
    {
        if (_commandHistory.Count == 0)
        {
            return;
        }

        if (direction < 0)
        {
            // 第一次从草稿进入历史时，先保存草稿
            if (_commandHistoryCursor == _commandHistory.Count)
            {
                _draftBeforeHistoryNav = terminalInput.text ?? string.Empty;
            }

            _commandHistoryCursor = Mathf.Max(0, _commandHistoryCursor - 1);
            FillInputWithHistory(_commandHistory[_commandHistoryCursor]);
        }
        else if (direction > 0)
        {
            if (_commandHistoryCursor < _commandHistory.Count)
            {
                _commandHistoryCursor++;
            }

            if (_commandHistoryCursor >= _commandHistory.Count)
            {
                _commandHistoryCursor = _commandHistory.Count;
                FillInputWithHistory(_draftBeforeHistoryNav);
            }
            else
            {
                FillInputWithHistory(_commandHistory[_commandHistoryCursor]);
            }
        }
    }

    private void FillInputWithHistory(string text)
    {
        text ??= string.Empty;
        terminalInput.SetTextWithoutNotify(text);
        terminalInput.caretPosition = text.Length;
        terminalInput.selectionAnchorPosition = text.Length;
        terminalInput.selectionFocusPosition = text.Length;
    }

    private void RefreshTerminalText()
    {
        terminalText.text = _inputHistory.Count > 0
            ? string.Join("\n", _inputHistory)
            : string.Empty;
    }
}

