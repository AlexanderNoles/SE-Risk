using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class represents the player local to this machine
/// </summary>
public class LocalPlayer : Player
{
    public CardDisplayer cardDisplayer;

    private void Start()
    {
        PlayerInputHandler.SetLocalPlayer(this);
    }
    /// <summary>
    /// Lets the local player choose which territory to deploy their capital in
    /// </summary>
    /// <param name="territories">The list of avaialable territories to place the capital in</param>
    public override void ClaimCapital(List<Territory> territories)
    {
        this.territories = territories;
        PlayerInputHandler.Setup(claimingCapital: true);
    }
    /// <summary>
    /// The repeatable action of the setup phase, lets the local player place exactly 1 troop in a free territory if one exists, if not than in a territory that player owns
    /// </summary>
    /// <param name="territories">The list of valid territories for the player to place a troop on</param>
    public override void Setup(List<Territory> territories)
    {
        this.territories = territories;
        PlayerInputHandler.Setup();
    }
    /// <summary>
    /// Lets the local player take their "Deploy Phase" i.e lets them place a number of troops in a territory they own
    /// </summary>
    /// <param name="territories">The territories the player owns</param>
    /// <param name="troopCount">The number of troops the player can deploy</param>
    /// <returns></returns>
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
    /// <summary>
    /// Lets the local player take their "Attack Phase", i.e lets them request attacks for territories they own to ones they do not
    /// </summary>
    /// <returns></returns>
    public override void Attack()
    {
        cardDisplayer.SetAbleToTurnInCards(false);
        PlayerInputHandler.Attack();
    }
    /// <summary>
    /// Handles the changes to the local player at the conclusion of an attack, such as setting the correct number of troops to each territory
    /// </summary>
    /// <param name="attackResult">The result of the attack</param>
    /// <param name="attacker">The territory that the attack was made from</param>
    /// <param name="defender">The attacked territory</param>
    /// <param name="attackerDiceCount">The number of dice the attacker attacked with</param>
    public override void OnAttackEnd(Map.AttackResult attackResult, Territory attacker, Territory defender, int attackerDiceCount)
    {
        if (attackResult == Map.AttackResult.Won)
        {
            MatchManager.WinCheck(this);
            defender.SetCurrentTroops(attacker.GetCurrentTroops() - 1 >= attackerDiceCount ? attackerDiceCount : attacker.GetCurrentTroops() - 1);
            attacker.SetCurrentTroops(attacker.GetCurrentTroops() - defender.GetCurrentTroops());
            AudioManagement.PlaySound("Territory Capture");
            territoryTakenThisTurn = true;
        }
        PlayerInputHandler.OnAttackEnd(attackResult, attacker, defender);
    }
    /// <summary>
    /// Lets the player take the fortify phase of their turn, i.e lets them move some number of troops from one territory to exactly one other
    /// </summary>
    public override void Fortify()
    {
        PlayerInputHandler.Fortify();
    }
    /// <summary>
    /// Returns the number of troops the player is holding (yet to place on the board)
    /// </summary>
    /// <returns>The number of held troops</returns>
    public int GetTroopCount(){ return troopCount; }
    /// <summary>
    /// Sets the number of troops the player is holding (yet to deploy)
    /// </summary>
    /// <param name="troopCount">The number of troops to set the players hand to</param>
    public void SetTroopCount(int troopCount) { this.troopCount = troopCount; }
    /// <summary>
    /// Handles the functions that need to run at the end of the turn, such as drawing a new card
    /// </summary>
    public override void OnTurnEnd()
    {
        turnReset = false;
        base.OnTurnEnd();
        DisplayNewCard();
    }
    /// <summary>
    /// Shows the last card in the local players hand
    /// </summary>
    public void DisplayNewCard()
    {
        if (territoryTakenThisTurn)
        {
            cardDisplayer.SetCard(hand.GetLastCard());
            cardDisplayer.UpdateCardVisuals();
            StartCoroutine(ShowOneCard());
        }
    }
    /// <summary>
    /// The Coroutine that manages showing the last card in the players hand
    /// </summary>
    private IEnumerator ShowOneCard()
    {
        cardDisplayer.ShowOneCard();
        yield return new WaitForSecondsRealtime(3);
        cardDisplayer.HideCards(false);
    }
    /// <summary>
    /// Sets the hand to be displayed in the card displayer
    /// </summary>
    public void SetCardDisplayerHand()
    {
        cardDisplayer.SetHand(hand);
    }

}
