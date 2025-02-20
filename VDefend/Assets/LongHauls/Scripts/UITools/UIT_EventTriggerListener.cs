﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIT_EventTriggerListener : EventTrigger {
    #region Press
    public Action<bool, Vector2> OnPressStatus;
    protected Action<bool> OnPressDuration;
    float m_pressDurationCheck,m_pressDuration;
    bool m_pressing = false;
    bool m_pressDurationChecking = false;
    public void SetOnPressDuration (float _pressDuration,Action<bool> _OnPressDuration)
    {
        m_pressDuration = _pressDuration;
        OnPressDuration = _OnPressDuration;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (m_pressing)
            return;
        m_pressing = true;
        OnPressStatus?.Invoke(true, eventData.position);

        m_pressDurationCheck = m_pressDuration;
        m_pressDurationChecking = true;
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (!m_pressing)
            return;
        m_pressing = false;
        OnPressStatus?.Invoke(false, eventData.position);

        if (!m_pressDurationChecking)
            return;
        OnPressDuration?.Invoke(m_pressDurationCheck < 0);
        m_pressDurationChecking = false;
    }
    private void Update()
    {
        if (!m_pressing|| !m_pressDurationChecking)
            return;
        if (m_pressDurationCheck < 0)
            return;
        m_pressDurationCheck -= Time.deltaTime;
        if (m_pressDurationCheck < 0)
        {
            OnPressDuration?.Invoke(true);
            m_pressDurationChecking = false;
        }
    }
    private void OnDisable()
    {
        if (m_pressing) OnPressStatus(false, Vector2.zero);
    }
    #endregion

    #region Drag
    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        D_OnDrag?.Invoke(eventData.position);
        D_OnDragDelta?.Invoke(eventData.delta);
    }
    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        D_OnDragStatus?.Invoke(true, eventData.position);
    }
    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        D_OnDragStatus?.Invoke(false, eventData.position);
    }
    #endregion
    public Action<bool, Vector2> D_OnDragStatus;
    public Action<Vector2> D_OnDrag, D_OnDragDelta;
    public Action D_OnRaycast;
    public void OnRaycast()
    {
        D_OnRaycast?.Invoke();
    }
}
