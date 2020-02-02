using GameSettings;
using System.Collections;
using System.Collections.Generic;
using TTiles;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable 0649
public class GameTileCell : GameTileBase, TReflection.UI.IUIPropertyFill
{
    public bool m_Disabled => m_TimerDisabled.m_Timing;
    public bool m_Infected => m_InfectStack > 0;
    public int m_InfectStack { get; private set; } = 0;
    TimeCounter m_TimerInfect = new TimeCounter(GameConsts.F_InfectStackDuration), m_TimerDisabled = new TimeCounter(),m_TimerDeinfect=new TimeCounter(GameConsts.F_CellDeinfectDuration);
    Transform m_Container;
    Image m_Container_Image,m_Container_Infect,m_Container_Deinfect;
    public override void Init(TileAxis _axis)
    {
        base.Init(_axis);
        TReflection.UI.UIPropertyFill(this, this.transform);
        OnActivate();
    }
    void OnActivate()
    {
        m_TimerInfect.Reset();
        m_TimerDeinfect.Reset();
        m_TimerDisabled.SetTimer(0);
        m_InfectStack = 0;
        StatusChange();
    }

    void StatusChange()
    {
        m_Container.SetActivate(!m_Disabled);
        m_Container_Image.sprite = GameManager.Instance.m_Resources.m_GameAtlas[m_Infected?"cell_healthy":"cell_infected"];
        m_Container_Infect.SetActivate(m_Infected);
        m_Container_Deinfect.SetActivate(m_Infected);
        m_Container_Infect.fillAmount = 1 - m_TimerInfect.m_TimeLeftScale;
        m_Container_Deinfect.fillAmount = 1 - m_TimerDeinfect.m_TimeLeftScale;
    }

    public void Tick(float deltaTime)
    {
        if (m_Disabled)
        {
            m_TimerDisabled.Tick(deltaTime);
            if (!m_Disabled)
                OnActivate();
            return;
        }
        
        if (m_Infected)
        {
            m_Container_Infect.fillAmount =1- m_TimerInfect.m_TimeLeftScale;
            m_Container_Deinfect.fillAmount =1- m_TimerDeinfect.m_TimeLeftScale;
            m_TimerInfect.Tick(deltaTime);
            if (!m_TimerInfect.m_Timing)
            {
                m_TimerInfect.Reset();
                DoInfectStack();
            }
        }
    }


    public void DoInfectStack()
    {
        if (m_Disabled)
            return;
        
        m_InfectStack++;
        StatusChange();
        if (m_InfectStack > GameConsts.I_InfectDisbaleStack)
        {
            if (!GameManager.Instance.CostNearbyAntibody(Pos))
                GameManager.Instance.SpawnEntity(enum_EntityType.Virus, Pos);

            DoDisable();
        }
    }
    
    public void DoDeinfect(float deltaTime)
    {
        m_TimerDeinfect.Tick(deltaTime);
        if (!m_TimerDeinfect.m_Timing)
            DoDisable();
    }

    void DoDisable()
    {
        m_TimerDisabled.SetTimer(GameConsts.F_DisableDuration);
        StatusChange();
    }

    void ClearInfect()
    {
        if (!m_Infected)
            return;

        m_InfectStack = 0;
        m_TimerInfect.Reset();
        DoDisable();
    }
}
