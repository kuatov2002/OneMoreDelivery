using System;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.Inventory;
using GameCreator.Runtime.VisualScripting;
using UnityEngine;

namespace GameCreator.Runtime.Inventory
{
    [Title("Compare Items")]
    [Description("Returns true if two Items are equal")]

    [Category("Inventory/Compare Items")]
    
    [Parameter("Item A", "The first item to compare")]
    [Parameter("Item B", "The second item to compare")]
    
    [Keywords("Inventory", "Compare", "Equal", "Same", "Match", "Variable")]
    
    [Image(typeof(IconItem), ColorTheme.Type.Green)]
    [Serializable]
    public class ConditionInventoryCompareItems : Condition
    {
        // EXPOSED MEMBERS: -----------------------------------------------------------------------

        [SerializeField] private PropertyGetItem m_ItemA = new PropertyGetItem();
        [SerializeField] private PropertyGetItem m_ItemB = new PropertyGetItem();

        // PROPERTIES: ----------------------------------------------------------------------------
        
        protected override string Summary => $"{this.m_ItemA} equals {this.m_ItemB}";
        
        // RUN METHOD: ----------------------------------------------------------------------------

        protected override bool Run(Args args)
        {
            Item itemA = this.m_ItemA.Get(args);
            Item itemB = this.m_ItemB.Get(args);
            
            if (itemA == null || itemB == null) return false;
            
            return itemA == itemB;
        }
    }
}