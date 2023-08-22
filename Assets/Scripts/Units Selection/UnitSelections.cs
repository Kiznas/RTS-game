using System;
using System.Linq;
using UnityEngine;

namespace Units_Selection
{
    public class UnitSelections : MonoBehaviour
    {
        public Transform[] unitList = Array.Empty<Transform>();
        public Transform[] unitSelectedList = Array.Empty<Transform>();
        public Transform[] unselectedUnits = Array.Empty<Transform>();

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
            unselectedUnits = unitList.Except(unitSelectedList).ToArray();
        }

        public void ClickSelect(Transform unitToAdd)
        {
            DeselectAll();
            AddElement(ref unitSelectedList, unitToAdd);
            UpdateUnselectedUnits();
        }

        public void ShiftClickSelect(Transform unitToAdd)
        {
            if (!unitSelectedList.Contains(unitToAdd))
            {
                AddElement(ref unitSelectedList, unitToAdd);
            }
            else
            {
                RemoveElement(ref unitSelectedList, unitToAdd);
            }
            UpdateUnselectedUnits();
        }

        public void DragSelect(Transform[] units)
        {
            unitSelectedList = units;
            UpdateUnselectedUnits();
        }

        public void DeselectAll()
        {
            unitSelectedList = Array.Empty<Transform>();
            UpdateUnselectedUnits();
        }

        public void AddElement(ref Transform[] array, Transform element)
        {
            int newSize = array.Length + 1;
            Array.Resize(ref array, newSize);
            array[newSize - 1] = element;
        }

        public void RemoveElement(ref Transform[] array, Transform element)
        {
            int index = Array.IndexOf(array, element);
            if (index >= 0)
            {
                for (int i = index; i < array.Length - 1; i++)
                {
                    array[i] = array[i + 1];
                }
                Array.Resize(ref array, array.Length - 1);
            }
        }
    }
}