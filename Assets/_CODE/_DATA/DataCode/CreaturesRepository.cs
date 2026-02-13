using System.Collections.Generic;
using UnityEngine;

namespace Creatures
{
    [CreateAssetMenu(fileName = "CreatureRepository", menuName = "Creature/Repository")]
    public class CreaturesRepository : ConfigBase
    {
        public List<CreaturesData> CreaturesCollection = new List<CreaturesData>();
    }
}