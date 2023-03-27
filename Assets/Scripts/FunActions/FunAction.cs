using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace V2
{
    [Serializable]
    public struct FunAction : IComponentData
    {
        public int at;
        public int pos;
    }
   
    public struct FunActionSorter : IComparer<FunAction>
    {
        public int Compare(FunAction a, FunAction b)
        {
            return a.at.CompareTo(b.at);
        }
    }
}