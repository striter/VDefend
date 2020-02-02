using GameSettings;
using System.Collections;
using System.Collections.Generic;
using TTiles;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable 0649
public class GameTileCell : GameTileBase, TReflection.UI.IUIPropertyFill, ISingleCoroutine
{
    public bool m_Disabled => m_TimerDisabled.m_Timing;
    public bool m_Infected { get; private set; } = false;
    TimeCounter m_TimerInfectDisable = new TimeCounter(GameConsts.F_InfectDisableDuration), m_TimerDisabled = new TimeCounter(), m_TimerDeinfect = new TimeCounter(GameConsts.F_CellDeinfectDuration);
    Transform m_Container;
    Image m_Container_Image, m_Container_Infect, m_Container_Deinfect;
    public override void Init(TileAxis _axis)
    {
        base.Init(_axis);
        TReflection.UI.UIPropertyFill(this, this.transform);
        OnActivate();
    }
    void OnActivate()
    {
        m_TimerInfectDisable.Reset();
        m_TimerDeinfect.Reset();
        m_TimerDisabled.SetTimer(0);
        m_Infected = false;
        StatusChange();
    }

    void StatusChange()
    {
        m_Container_Image.sprite = GameManager.Instance.m_Resources.m_GameAtlas[m_Disabled ? "cell_grey" : !m_Infected ? "cell_healthy" : "cell_infected"];
        if (m_Disabled)
            this.StartSingleCoroutine(0, TIEnumerators.ChangeValueTo((float value) => { m_Container_Image.color = TCommon.ColorAlpha(m_Container_Image.color, value); }, 1, 0, 2));
        else
            m_Container_Image.color = TCommon.ColorAlpha(m_Container_Image.color, 1f);
        m_Container_Infect.SetActivate(!m_Disabled && m_Infected);
        m_Container_Deinfect.SetActivate(!m_Disabled && m_Infected);
        m_Container_Infect.fillAmount = 1 - m_TimerInfectDisable.m_TimeLeftScale;
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
            m_Container_Infect.fillAmount = 1 - m_TimerInfectDisable.m_TimeLeftScale;
            m_Container_Deinfect.fillAmount = 1 - m_TimerDeinfect.m_TimeLeftScale;
            m_TimerInfectDisable.Tick(deltaTime);
            if (!m_TimerInfectDisable.m_Timing)
            {
                if (!GameManager.Instance.CostNearbyAntibody(Pos))
                    GameManager.Instance.SpawnEntity(enum_EntityType.Virus, Pos);
                DoDisable();
            }
        }
    }


    public void DoInfect()
    {
        if (m_Disabled || m_Infected)
            return;
        m_Infected = true; 
        StatusChange();
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

        m_Infected = false;
        m_TimerInfectDisable.Reset();
        DoDisable();
    }
}
