using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UnitSelections : MonoBehaviour
{
    public List<Unit> unitlist = new List<Unit>();
    public List<Unit> unitSelectedList = new List<Unit>();

    private static UnitSelections _instance;
    public static UnitSelections Instance { get { return _instance; } }

    private void Awake()
    {
        //if an instance of this already exists and it isn’t this one
        if (_instance != null && _instance != this)
        {
            // we destroy this instance
            Destroy(this.gameObject);
        }
        else
        {
            //make this the instance
            _instance = this;
        }
    }

    public void ClickSelect(Unit unitToAdd) 
    {
        DeselectAll();
        unitSelectedList.Add(unitToAdd);
        unitToAdd.IsSelected = true;
    }

    public void ShiftClickSelect(Unit unitToAdd)
    {
        if(!unitSelectedList.Contains(unitToAdd))
        {
            unitSelectedList.Add(unitToAdd);
            unitToAdd.IsSelected= true;
        }
        else
        { 
            unitToAdd.IsSelected= false;
            unitSelectedList.Remove(unitToAdd);
        }
    }

    public void DragSelect(List<Unit> units)
    {
        unitSelectedList.Clear();
        unitSelectedList = units;
        unitSelectedList.ForEach(unit => { unit.IsSelected = true; });
    }

    public void DeselectAll()
    {
        foreach (var unit in unitSelectedList)
        {
            unit.transform.GetChild(0).gameObject.SetActive(false);
            unit.IsSelected = false;
        }
        unitSelectedList.Clear();
    }
}
