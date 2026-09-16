using System;
using System.Collections.Generic;
using System.Text;
using Dragon.Config;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板 Config 页：下拉选表，左侧 ID 列表，右侧行详情（key : value）。
    /// </summary>
    public class UIGMPanelConfig : MonoBehaviour
    {
        private const string DetailValueColor = "#4CAF50";

        [SerializeField] private TMP_Dropdown _tableDropdown;
        [SerializeField] private ScrollRect _rowScroll;
        [SerializeField] private RectTransform _rowContent;
        [SerializeField] private TextMeshProUGUI _detailText;
        [SerializeField] private UIConfigRowItem _rowItemPrefab;

        private readonly List<UIConfigRowItem> _rowItems = new List<UIConfigRowItem>();
        private readonly List<JSONNode> _currentRows = new List<JSONNode>();
        private string[] _tableNames = Array.Empty<string>();

        private void Awake()
        {
            if (_tableDropdown != null)
            {
                _tableDropdown.onValueChanged.AddListener(OnTableChanged);
            }
        }

        private void OnDestroy()
        {
            if (_tableDropdown != null)
            {
                _tableDropdown.onValueChanged.RemoveListener(OnTableChanged);
            }
        }

        private void OnEnable()
        {
            RefreshTableDropdown();
        }

        private void RefreshTableDropdown()
        {
            ConfigManager configManager = GameCenter.ConfigManager;
            if (configManager == null || !configManager.IsInitialized)
            {
                ClearRows();
                SetDetailText("Config not loaded.");
                return;
            }

            _tableNames = configManager.GetConfigFileNames();
            if (_tableDropdown == null)
            {
                LoadTable(0);
                return;
            }

            _tableDropdown.onValueChanged.RemoveListener(OnTableChanged);
            _tableDropdown.ClearOptions();
            _tableDropdown.AddOptions(new List<string>(_tableNames));

            int index = Mathf.Clamp(_tableDropdown.value, 0, Mathf.Max(0, _tableNames.Length - 1));
            _tableDropdown.SetValueWithoutNotify(index);
            _tableDropdown.RefreshShownValue();
            _tableDropdown.onValueChanged.AddListener(OnTableChanged);

            LoadTable(index);
        }

        private void OnTableChanged(int index)
        {
            LoadTable(index);
        }

        private void LoadTable(int tableIndex)
        {
            ClearRows();
            SetDetailText(string.Empty);

            if (tableIndex < 0 || tableIndex >= _tableNames.Length)
            {
                return;
            }

            string fileName = _tableNames[tableIndex];
            ConfigManager configManager = GameCenter.ConfigManager;
            if (configManager == null || !configManager.TryGetRawJsonNode(fileName, out JSONNode root))
            {
                SetDetailText($"Failed to load: {fileName}");
                return;
            }

            JSONArray array = root.AsArray;
            if (array == null)
            {
                SetDetailText("Not a table array.");
                return;
            }

            if (_rowItemPrefab == null || _rowContent == null)
            {
                SetDetailText("Row list UI is not configured.");
                return;
            }

            for (int i = 0; i < array.Count; i++)
            {
                JSONNode row = array[i];
                _currentRows.Add(row);

                UIConfigRowItem item = Instantiate(_rowItemPrefab, _rowContent);
                item.Bind(i, GetRowIdDisplay(row, i), OnRowClicked);
                _rowItems.Add(item);
            }

            if (_rowScroll != null)
            {
                _rowScroll.verticalNormalizedPosition = 1f;
            }

            if (_rowItems.Count > 0)
            {
                SelectRow(0);
            }
        }

        private void OnRowClicked(int index)
        {
            SelectRow(index);
        }

        private void SelectRow(int index)
        {
            if (index < 0 || index >= _currentRows.Count)
            {
                return;
            }

            for (int i = 0; i < _rowItems.Count; i++)
            {
                _rowItems[i].SetSelected(i == index);
            }

            JSONNode row = _currentRows[index];
            SetDetailText(FormatRowDetail(row));
        }

        private void ClearRows()
        {
            _rowItems.Clear();
            _currentRows.Clear();

            if (_rowContent == null)
            {
                return;
            }

            for (int i = _rowContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_rowContent.GetChild(i).gameObject);
            }
        }

        private void SetDetailText(string text)
        {
            if (_detailText != null)
            {
                _detailText.text = text;
            }
        }

        private static string FormatRowDetail(JSONNode row)
        {
            if (row == null || row.IsNull)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (KeyValuePair<string, JSONNode> pair in row)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(pair.Key)
                    .Append(" : <color=")
                    .Append(DetailValueColor)
                    .Append(">")
                    .Append(EscapeRichText(FormatValue(pair.Value)))
                    .Append("</color>");
            }

            return builder.ToString();
        }

        private static string EscapeRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string FormatValue(JSONNode valueNode)
        {
            if (valueNode == null || valueNode.IsNull)
            {
                return string.Empty;
            }

            if (valueNode.IsBoolean)
            {
                return valueNode.AsBool ? "TRUE" : "FALSE";
            }

            if (valueNode.IsString || valueNode.IsNumber)
            {
                return valueNode.Value;
            }

            return valueNode.ToString();
        }

        private static string GetRowIdDisplay(JSONNode row, int index)
        {
            if (row == null)
            {
                return "#" + index;
            }

            JSONNode idNode = row["id"] ?? row["Id"];
            if (idNode != null && !idNode.IsNull)
            {
                return "#" + idNode.Value;
            }

            return "#" + index;
        }
    }
}
