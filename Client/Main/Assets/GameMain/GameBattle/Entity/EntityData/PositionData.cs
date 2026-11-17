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
    /// 逻辑坐标换成俯视画面坐标。
    /// 逻辑 X 是左右，逻辑 Y 是前后，直接对应画面 X/Y。
    /// 逻辑 Z 是高度。跳跃关闭时高度不画出来。
    /// </summary>
    public Vector3 GetRenderPosition()
    {
        return new Vector3(X, Y, 0f);
    }
}
