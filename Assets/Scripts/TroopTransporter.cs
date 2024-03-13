using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class TroopTransporter : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI fromTroopDisplay;
    [SerializeField]
    TextMeshProUGUI toTroopDisplay;
    [SerializeField]
    TextMeshProUGUI toTerritoryName;
    [SerializeField]
    TextMeshProUGUI fromTerritoryName;
    Territory toTerritory;
    Territory fromTerritory = null;
    int fromTroopCount;
    int toTroopCount;
    public void UpdateTroopCounts(int count=1)
    {
        //updates the display and local variables to current troop distribution
        fromTroopCount -= count;
        toTroopCount += count;
        fromTroopDisplay.text = fromTroopCount.ToString();
        toTroopDisplay.text = toTroopCount.ToString();
    }
    public void SetupTroopTransporter(Territory toTerritory, Territory fromTerritory)
    {
        //runs when a territory is selected and initialises some variables for upcoming operations
        this.toTerritory = toTerritory;
        this.fromTerritory = fromTerritory;
        this.gameObject.SetActive(true);
        fromTroopCount = fromTerritory.GetCurrentTroops();
        toTroopCount = toTerritory.GetCurrentTroops();
        fromTroopDisplay.text = fromTroopCount.ToString();
        toTroopDisplay.text = toTroopCount.ToString();
        fromTerritoryName.SetText(fromTerritory.name);
        toTerritoryName.SetText(toTerritory.name);
    }
    public void SetupTroopTransporter(Territory toTerritory, int fromTroopCount)
    {
        //same as above but for when you are placing new troops onto the board
        this.toTerritory = toTerritory;
        this.fromTerritory = null;
        this.fromTroopCount = fromTroopCount;
        this.gameObject.SetActive(true);
        toTroopCount = toTerritory.GetCurrentTroops();
        fromTroopDisplay.text = fromTroopCount.ToString();
        toTroopDisplay.text = toTroopCount.ToString();
        fromTerritoryName.SetText("Deploy");
        toTerritoryName.SetText(toTerritory.name);
    }
    public int FinaliseTerritoryTroopCounts()
    {
        //runs at the end of the troop transfer and ensures the territories hold the correct number of troops
        this.gameObject.SetActive(false);
        toTerritory.SetCurrentTroops(toTroopCount);
        if (fromTerritory != null)
        {
            fromTerritory.SetCurrentTroops(fromTroopCount);
            return 0;
        }
        else
        {
            return fromTroopCount;
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)|| Input.GetKeyDown(KeyCode.D))
        {
            if ((fromTerritory != null && fromTroopCount > 1) || (fromTerritory == null && fromTroopCount > 0))
            {
                UpdateTroopCounts();
            }       
        }
        else if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) && toTroopCount>toTerritory.GetCurrentTroops())
        {
            UpdateTroopCounts(-1);
        }
    }
}
