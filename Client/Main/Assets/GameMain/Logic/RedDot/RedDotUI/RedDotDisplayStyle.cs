namespace Dragon.GameUI
{
    /// <summary>
    /// <see cref="RedDotView"/> 的展示样式。
    /// </summary>
    public enum RedDotDisplayStyle
    {
        /// <summary>仅显示红点图标。</summary>
        DotOnly = 0,

        /// <summary>仅显示数字。</summary>
        CountOnly = 1,

        /// <summary>红点 + 数字。</summary>
        DotAndCount = 2,

        /// <summary>Count &lt;= 1 只显示点；&gt; 1 显示数字（可配合点或仅数字）。</summary>
        DotOrCount = 3,
    }
}
