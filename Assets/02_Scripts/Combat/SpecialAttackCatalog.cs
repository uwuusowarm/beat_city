using System.Collections.Generic;

public enum SpecialAttackId
{
    ChainAttack,
    GroundSlam,
    DashStrike
}

public class SpecialAttackDef
{
    public SpecialAttackId id;
    public string displayName;
    public int shopCost;
    public bool unlockedByDefault;
    public int meterCost;

    public SpecialAttackDef(SpecialAttackId id, string displayName, int shopCost, bool unlockedByDefault, int meterCost)
    {
        this.id = id;
        this.displayName = displayName;
        this.shopCost = shopCost;
        this.unlockedByDefault = unlockedByDefault;
        this.meterCost = meterCost;
    }
}

public static class SpecialAttackCatalog
{
    public static readonly List<SpecialAttackDef> All = new List<SpecialAttackDef>
    {
        new SpecialAttackDef(SpecialAttackId.ChainAttack, "Chain Attack", 500, false, 25),
        new SpecialAttackDef(SpecialAttackId.GroundSlam, "Ground Slam", 500, false, 25),
        new SpecialAttackDef(SpecialAttackId.DashStrike, "Dash Strike", 500, false, 25),
    };

    public static SpecialAttackDef Get(SpecialAttackId id)
    {
        return All.Find(def => def.id == id);
    }
}
