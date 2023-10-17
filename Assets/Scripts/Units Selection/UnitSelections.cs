using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Units_Selection
{
    public class UnitSelections : MonoBehaviour
    {
        public readonly HashSet<Transform> UnitList = new();
        public HashSet<Transform> UnitSelectedHash = new();
        public HashSet<Transform> UnselectedUnitsHash = new();

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
            UnselectedUnitsHash = new HashSet<Transform>(UnitList);
            UnselectedUnitsHash.ExceptWith(UnitSelectedHash);
        }

        public void ClickSelect(Transform unitToAdd)
        {
            DeselectAll();
            AddElement(UnitSelectedHash, unitToAdd);
            UpdateUnselectedUnits();
        }

        public void ShiftClickSelect(Transform unitToAdd)
        {
            if (!UnitSelectedHash.Contains(unitToAdd))
            {
                AddElement(UnitSelectedHash, unitToAdd);
            }
            else
            {
                RemoveElement(UnitSelectedHash, unitToAdd);
            }
            UpdateUnselectedUnits();
        }

        public void DragSelect(Transform[] units)
        {
            UnitSelectedHash.AddRange(units);
            UpdateUnselectedUnits();
        }

        public void DeselectAll()
        {
            UnitSelectedHash.Clear();
            UpdateUnselectedUnits();
        }

        public void AddElement(HashSet<Transform> hashSet, Transform element)
        {
            hashSet.Add(element);
        }

        public void RemoveElement(HashSet<Transform> hashSet, Transform element)
        {
            hashSet.Remove(element);
        }
    }
}