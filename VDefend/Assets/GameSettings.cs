using UnityEngine;

namespace GameSettings
{
    #region GameEnums
    public enum enum_EntityType
    {
        Invalid=-1,
        TCell=1,
        Virus=2,
        Antibody=3,
    }

    public enum enum_TCellState
    {
        Invalid=-1,
        Normal=1,
        Attack=2,
        Restrain=3,
        Assist=4,
    }

    public enum enum_PathDirection
    {
        Invalid=-1,
        E=0,
        NE=1,
        SE=2,
        W=3,
        SW=4,
        NW=5,
    }
    #endregion

    public static class GameConsts
    {
        public const int I_TileXCount = 10;
        public const int I_TileYCount = 5;
        public const float F_TileSize = 90;

        public const float F_InfectDisableDuration = 5f;
        public const float F_DisableDuration= 10f;
        public const float F_CellDeinfectDuration = 3f;
        public const float F_AntibodyEffectRange = 100f;
        public const float F_AntibodyGenerateRange = 300f;
        public const float F_TCellPickupRange = 50f;
        public const float F_TCellPickupDuration = 30f;
        public const float F_GameLoseScale = .2f;
    }

    public static class GameExpressions
    {
        public static float GameAntibodyDuration(float gameTime, bool playerAsssiting)
        {
            float duration = gameTime > 120f ? 5f : 10f;
            if (playerAsssiting)
                duration /= 2;
            return duration;
        }
        public static int GameAntibodyCount(float gameTime, bool playerAsssiting)
        {
            int count = (int)(gameTime / 120) + 1;
            if (playerAsssiting)
                count *= 2;
            count = Mathf.Clamp(count, 0, 10);
            return count;
        }

        public static EntityData GetEntityData(enum_EntityType type,enum_TCellState tcellType)
        {
            EntityData data = new EntityData();
            switch(type)
            {
                case enum_EntityType.Antibody:
                    data = new EntityData(60f, 0f, 0f);
                    break;
                case enum_EntityType.Virus:
                    data = new EntityData(100f, 200f, 0f);
                    break;
                case enum_EntityType.TCell:
                    switch (tcellType)
                    {
                        case enum_TCellState.Normal:
                            data = new EntityData(100f, 180f, 1f);
                            break;
                        case enum_TCellState.Attack:
                            data = new EntityData(130f, 250f, 2f);
                            break;
                        case enum_TCellState.Restrain:
                            data = new EntityData(75f, 100f, .5f);
                            break;
                        case enum_TCellState.Assist:
                            data = new EntityData(100f, 200f, 0f);
                            break;
                    }
                    break;
            }
            return data;
        }
        public static enum_PathDirection InverseDirection(this enum_PathDirection direction)
        {
            switch(direction)
            {
                default:
                    return enum_PathDirection.Invalid;
                case enum_PathDirection.E:
                    return enum_PathDirection.W;
                case enum_PathDirection.NE:
                    return enum_PathDirection.SW;
                case enum_PathDirection.W:
                    return enum_PathDirection.E;
                case enum_PathDirection.NW:
                    return enum_PathDirection.SE;
                case enum_PathDirection.SE:
                    return enum_PathDirection.NW;
                case enum_PathDirection.SW:
                    return enum_PathDirection.NE;
            }
        }
    }

    public struct EntityData
    {
        public float m_Speed;
        public float m_Range;
        public float m_DeinfectMultiple;
        public EntityData(float speed,float range,float deinfectMultiple)
        {
            m_DeinfectMultiple = deinfectMultiple;
            m_Speed = speed;
            m_Range = range;
        }
    }
}