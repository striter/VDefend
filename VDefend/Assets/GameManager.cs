using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameSettings;
using TTiles;
using System.Linq;
using System;
#pragma warning disable 0649
public class GameManager : SimpleSingletonMono<GameManager>,TReflection.UI.IUIPropertyFill {

    ObjectPoolSimpleComponent<TileAxis, GameTileCell> m_CellTilePool;
    ObjectPoolSimpleComponent<TileAxis, GameTilePath> m_PathTilePool;
    ObjectPoolSimpleComponent<int, GameEntityBase> m_EntityPool;
    ObjectPoolSimpleComponent<enum_TCellState, GamePickup> m_PickupPool;
    Dictionary<enum_EntityType, List<int>> m_EntityDic=new Dictionary<enum_EntityType, List<int>>(); 
    public bool m_Gaming { get; private set; }
    public GameResources m_Resources { get; private set; }
    public GameAudio m_Audios { get; private set; }
    GameEntityBase m_Player;
    float m_GameTimePassed = 0f;
    TimeCounter m_TimerAntibody = new TimeCounter(),m_TimerPickup=new TimeCounter(GameConsts.F_TCellPickupDuration);
    Vector2 m_AntibodyPos;


    Text m_GameTime, m_GameProgress, m_AntibodyTime;
    Transform m_GamePanel;
    Image m_ProgressBar;
    Image m_GamePanel_Background;
    Text m_GamePanel_Button_Text;
    Button m_GamePanel_Button;
    Button m_Quit;
    protected override void Awake()
    {
        base.Awake();
        TReflection.UI.UIPropertyFill(this, transform);
        m_CellTilePool = new ObjectPoolSimpleComponent<TileAxis, GameTileCell>(transform.Find("CellGrid"),"GridItem");
        m_PathTilePool = new ObjectPoolSimpleComponent<TileAxis, GameTilePath>(transform.Find("PathGrid"), "GridItem");
        m_EntityPool = new ObjectPoolSimpleComponent<int, GameEntityBase>(transform.Find("Entities"),"EntityItem");
        m_PickupPool=new ObjectPoolSimpleComponent<enum_TCellState, GamePickup>(transform.Find("Pickups"),"GridItem");
        m_Quit.onClick.AddListener(OnQuitClick);
        m_GamePanel_Button.onClick.AddListener(GameStart);
        m_Resources = GetComponent<GameResources>();
        m_Audios = GetComponent<GameAudio>();
        m_Audios.Init();
    }
    private void Start()
    {
        m_Audios.SwitchBGM(m_Gaming);
        ShowGamePanel("home", "Start", GameStart);
    }
    void GameStart()
    {
        m_CellTilePool.ClearPool();
        m_PathTilePool.ClearPool();
        m_PickupPool.ClearPool();
        int totalXCount = GameConsts.I_TileXCount * 2;
        int totalYCount = GameConsts.I_TileYCount * 2-1 ;
        Vector2 originPos =new Vector2((-totalXCount+2) / 2f*GameConsts.F_TileSize,( -totalYCount + 1) / 2f * GameConsts.F_TileSize);
        for (int i = 0; i < totalYCount; i++)
        {
            bool offsetline = i % 2 == 1;
            int lineCount = offsetline ? totalXCount  : totalXCount-1;
            for (int j = 0; j < lineCount; j++)
            {
                bool isCell = offsetline ? (j + 1) % 3 == 0 : j % 3 == 0;

                TileAxis axis = new TileAxis(j, i);
                Vector2 tilePosition = originPos + new Vector2(axis.X * GameConsts.F_TileSize + (offsetline ? -GameConsts.F_TileSize / 2f : 0),
                     axis.Y * GameConsts.F_TileSize);
                if (isCell)
                {
                    GameTileCell tile = m_CellTilePool.AddItem(axis);
                    (tile.transform as RectTransform).anchoredPosition = tilePosition;
                    tile.Init(axis);
                }
                else
                {
                    GameTilePath currentPath = m_PathTilePool.AddItem(axis);
                    (currentPath.transform as RectTransform).anchoredPosition = tilePosition;
                    currentPath.Init(axis);

                    GameTilePath connectPath = null;
                    TileAxis connectAxis = axis + TileAxis.Down;
                    if (m_PathTilePool.ContainsItem(connectAxis))
                    {
                        connectPath = m_PathTilePool.GetItem(connectAxis);
                        currentPath.SetNearbyPath( connectPath);
                        connectPath.SetNearbyPath( currentPath);
                    }
                    connectAxis = axis + new TileAxis(1,-1);
                    if (axis.X % 3 ==2 && m_PathTilePool.ContainsItem(connectAxis))
                    {
                        connectPath = m_PathTilePool.GetItem(connectAxis);
                        currentPath.SetNearbyPath(connectPath);
                        connectPath.SetNearbyPath(currentPath);
                    }
                    connectAxis = axis + new TileAxis(-1, -1);
                    if (axis.X%3 == 0 && m_PathTilePool.ContainsItem(connectAxis))
                    {
                        connectPath = m_PathTilePool.GetItem(connectAxis);
                        currentPath.SetNearbyPath(connectPath);
                        connectPath.SetNearbyPath(currentPath);
                    }
                    connectAxis = axis + TileAxis.Left; 
                    if (m_PathTilePool.ContainsItem(connectAxis))
                    {
                        connectPath = m_PathTilePool.GetItem(connectAxis);
                        currentPath.SetNearbyPath( connectPath);
                        connectPath.SetNearbyPath( currentPath);
                    }
                }
            }
        }

        ResetEntityDic();
        m_Player=SpawnEntity( enum_EntityType.TCell,Vector2.zero).PlayerTakeControll();
        for(int i=0;i<GameConsts.I_StartAllyCount;i++)
            SpawnEntity(enum_EntityType.TCell, RandomPathPoint().Pos);
        for (int i = 0; i < GameConsts.I_StartVirusCount; i++)
            SpawnEntity(enum_EntityType.Virus, RandomPathPoint().Pos);

        m_Gaming = true;
        m_GameTimePassed = 0f;
        SpawnPickups();
        PrepareAntibody();
        m_GamePanel.SetActivate(!m_Gaming);
        m_Audios.SwitchBGM(m_Gaming);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            GameStart();
            return;
        }

