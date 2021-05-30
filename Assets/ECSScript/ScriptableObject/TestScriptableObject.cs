using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KaizerwaldCode
{
    [CreateAssetMenu(fileName = "TestScriptableObject", menuName = "Game/Test ScriptableObject")]
    public class TestScriptableObject : ScriptableObject
    {
        [SerializeField]
        private int totalNumberOfNpcs;

        [SerializeField]
        private int totalFriends;

        public int TotalNumberOfNpcs => this.totalNumberOfNpcs;

        public int TotalFriends => this.totalFriends;
    }
}
