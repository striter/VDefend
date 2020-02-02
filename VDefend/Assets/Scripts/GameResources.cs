using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
public class GameResources : MonoBehaviour {
    
    public AtlasLoader m_GameAtlas { get; private set; }
    protected void Awake()
    {
        m_GameAtlas = new AtlasLoader(TResources.Load<UnityEngine.U2D.SpriteAtlas>("Atlas_Game"));
    }
}
