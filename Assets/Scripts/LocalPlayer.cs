using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayer : Player
{
    private void Start()
    {
        PlayerInputHandler.SetLocalPlayer(this);
    }

    int troopCount;
    List<Territory> territories;
    public override bool Deploy(List<Territory> territories, int troopCount)
    {
        this.troopCount = troopCount;
        this.territories = territories;
        PlayerInputHandler.StopWaiting();
        return true;
    }
    public override bool Attack(List<Territory> territories)
    {

        return true;
    }
        public int GetTroopCount(){ return troopCount; }
    public void SetTroopCount(int troopCount) { this.troopCount = troopCount; }
    public List<Territory> GetTerritories() {  return territories; }


}
