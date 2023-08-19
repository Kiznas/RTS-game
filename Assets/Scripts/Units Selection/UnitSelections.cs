using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Units_Selection
{
    public class UnitSelections : MonoBehaviour
    {
        public List<Transform> unitList = new();
        public List<Transform> unitSelectedList = new();
        public List<Transform> unselectedUnits = new(); 
        
        public static UnitSelections Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            UpdateUnselectedUnits();
        }

        private void UpdateUnselectedUnits()
        {
            unselectedUnits.Clear();
            unselectedUnits = unitList.Except(unitSelectedList).ToList();
        }

        public void ClickSelect(Transform unitToAdd)
        {
            DeselectAll();
            unitSelectedList.Add(unitToAdd);
            UpdateUnselectedUnits();
        }

        public void ShiftClickSelect(Transform unitToAdd)
        {
            if (!unitSelectedList.Contains(unitToAdd))
            {
                unitSelectedList.Add(unitToAdd);
            }
            else
            {
                unitSelectedList.Remove(unitToAdd);
            }
            UpdateUnselectedUnits();
        }

        public void DragSelect(List<Transform> units)
        {
            unitSelectedList.Clear();
            unitSelectedList.AddRange(units);
            UpdateUnselectedUnits();
        }

        public void DeselectAll()
        {
            unitSelectedList.Clear();
            UpdateUnselectedUnits();
        }
    }
}