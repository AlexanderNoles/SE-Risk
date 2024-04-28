using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// <c>DiceRollMenu</c> is a monobehaviour class that controls the dice rolling UI
/// </summary>
public class DiceRollMenu : MonoBehaviour
{
    /// <summary>
    /// A serialized list of GameObjects to be set active after setup. It is done this way to avoid the UI flashing on screen for one frame if no dice actually need to be rolled.
    /// </summary>
    public List<GameObject> thingsToSetActiveAfter;

    //Indicates if the player needs to select a number of troops to defend/attack withs
    private static bool active;
    private static bool currentSetupDone;
    private static bool playerIsAttacker;

    private static Territory attackingTerritory;
    private static Territory defendingTerritory;

    private const int minDice = 1;
    private static int maxDice;
    private static int currentNumberOfDice;

    [Header("References")]
    /// <summary>
    /// Text UI element
    /// </summary>
    public TextMeshProUGUI descripter;
    /// <summary>
    /// Number UI element
    /// </summary>
    public TextMeshProUGUI diceCount;

    /// <summary>
    /// Returns the active state of the UI
    /// </summary>
    /// <returns>true or false</returns>
    public static bool IsActive()
    {
        return active;
    }

    private void Awake()
    {
        active = false;
    }

    /// <summary>
    /// <c>Activate</c> is called when the UI needs to be activated. This is done when the localPlayer needs to make a decision about the number of dice to rolled. Sometimes the number of dice to be rolled is a range of a single value, in this case, the UI will immediately close.
    /// </summary>
    /// <param name="attacker">The Attacking Territory</param>
    /// <param name="defender">The Defending Territory</param>
    /// <param name="isAttacker">Does the attacking Territory belong to the player?</param>
    public static void Activate(Territory attacker, Territory defender, bool isAttacker) 
    {
        active = true;
        playerIsAttacker = isAttacker;

        attackingTerritory = attacker;
        defendingTerritory = defender;

        //Minus one from max dice as it is originally intended to be used for a max exclusive random number generator
        //(the one currently used by the ai)
        if (playerIsAttacker)
        {
            maxDice = MatchManager.GetPlayerFromIndex(attacker.GetOwner()).GetMaxAttackingDice(attacker);
        }
        else
        {
            maxDice = MatchManager.GetPlayerFromIndex(defender.GetOwner()).GetMaxDefendingDice(defender);
        }

        currentNumberOfDice = maxDice;
        currentSetupDone = false;
    }

    private void Update()
    {
        if (active)
        {
            if (playerIsAttacker && Input.GetKeyDown(KeyCode.Escape))
            {
                //Cancel attack
                //Just need to notify the player
                UIManagement.SetActiveGreyPlane(false);
                PlayerInputHandler.OnAttackEnd(Map.AttackResult.Cancelled, attackingTerritory, defendingTerritory);
                active = false;
                return;
            }

            //Confirm number of dice to be rolled
            if (maxDice == minDice || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                int attackingDice, defendingDice;
                AudioManagement.PlaySound("Attack");
                //Get dice for both the player and whoever they are attacking
                if (playerIsAttacker)
                {
                    attackingDice = currentNumberOfDice;
                    defendingDice = MatchManager.GetPlayerFromIndex(defendingTerritory.GetOwner()).GetDefendingDice(defendingTerritory);
                }
                else
                {
                    attackingDice = MatchManager.GetPlayerFromIndex(attackingTerritory.GetOwner()).GetAttackingDice(attackingTerritory);
                    defendingDice = currentNumberOfDice;
                }

                UIManagement.SetActiveGreyPlane(false);

                //Actually call attack
                Map.Attack(attackingTerritory, defendingTerritory, attackingDice, defendingDice);

                active = false;
                return;
            }
            else if (!currentSetupDone)
            {
                //We run this here rather than in the Activate in case the above case of min and max being the same occurs
                //in which case no choice needs to be made
                currentSetupDone = true;

                string descripterText = 
                    "Number of dice to " + 
                    (playerIsAttacker ? "attack " : "defend ") +
                    defendingTerritory.name
                    + " with:";

                descripter.text = descripterText;

                UIManagement.SetActiveGreyPlane(true);
                foreach (GameObject gameObject in thingsToSetActiveAfter)
                {
                    gameObject.SetActive(true);
                }

                //Update ui elements
                UpdateDiceCountText();
            }

            //Register actual input from player
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                AdjustCurrentNumberOfDice(1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                AdjustCurrentNumberOfDice(-1);
            }
        }
        else if (!active && thingsToSetActiveAfter[0].activeSelf)
        {
            //Hide all UI elements if they are active
            foreach(GameObject gameObject in thingsToSetActiveAfter)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void AdjustCurrentNumberOfDice(int adjustment)
    {
        currentNumberOfDice = Mathf.Clamp(currentNumberOfDice + adjustment, minDice, maxDice);
        UpdateDiceCountText();
    }

    private void UpdateDiceCountText()
    {
        diceCount.text = currentNumberOfDice.ToString();
    }
}
