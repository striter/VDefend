using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
using UnityEngine.UI;

public class GamePickup : MonoBehaviour {
    public enum_TCellState m_TCellType { get; private set; } = enum_TCellState.Invalid;
    RectTransform rectTransform;
    public Vector2 Pos => rectTransform.anchoredPosition;
    Image m_Image;
    public void Play(enum_TCellState type)
    {
        rectTransform = transform as RectTransform;
        m_TCellType = type;
        GetComponentInChildren<Image>().color = type.GetColor();
    }
}
