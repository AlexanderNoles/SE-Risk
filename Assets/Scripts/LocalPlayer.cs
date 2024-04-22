using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayer : Player
{
    private void Start()
    {
        PlayerInputHandler.SetLocalPlayer(this);
    }

    public override void Setup(List<Territory> territories)
    {
        this.territories = territories;
        PlayerInputHandler.Setup();
    }
    public override bool Deploy(List<Territory> territories, int troopCount)
    {
        this.troopCount = troopCount;
        this.territories = territories;
        PlayerInputHandler.Deploy();
        return true;
    }
    public override bool Attack()
    {
        PlayerInputHandler.Attack();
        return true;
    }
    public override void OnAttackEnd(Map.AttackResult attackResult, Territory attacker, Territory defender)
    {
        PlayerInputHandler.OnAttackEnd(attackResult, attacker, defender);
    }

    public override void Fortify()
    {
        PlayerInputHandler.Fortify();
    }
    public int GetTroopCount(){ return troopCount; }
    public void SetTroopCount(int troopCount) { this.troopCount = troopCount; }


}
