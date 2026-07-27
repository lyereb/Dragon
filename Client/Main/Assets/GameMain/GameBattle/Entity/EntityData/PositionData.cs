using UnityEngine;

public class PositionData
{
    #region 位置
    public float X { get; private set; }
    public float Y { get; private set; } // Depth on battle plane.
    public float Z { get; private set; } // Height, usually for jump.

    public Vector3 LogicPosition => new Vector3(X, Y, Z);
    #endregion

    public PositionData()
    {
        SetLogicPosition(0f, 0f, 0f);
    }

    public void SetLogicPosition(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void SetLogicPosition(Vector3 logicPosition)
    {
        SetLogicPosition(logicPosition.x, logicPosition.y, logicPosition.z);
    }

    /// <summary>
    /// Convert logic position (x,y,z) to render position.
    /// Logic: X = horizontal, Y = depth, Z = height.
    /// Render: depth (Y) contributes to rendered vertical via DepthToVerticalOffset.
    /// Example:
    /// - 0f   : no 2.5D projection offset.
    /// - 0.5f : every 1 depth adds 0.5 vertical render offset.
    /// </summary>
    public Vector3 GetRenderPosition()
    {
        return new Vector3(X, Z + Y * GlobalDefine.DepthToVerticalOffset, Y);
    }
}
