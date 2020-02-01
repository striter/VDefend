using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
using UnityEngine.UI;
using TTiles;
#pragma warning disable 0649
public class GameTileBase : MonoBehaviour {
    public TileAxis m_Axis { get; private set; }
    public Vector2 Pos => rectTransform.anchoredPosition;
    RectTransform rectTransform;

    public virtual void Init( TileAxis _axis)
    {
        m_Axis = _axis;
        rectTransform = transform as RectTransform;
    }

}

