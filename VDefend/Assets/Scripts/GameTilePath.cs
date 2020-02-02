using GameSettings;
using System.Collections;
using System.Collections.Generic;
using TTiles;
using UnityEngine;

public class GameTilePath : GameTileBase {
    public List< GameTilePath> m_NearTiles { get; private set; } = new List< GameTilePath>();

    public void SetNearbyPath( GameTilePath tile) => m_NearTiles.Add( tile);
}
