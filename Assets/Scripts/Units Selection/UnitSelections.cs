using System.Collections.Generic;
using UnityEngine;

namespace Units_Selection
{
    public class UnitSelections : MonoBehaviour{
        public List<Unit> unitList = new();
        public List<Unit> unitSelectedList = new();

        public static UnitSelections Instance { get; private set; }

        private void Awake(){
            if (Instance != null && Instance != this){
                Destroy(gameObject);
            }
            else{
                Instance = this;
            }
        }

        public void ClickSelect(Unit unitToAdd){
            DeselectAll();
            unitToAdd.UnitSelected(true);
            unitSelectedList.Add(unitToAdd);
        }

        public void ShiftClickSelect(Unit unitToAdd){
            if(!unitSelectedList.Contains(unitToAdd))
            {
                unitSelectedList.Add(unitToAdd);
                unitToAdd.UnitSelected(true);
            }
            else
            {
                unitToAdd.UnitSelected(false);
                unitSelectedList.Remove(unitToAdd);
            }
        }

        public void DragSelect(List<Unit> units){
            unitSelectedList.Clear();
            unitSelectedList = units;
            unitSelectedList.ForEach(unit => { unit.UnitSelected(true); });
        }

        public void DeselectAll(){
            foreach (var unit in unitSelectedList)
            {
                unit.UnitSelected(false);
            }
            unitSelectedList.Clear();
        }
    }
}
