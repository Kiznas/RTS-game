using System.Collections.Generic;
using UnityEngine;

namespace Units_Selection
{
    public class UnitSelections : MonoBehaviour
    {
        public List<Unit> unitList = new();
        public List<Unit> unitSelectedList = new();

        public static UnitSelections Instance { get; private set; }

        private void Awake()
        {
            //if an instance of this already exists and it isn’t this one
            if (Instance != null && Instance != this)
            {
                // we destroy this instance
                Destroy(gameObject);
            }
            else
            {
                //make this the instance
                Instance = this;
            }
        }

        public void ClickSelect(Unit unitToAdd) 
        {
            DeselectAll();
            unitSelectedList.Add(unitToAdd);
            unitToAdd.isSelected = true;
        }

        public void ShiftClickSelect(Unit unitToAdd)
        {
            if(!unitSelectedList.Contains(unitToAdd))
            {
                unitSelectedList.Add(unitToAdd);
                unitToAdd.isSelected= true;
            }
            else
            { 
                unitToAdd.isSelected= false;
                unitSelectedList.Remove(unitToAdd);
            }
        }

        public void DragSelect(List<Unit> units)
        {
            unitSelectedList.Clear();
            unitSelectedList = units;
            unitSelectedList.ForEach(unit => { unit.isSelected = true; });
        }

        public void DeselectAll()
        {
            foreach (var unit in unitSelectedList)
            {
                unit.transform.GetChild(0).gameObject.SetActive(false);
                unit.isSelected = false;
            }
            unitSelectedList.Clear();
        }
    }
}