        if (!m_Gaming)
            return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
            m_Player.PlayerSetDestination(new Vector2(Input.mousePosition.x, Input.mousePosition.y) - new Vector2(Screen.width / 2f, Screen.height / 2f));

        int m_DisableCount = 0;
        int m_VirusCount = 0;
        int m_InfectCount = 0;
        float deltaTime = Time.deltaTime;
        m_GameTimePassed += deltaTime;
        m_CellTilePool.m_ActiveItemDic.Traversal((GameTileCell tile) =>
        {
            tile.Tick(deltaTime);
            if (tile.m_Disabled)
                m_DisableCount++;
            else if (tile.m_Infected)
                m_InfectCount++;
        });
        m_EntityPool.m_ActiveItemDic.Traversal((GameEntityBase entity) => {
            entity.Tick(deltaTime);
            if (entity.m_EntityType == enum_EntityType.Virus)
                m_VirusCount++;
        },true);

        PickupTick(deltaTime);
        AntibodyTick(deltaTime);


        float gameProgress = 1 - m_DisableCount / (float)m_CellTilePool.m_ActiveItemDic.Count;
        m_AntibodyTime.text = string.Format("Antibody Coming:{0:F2}", m_TimerAntibody.m_timeCheck);
        m_GameProgress.text = string.Format("Body Health:{0:F2}, Infected Cells:{1}, Virus:{2}", gameProgress, m_InfectCount, m_VirusCount);
        m_ProgressBar.fillAmount = gameProgress;
        m_GameTime.text = string.Format("Game Time:{0:F2}", m_GameTimePassed);

