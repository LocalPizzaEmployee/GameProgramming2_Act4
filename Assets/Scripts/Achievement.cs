using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Achievement : IAchievementHandler
{
    public static event Action<IAchievementHandler> OnDoneAchievement;
    
    [field:SerializeField]
    public string Name { get; private set; }
    [field:SerializeField]
    public string Description { get; private set; }    
    [field:SerializeField]
    public bool Obtained { get; private set; } = false;
    [field:SerializeField]
    public AchievementKind Kind { get; private set; }
    [field:SerializeField]
    public AchievementType Type { get; private set; }
    [field:SerializeField]
    public int ConditionQuantity { get; private set; }

    public Achievement(AchievementInfo info)
    {
        Name = info.Name;
        Description = info.Description;
        Kind = info.Kind;
        Type = info.Type;
        ConditionQuantity = info.ConditionQuantity;
    }
    
    //TODO update the achievement
    public void UpdateAchievement(int count)
    {
        if (Obtained) return;
        
        switch (Type)
        {
            case AchievementType.Kill:
                if (count >= ConditionQuantity)
                    InvokeOnDoneAchievement();
                break;
            case AchievementType.Die:
                if (count <= ConditionQuantity)
                    InvokeOnDoneAchievement();
                break;
        }
    }

    private void InvokeOnDoneAchievement()
    {
        Obtained = true;
        Debug.Log("Achievement unlocked");
        OnDoneAchievement?.Invoke(this);
    }
}

public interface IAchievementHandler
{
    string Name { get; }
    string Description { get; }
    bool Obtained { get; }
    AchievementKind Kind { get; }
    AchievementType Type { get; }
    int ConditionQuantity { get; }
    void UpdateAchievement(int count);
}

public enum AchievementType
{
    Unassigned,
    Kill,
    Die,
}

public enum AchievementKind
{
    Unassigned,
    Kill100,
    DiedWithoutKills,
}
