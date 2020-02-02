using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
using UnityEngine.UI;
using System;
#pragma warning disable 0649
public class GameEntityBase : MonoBehaviour,ISingleCoroutine{
    public enum_EntityType m_EntityType { get; private set; }
    public enum_TCellState m_TCellType { get; private set; } = enum_TCellState.Invalid;
    public int m_Identity { get; private set; }
    public Vector2 Pos => rectTransform.anchoredPosition;
    public RectTransform rectTransform { get; private set; }

    bool m_PlayerControling = false;
    bool m_Activating = false;
    Action<int> DoRecycle;
    List<GameTileCell> m_CellsEffecting = new List<GameTileCell>();
    public EntityData m_Data { get; private set; }
    Queue<Vector2> m_PathFinding = new Queue<Vector2>();
    Image m_TCell;

    public void Activate( enum_EntityType type, int identity, Action<int> DoRecycle)
    {
        m_Activating = true;
        rectTransform = transform as RectTransform;
        m_EntityType = type;
        m_Identity = identity;
        this.DoRecycle = DoRecycle;
        m_PathFinding.Clear();
        m_CellsEffecting.Clear();
        m_PlayerControling = false;
        m_TCell = transform.Find("TCell").GetComponent<Image>();
        transform.Find(enum_EntityType.Antibody.ToString()).SetActivate(m_EntityType == enum_EntityType.Antibody);
        transform.Find(enum_EntityType.TCell.ToString()).SetActivate(m_EntityType == enum_EntityType.TCell);
        transform.Find(enum_EntityType.Virus.ToString()).SetActivate(m_EntityType == enum_EntityType.Virus);
        transform.localScale = Vector3.one;
        this.StopAllCoroutines();
        SwitchState(enum_TCellState.Normal);
    }
    private void OnDisable()
    {
        this.StopAllCoroutines();
    }

    void SwitchState(enum_TCellState type)
    {
        m_TCellType = type;
        m_Data = GameExpressions.GetEntityData(m_EntityType, m_TCellType);
        m_TCell.sprite = GameManager.Instance.m_Resources.m_GameAtlas["cell_t_"+m_TCellType];
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
        if (!m_Activating)
            return;

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

        if(m_Activating)
            TickPathFind(deltaTime);
    }

    public void DoAbsorb(Vector2 absorbPos)
    {
        m_Activating = false;
        Vector2 startPos = Pos;
        this.StartSingleCoroutine(0, TIEnumerators.ChangeValueTo((float value) =>
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, absorbPos, value);
            transform.localScale = Vector3.one * Mathf.Lerp(1,.5f,value); 
        }, 0, 1, .5f,()=> {
            DoRecycle(m_Identity);
        }));
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
            GameManager.Instance.m_Audios.Play("Antibody_eat");
            DoAbsorb(Pos);
            return;
        }

        GameManager.Instance.CheckNearbyCells(this, ref m_CellsEffecting, p => !p.m_Infected);
        if (m_CellsEffecting.Count <= 0)
            return;
        GameTileCell cell = m_CellsEffecting.RandomItem();
        cell.DoInfect();
        DoAbsorb(cell.Pos);
    }

    #region PathFind
    bool m_HavePath => m_PathFinding.Count > 0;

    void TickAIMove(float deltaTime)
    {
        if (m_HavePath)
            return;

        m_PathFinding= GameManager.Instance.TryPathFind(GameManager.Instance.GetPathFindPoint(Pos), GameManager.Instance.RandomPathPoint());
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
