using Creatures;
using Creatures.Navigation;
using UnityEngine;

public class CreateCreatureEvent
{
    public CreaturesData CreaturesData;
}

public class CreatureCommandEvent
{
    public GameObject TargetCreature;
    public CreatureState CommandState;

    public bool AnyCommand;
}

public class CreatureDeadEvent
{
    public GameObject Creature;
    public bool UpdateList;
}

public class CreatureRemoveAll
{

}

public class CreatureTransformCommandEvent
{
    public int CommandIndex;
} 

public class SelectedWeaponEvent
{
    public int WeaponIndex;
}

public class LoadSceneEvent
{
    public string SceneName;
}

public class ShowInterstitialAdEvent
{

}

public class CreatureWindowView
{
    public bool IsShow;
}

public class CreatureVisibleEffect
{
    public bool IsShow;
}

public class ChangePlayerTagEvent
{
    public GameObject PlayerTarget;
}