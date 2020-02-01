using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
using UnityEngine.UI;
using System;
#pragma warning disable 0649
public class GameEntityBase : MonoBehaviour{
    public enum_EntityType m_EntityType { get; private set; }
    public enum_TCellState m_TCellType { get; private set; } = enum_TCellState.Invalid;
    public int m_Identity { get; private set; }
    public Vector2 Pos => rectTransform.anchoredPosition;
    public RectTransform rectTransform { get; private set; }

    bool m_PlayerControling = false;
    Action<int> DoRecycle;
    List<GameTileCell> m_CellsEffecting = new List<GameTileCell>();
    public EntityData m_Data { get; private set; }
    Queue<Vector2> m_PathFinding = new Queue<Vector2>();

    public void Activate( enum_EntityType type, int identity, Action<int> DoRecycle)
    {
        rectTransform = transform as RectTransform;
        transform.Find(enum_EntityType.Antibody.ToString()).SetActivate(type== enum_EntityType.Antibody);
        transform.Find(enum_EntityType.TCell.ToString()).SetActivate(type == enum_EntityType.TCell);
        transform.Find(enum_EntityType.Virus.ToString()).SetActivate(type == enum_EntityType.Virus);
        m_EntityType = type;
        m_Identity = identity;
        this.DoRecycle = DoRecycle;
        m_PathFinding.Clear();
        m_CellsEffecting.Clear();
        m_PlayerControling = false;
        SwitchState(enum_TCellState.Normal);
    }
    void SwitchState(enum_TCellState type)
    {
        m_TCellType = type;
        m_Data = GameExpressions.GetEntityData(m_EntityType, m_TCellType);
    }

    public GameEntityBase PlayerTakeControll()
    {
        m_PlayerControling = true;
        return this;
    }

    public void PlayerSetDestination(Vector2 pos)
    {
        GameManager.Instance.TryPathFind(rectTransform.anchoredPosition, pos, ref m_PathFinding);
    }

    public void Tick(float deltaTime)
    {
        if (!m_PlayerControling)
            TickAIMove(deltaTime);

        switch (m_EntityType)
        {
            case enum_EntityType.TCell:
                TickTCell(deltaTime);
                break;
            case enum_EntityType.Virus:
                TickVirus(deltaTime);
                break;
        }

        TickPathFind(deltaTime);
    }

    public void OnDead()
    {
        DoRecycle(m_Identity);
    }
    #region TCell

    void TickTCell(float deltaTime)
    {
        enum_TCellState newState = GameManager.Instance.PickupNearby(this);
        if(newState!= enum_TCellState.Invalid)
            SwitchState(newState);
        GameManager.Instance.CheckNearbyCells(this, ref m_CellsEffecting,p=>p.m_Infected);
        m_CellsEffecting.Traversal((GameTileCell cell) => {
            cell.DoDeinfect(deltaTime*m_Data.m_DeinfectMultiple);
        });
    }
    #endregion

    void TickVirus(float deltaTime)
    {
        if (GameManager.Instance.CostNearbyAntibody(Pos))
        {
            OnDead();
            return;
        }

        GameManager.Instance.CheckNearbyCells(this, ref m_CellsEffecting, p => !p.m_Infected);

        m_CellsEffecting.Traversal((GameTileCell cell) =>
        {
                cell.DoInfectStack();
        } );
    }

    #region PathFind
    bool m_HavePath => m_PathFinding.Count > 0;

    void TickAIMove(float deltaTime)
    {
        if (m_HavePath)
            return;

        GameManager.Instance.TryPathFind(GameManager.Instance.GetPathFindPoint(Pos), GameManager.Instance.RandomPathPoint(), ref m_PathFinding);
    }
    void TickPathFind(float deltaTime)
    {
        if (!m_HavePath)
            return;

        Vector2 destination = m_PathFinding.Peek();
        Vector2 offset = destination - rectTransform.anchoredPosition;
        float length = m_Data.m_Speed * deltaTime;
        if (length > offset.magnitude)
        {
            rectTransform.anchoredPosition = destination;
            m_PathFinding.Dequeue();
        }
        else
            rectTransform.anchoredPosition += offset.normalized * length;
    }

    #endregion
}