        if (gameProgress < GameConsts.F_GameLoseScale)
            OnGameFinish(false);
        if (m_InfectCount == 0 && m_VirusCount == 0)
            OnGameFinish(true);
    }
    void PrepareAntibody()
    {
        m_AntibodyPos = RandomPathPoint().Pos;
        m_TimerAntibody.SetTimer(GameExpressions.GameAntibodyDuration(m_GameTimePassed, m_Player.m_TCellType== enum_TCellState.Assist));
    }
    void AntibodyTick(float deltaTime)
    {
        m_TimerAntibody.Tick(deltaTime);
        if (!m_TimerAntibody.m_Timing)
        {
            int count = GameExpressions.GameAntibodyCount(m_GameTimePassed, m_Player.m_TCellType == enum_TCellState.Assist);
            for(int i=0;i<count;i++)
                SpawnEntity( enum_EntityType.Antibody,m_AntibodyPos+new Vector2(UnityEngine.Random.value*GameConsts.F_AntibodyGenerateRange, UnityEngine.Random.value*GameConsts.F_AntibodyGenerateRange) );
            m_Audios.Play("Antibody_appear", 2);
            PrepareAntibody();
        }
    }


    void PickupTick(float deltaTime)
    {
        m_TimerPickup.Tick(deltaTime);
        if (m_TimerPickup.m_Timing)
            return;
        SpawnPickups();
    }

    void SpawnPickups()
    {
        m_PickupPool.ClearPool();
        m_Audios.Play("Item_generate");
        TCommon.TraversalEnum((enum_TCellState state) =>
        {
            if (state == enum_TCellState.Normal)
                return;

            GamePickup pickup = m_PickupPool.AddItem(state);
            (pickup.transform as RectTransform).anchoredPosition = RandomPathPoint().Pos;
            pickup.Play(state);
        });
        m_TimerPickup.Reset();
    }

    public enum_TCellState PickupNearby(GameEntityBase entity)
    {
        enum_TCellState state = enum_TCellState.Invalid;
        m_PickupPool.m_ActiveItemDic.TraversalBreak((GamePickup pickup) =>
        {
            if (pickup.m_TCellType != entity.m_TCellType && Vector2.Distance(pickup.Pos, entity.Pos) < GameConsts.F_TCellPickupRange)
            {
                state = pickup.m_TCellType;
                m_PickupPool.RemoveItem(pickup.m_TCellType);
                m_Audios.Play("Item_use");
                return true;
            }
            return false;
        });
        return state;
    }

    void OnGameFinish(bool win)
    {
        m_Gaming = false;
        m_Audios.SwitchBGM(m_Gaming);
        m_Audios.Play(win?"Result_win":"Result_lose");
        ShowGamePanel(win?"win":"lose","Restart",GameStart);
    }


    void ShowGamePanel(string bg,string text,Action OnClick)
    {
        m_GamePanel.SetActivate(true);
        m_GamePanel_Button_Text.text = text;
        m_GamePanel_Background.sprite = m_Resources.m_GameAtlas["game_"+bg];
        m_GamePanel_Button.onClick.RemoveAllListeners();
        m_GamePanel_Button.onClick.AddListener(()=> { m_Audios.Play("UI",2); OnClick();m_GamePanel.SetActivate(false); });
    }

    void OnQuitClick()
    {
        Application.Quit();
        m_Audios.Play("UI_2");
    }

    #region Pathfind
    public GameTilePath RandomPathPoint() => m_PathTilePool.m_ActiveItemDic.RandomValue();
    public GameTilePath GetPathFindPoint(Vector2 sourcePos)
    {
        GameTilePath targetPath = null;
        float distance = float.MaxValue;

        m_PathTilePool.m_ActiveItemDic.Traversal((GameTilePath path) =>
        {
            float nDistance = Vector2.Distance(path.Pos, sourcePos);
            if(distance>nDistance)
            {
                distance = nDistance;
                targetPath = path;
            }
        });
        return targetPath;
    }
    public void TryPathFind(Vector2 source, Vector2 destination, ref Queue<Vector2> paths)
    {
        GameTilePath sourcePath = GetPathFindPoint(source);
        TryPathFind(sourcePath, GetPathFindPoint(destination), ref paths, Vector2.Distance(source,sourcePath.Pos)>GameConsts.F_TileSize*2/3f);
    }
    public void TryPathFind(GameTilePath sourcePath, GameTilePath targetPath, ref Queue<Vector2> paths,bool addSource=false)
    {
        paths.Clear();
        if(addSource)
        paths.Enqueue(sourcePath.Pos);
        Vector3 destination = targetPath.Pos;
        GameTilePath currentPath = sourcePath;
        int pathFindCount = 0;
        while(pathFindCount<15)
        {
            pathFindCount++;
            GameTilePath nextPath = null;
            float pathDistance = float.MaxValue;

            currentPath.m_NearTiles.Traversal((GameTilePath pathFind) =>
            {
                if (pathFind == currentPath)
                    return;

                float nDistace = Vector2.Distance(pathFind.Pos, destination);
                if (pathDistance > nDistace)
                {
                    pathDistance = nDistace;
                    nextPath = pathFind;
                }
            });
            currentPath = nextPath;
            paths.Enqueue(nextPath.Pos);
            if (nextPath == targetPath)
                break;
        }
    }
    #endregion
    #region Entity
    int m_entityCount=0;
    int GetEntityIdentity(enum_EntityType type) => (int)type * 1000 + m_entityCount++;

    void ResetEntityDic()
    {
        m_EntityPool.ClearPool();
        m_EntityDic.Clear();
    }
    public GameEntityBase SpawnEntity(enum_EntityType type,Vector2 position)
    {
        int index = GetEntityIdentity(type);
        GameEntityBase entity = m_EntityPool.AddItem(index);
        entity.Activate(type,index,RecycleEntity);
        (entity.transform as RectTransform).anchoredPosition = GetPathFindPoint(position).Pos;
        if (!m_EntityDic.ContainsKey(type))
            m_EntityDic.Add(type, new List<int>());
        m_EntityDic[type].Add(index);
        return entity;
    }
    void RecycleEntity(int index)
    {
        m_EntityDic[m_EntityPool.GetItem(index).m_EntityType].Remove(index);
        m_EntityPool.RemoveItem(index);
    }

    public void CheckNearbyCells(GameEntityBase entity,ref List<GameTileCell> cellList,Predicate<GameTileCell> cellPredicate=null)
    {
        cellList.Clear();
        foreach(GameTileCell cell in m_CellTilePool.m_ActiveItemDic.Values)
        {
            if (cell.m_Disabled||(cellPredicate!=null&&!cellPredicate(cell)))
                continue;

            if (Vector2.Distance(cell.Pos, entity.Pos) < entity.m_Data.m_Range)
                cellList.Add(cell);
        }
    }

    public bool CostNearbyAntibody(Vector2 pos)
    {
        if(!m_EntityDic.ContainsKey( enum_EntityType.Antibody))
            return false;

        GameEntityBase targetAntibody=null;
        float distance = float.MaxValue;
        foreach(int index in m_EntityDic[ enum_EntityType.Antibody])
        {
            GameEntityBase targetEntity = m_EntityPool.GetItem(index);
            float nDistance = Vector2.Distance(pos, targetEntity.Pos);
            if (nDistance > GameConsts.F_AntibodyEffectRange&& distance > nDistance)
            {
                distance = nDistance;
                targetAntibody = targetEntity;
            }
        }

        if (targetAntibody)
            targetAntibody.OnDead();
        return targetAntibody;
    }


    #endregion
}
