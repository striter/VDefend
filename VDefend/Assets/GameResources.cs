using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
public class GameResources : SimpleSingletonMono<GameResources> {

    public Dictionary<bool, Sprite> m_CellSprite { get; private set; } = new Dictionary<bool, Sprite>();
    public Dictionary<enum_TCellState, Sprite> m_TCellPickup { get; private set; } = new Dictionary<enum_TCellState, Sprite>();
    protected override void Awake()
    {
        base.Awake();
        m_CellSprite.Add(true, TResources.Load<Sprite>("UI/cell_infected"));
        m_CellSprite.Add(false, TResources.Load<Sprite>("UI/cell_healthy"));
    }
}
