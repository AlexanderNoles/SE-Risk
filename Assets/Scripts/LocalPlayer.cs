using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayer : Player
{
    public CardDisplayer cardDisplayer;

    private void Start()
    {
        PlayerInputHandler.SetLocalPlayer(this);
    }

    public override void ClaimCapital(List<Territory> territories)
    {
        this.territories = territories;
        PlayerInputHandler.Setup(claimingCapital: true);
    }

    public override void Setup(List<Territory> territories)
    {
        this.territories = territories;
        PlayerInputHandler.Setup();
    }
    public override bool Deploy(List<Territory> territories, int troopCount)
    {
        if (turnReset)
        {
            cardDisplayer.SetHand(hand);
            cardDisplayer.UpdateCardVisuals();
            cardDisplayer.ShowCards(true);
        }
        this.troopCount = troopCount;
        this.territories = territories;
        territoryTakenThisTurn = false;
        cardDisplayer.SetAbleToTurnInCards(true);
        PlayerInputHandler.Deploy();
        return true;
    }
    public override bool Attack()
    {
        cardDisplayer.SetAbleToTurnInCards(false);
        PlayerInputHandler.Attack();
        return true;
    }
    public override void OnAttackEnd(Map.AttackResult attackResult, Territory attacker, Territory defender)
    {
        if (attackResult == Map.AttackResult.Won)
        {
            AudioManagement.PlaySound("Territory Capture");
            territoryTakenThisTurn = true;
        }
        PlayerInputHandler.OnAttackEnd(attackResult, attacker, defender);
    }

    public override void Fortify()
    {
        PlayerInputHandler.Fortify();
    }
    public int GetTroopCount(){ return troopCount; }
    public void SetTroopCount(int troopCount) { this.troopCount = troopCount; }

    public override void OnTurnEnd()
    {
        turnReset = false;
        base.OnTurnEnd();
        DrawAndDisplayNewCard();
    }

    public void DrawAndDisplayNewCard()
    {
        if (territoryTakenThisTurn)
        {
            cardDisplayer.SetCard(hand.GetLastCard());
            cardDisplayer.UpdateCardVisuals();
            StartCoroutine(ShowOneCard());
        }
    }
    private IEnumerator ShowOneCard()
    {
        cardDisplayer.ShowOneCard();
        yield return new WaitForSecondsRealtime(3);
        cardDisplayer.HideCards(false);
    }

    public void SetCardDisplayerHand()
    {
        cardDisplayer.SetHand(hand);
    }

}
