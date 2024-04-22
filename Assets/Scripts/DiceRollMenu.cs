using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DiceRollMenu : MonoBehaviour
{
    public List<GameObject> thingsToSetActiveAfter;

    //Indicates if the player needs to slec
    private static bool active;
    private static bool currentSetupDone;
    private static bool playerIsAttacker;

    private static Territory attackingTerritory;
    private static Territory defendingTerritory;

    private const int minDice = 1;
    private static int maxDice;
    private static int currentNumberOfDice;

    [Header("References")]
    public TextMeshProUGUI descripter;
    public TextMeshProUGUI diceCount;

    public static bool IsActive()
    {
        return active;
    }

    private void Awake()
    {
        active = false;
    }

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
            maxDice = attacker.GetOwner().GetMaxAttackingDice(attacker) - 1;
        }
        else
        {
            maxDice = defender.GetOwner().GetMaxDefendingDice(defender) - 1;
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

                if (playerIsAttacker)
                {
                    attackingDice = currentNumberOfDice;
                    defendingDice = defendingTerritory.GetOwner().GetDefendingDice(defendingTerritory);
                }
                else
                {
                    attackingDice = attackingTerritory.GetOwner().GetAttackingDice(attackingTerritory);
                    defendingDice = currentNumberOfDice;
                }

                UIManagement.SetActiveGreyPlane(false);
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
                    (playerIsAttacker ? attackingTerritory.name : defendingTerritory.name) 
                    + " with:";

                descripter.text = descripterText;

                UIManagement.SetActiveGreyPlane(true);
                foreach (GameObject gameObject in thingsToSetActiveAfter)
                {
                    gameObject.SetActive(true);
                }

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
        else if (!active)
        {
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