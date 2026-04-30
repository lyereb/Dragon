using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityData
{
    public PositionData Position;
    //形状数据
    public float WidthSize;
    public float DepthSize;
    public bool isRigidbody;

    public EntityData()
    {
        Position = new PositionData();
    }
}
