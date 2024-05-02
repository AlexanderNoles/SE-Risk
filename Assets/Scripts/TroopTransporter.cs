using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
/// <summary>
/// Controls the transport of troops from one territory to another. This includes controlling the troop transporter UI.
/// </summary>
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
    float holdTime;
    /// <summary>
    /// Adjusts the troops of both territories by some amount. Decreasing the from territory and increasing the to territory. A negative number will do the inverse. 
    /// </summary>
    /// <param name="count">The adjustment to be made. Default value is +1.</param>
    public void UpdateTroopCounts(int count=1)            
    {
        //updates the display and local variables to current troop distribution
        AudioManagement.PlaySound("Transfer Click");
        fromTroopCount -= count;
        toTroopCount += count;
        fromTroopDisplay.text = fromTroopCount.ToString();
        toTroopDisplay.text = toTroopCount.ToString();
    }
    /// <summary>
    /// Setup the troop transporter UI. This is only run when a player owns one of the territories.
    /// </summary>
    /// <param name="toTerritory">The territory troops are being transferred to.</param>
    /// <param name="fromTerritory">The territory troops are being transferred from.</param>
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
        fromTerritoryName.SetText(fromTerritory.GetName());
        toTerritoryName.SetText(toTerritory.GetName());
    }
    /// <summary>
    /// Setup the troop transporter UI. This is only run when a player owns one of the territories. Used when there is no from territory, such as during the deploy phase.
    /// </summary>
    /// <param name="toTerritory">The territory troops are being transferred to.</param>
    /// <param name="fromTroopCount">The maximun amount of troops that can be transported to that territory.</param>
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
        toTerritoryName.SetText(toTerritory.GetName());
    }
    /// <summary>
    /// Automatically runs at the end to troop transfer and in shows that the territories have the correct amount of troops.
    /// </summary>
    /// <returns>The amount of troops left to deploy or zero if not in deploy phase.</returns>
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

    private void Update()
    {
        if (Input.GetKey(KeyCode.RightArrow)|| Input.GetKey(KeyCode.D))
        {
            if (((fromTerritory != null && fromTroopCount > 1) || (fromTerritory == null && fromTroopCount > 0)) && Time.time-holdTime>1/7f)
            {
                holdTime = Time.time;
                UpdateTroopCounts();
            }       
        }
        else if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) && Time.time - holdTime > 1 / 7f)
        {
            if (toTroopCount > toTerritory.GetCurrentTroops())
            {
                UpdateTroopCounts(-1);
            }
            else
            {
                int allTroopCount;
                if (fromTerritory==null)
                {
                    allTroopCount = fromTroopCount;
                }
                else
                {
                    allTroopCount = fromTroopCount-1;
                }

                UpdateTroopCounts(allTroopCount);
            }

            holdTime = Time.time;
        }
    }
}