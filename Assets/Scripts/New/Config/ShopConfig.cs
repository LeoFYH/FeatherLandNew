using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class ShopConfig : ScriptableObject
    {
        [LabelText("蛋的价格")]
        public int eggPackage = 100;
    }
}