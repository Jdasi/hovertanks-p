using System;
using UnityEngine;

namespace HoverTanks.ArcadeRoute
{
    [Serializable]
    public class ArcadeLevelDefinition
    {
        public string SceneName => _sceneName;
        public Direction EntryDir => _entryDir;
        public Direction ExitDir => _exitDir;

        [SerializeField] string _sceneName;
        [SerializeField] Direction _entryDir;
        [SerializeField] Direction _exitDir;
    }
}
