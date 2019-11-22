using SynodicArc.Champions.Abilities;
using SynodicArc.Champions.Battle.CombatField;
using SynodicArc.Champions.Inputs;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SynodicArc.Champions.Battle
{
    struct BattleAITargetContainer
    {
        public BattleAbility battleAbility;
        public BattleCharacter battleCharacter;
        public int value;
    }


    public class BattleCharacterAI : MonoBehaviour
    {
        //---Private Variables for basic functionality---//
        private bool initialized = false;
        private bool active = false;

        //---Private variables for battle data--//
        private BattleCharacterAIType correspondingBattleCharacterAIType;

        private BattlePartyStateManager battlePartyStateManager;

        private BattleCharacter correspondingBattleCharacter;
        private BattleParty correspondingAllyParty;
        private BattleParty correspondingEnemyParty;

        private List<BattleAbility> correspondingBattleAbilities;

        private List<BattleCharacter> activeTargets;

        private Sides playerSide = Sides.Left;

        private BattleAITargetContainer waitingToUseAbility;

        private float timeSinceLastAction = 0.0f;
        private float timeBetweenActionsExpected = 3.0f; //the amount of time until AI checks for another action. Can be altered.

        private float timeBetweenActionsMin = 3.0f; //minimum waiting time before using next action

        private static float timeOfLastAbility = 0.0f; //the time when the last ability took place by any AI character
        private const float timeBetweenAIAbilitiesMin = 1.0f; //the minimum time between any 2 abilities by any AI characters

        private float altTimeScale = 0.0f;

        #region Update

        private void Start()
        {
            InitializeForScene();
        }

        private void Update()
        {
            if(active)
            {
                altTimeScale = Time.time;

                if(correspondingBattleCharacter.IsAlive())
                {
                    timeSinceLastAction += Time.deltaTime;

                    if (timeSinceLastAction > timeBetweenActionsExpected && Time.time > (timeOfLastAbility + timeBetweenAIAbilitiesMin))
                    {
                        LookForAction();
                    }

                }

            }

        }


        

        #endregion Update

        #region Initialization
        /// <summary>
        /// Initializes any special data needed for setting up the battle character AI.
        /// </summary>
        public void InitializeBattleCharacterAI(BattleCharacter battleCharacter, BattleCharacterAIType battleCharacterAIType)
        {
            if (initialized)
                return;

            correspondingBattleCharacter = battleCharacter;
            correspondingBattleCharacterAIType = battleCharacterAIType;

            initialized = true;
        }


        /// <summary>
        /// Initializes the battle character AI for a battle.
        /// </summary>
        public void InitializeForScene()
        {
            InitializeMiscellaneousData();

            FindBattleParties();

            //Only set AI to active at the beginning if it's actually an AI input type
            if(correspondingBattleCharacter.GetInputType() == InputTypes.AI)
            {
                active = true;
            }
        }


        /// <summary>
        /// Initializes all miscellaneous data
        /// </summary>
        private void InitializeMiscellaneousData()
        {
            activeTargets = new List<BattleCharacter>();
            correspondingBattleAbilities = correspondingBattleCharacter.GetEquippedBattleAbilities();

        }


        /// <summary>
        /// Finds and assigns default battle parties
        /// </summary>
        private void FindBattleParties()
        {
            try
            {
                correspondingAllyParty = GameObject.FindObjectsOfType<BattleParty>().Where(x => x.GetOwnerID() == correspondingBattleCharacter.GetOwnerID()).FirstOrDefault();
                playerSide = correspondingAllyParty.GetBattleSide();
                battlePartyStateManager = GameObject.FindObjectsOfType<BattlePartyStateManager>().Where(x => x.GetRespectiveBattleParty().Equals(correspondingAllyParty)).FirstOrDefault();
            }
            catch
            {
                Debug.LogError("NO ALLY PARTY FOUND FOR ID: " + correspondingBattleCharacter.GetOwnerID());
            }

            try
            {
                correspondingEnemyParty = GameObject.FindObjectsOfType<BattleParty>().Where(x => x.GetBattleSide() != correspondingAllyParty.GetBattleSide() && x.GetBattleSide() != Sides.Unassigned).FirstOrDefault();
            }
            catch
            {
                Debug.LogError("NO ENEMY PARTY FOUND FOR ID: " + correspondingBattleCharacter.GetOwnerID());
            }
        }

        #endregion Initialization


        #region Determine Action
        /// <summary>
        /// Look for an action to perform
        /// </summary>
        private void LookForAction()
        {
            if (!initialized)
                return;

            if (correspondingBattleCharacter.GetCurrentEnergyBars() < 1)
            {
                //wait some time before trying again
                timeBetweenActionsExpected += 0.5f; //TODO fix time to a constant
                return;
            }

            Dictionary<BattleAbility, Dictionary<BattleCharacter, int>> abilitiesAndTargets = new Dictionary<BattleAbility, Dictionary<BattleCharacter, int>>();
            List<BattleAITargetContainer> baitcList = new List<BattleAITargetContainer>();


            foreach (BattleAbility ba in correspondingBattleAbilities)
            {
                Dictionary<BattleCharacter, int> targetsAndDamage = new Dictionary<BattleCharacter, int>();

                List<BattleCharacter> targetCharacters = new List<BattleCharacter>();

                //Get our targets for the battle ability
                if (!ba.IsTargetAlly())
                {
                    foreach (BattleCharacter bc in correspondingEnemyParty.GetTargetableBattleCharacters(false, ba.IsRanged(), correspondingBattleCharacter.IsHawkeye(), ba.IsCanTargetDead()))
                    {
                        targetCharacters.Add(bc);
                    }
                }
                else if (ba.IsTargetAlly()) //target allies
                {
                    foreach (BattleCharacter bc in correspondingAllyParty.GetTargetableBattleCharacters(true, ba.IsRanged(), correspondingBattleCharacter.IsHawkeye(), ba.IsCanTargetDead()))
                    {
                        targetCharacters.Add(bc);
                    }
                }


                foreach (BattleCharacter bc in targetCharacters)
                {
                    int valueToCharacter = 0;

                    foreach (AbilityEffect ae in ba.GetAbilityEffects())
                    {
                        activeTargets.Clear();

                        //Get what our targets are for this ability effect
                        switch (ba.GetTargetType())
                        {
                            case (AbilityTargetTypes.SingleTarget):
                                activeTargets.Add(bc);
                                break;
                            case (AbilityTargetTypes.SameRow):
                                activeTargets = bc.GetSameRow();
                                break;
                            case (AbilityTargetTypes.SameLine):
                                activeTargets = bc.GetSameLine();
                                break;
                            case (AbilityTargetTypes.AllTargets):
                                activeTargets = bc.GetSameParty();
                                break;
                        }

                        //Calculate the effect on all applicable targets
                        foreach (BattleCharacter at in activeTargets)
                        {
                            float valueMod = GetValueMod(ba.GetTargetType(), at, ba.GetRequiredEnergyBars());

                            switch (ae.AbilityEffectType)
                            {
                                case (AbilityEffectTypes.RawDamage):
                                case (AbilityEffectTypes.PercentDamage):
                                case (AbilityEffectTypes.InstantDeath):
                                    valueToCharacter += (int)(at.CalculateDamage(ae.IntValue, false, ae.StaticDamage, ba.GetElement(), correspondingBattleCharacter.IsStrup(),
                                        correspondingBattleCharacter.IsStrda(), ae.BypassShields, ae.BypassBarrier, ae.Stun, correspondingBattleCharacter.IsBlind()) * valueMod);
                                    break;
                                case (AbilityEffectTypes.RawHeal):
                                case (AbilityEffectTypes.PercentHeal):
                                case (AbilityEffectTypes.Resurrect):
                                    valueToCharacter += (int)(at.CalculateHeal(ae.IntValue) * valueMod);
                                    break;
                            }

                        }

                    }

                    targetsAndDamage.Add(bc, valueToCharacter);

                    BattleAITargetContainer baitc;
                    baitc.battleAbility = ba;
                    baitc.battleCharacter = bc;
                    baitc.value = valueToCharacter;

                    baitcList.Add(baitc);
                }

            }




            //Make sure we have at least one action
            if(baitcList.Count <= 0)
            {
                timeBetweenActionsExpected += 1.0f; //TODO fix time
                Debug.Log("No actions found.");
                return;
            }


            baitcList = baitcList.OrderByDescending(x => x.value).ToList();


            if (baitcList[0].value <= 0)
            {
                //wait some time before trying again
                timeBetweenActionsExpected += 0.5f; //TODO fix time
                return;
            }



            int baitcListRange = 2;

            if (baitcList.Count < 3)
            {
                baitcListRange = baitcList.Count - 1;
            }


            int topAbilitiesTotalValue = 0;


            foreach(BattleAITargetContainer baitc in baitcList.GetRange(0, baitcListRange))
            {
                topAbilitiesTotalValue += baitc.value;
            }

            BattleAITargetContainer selectedBaitc;

            int abilitySelectSeed = Random.Range(0, topAbilitiesTotalValue);

            if(abilitySelectSeed >= 0 && abilitySelectSeed < baitcList[0].value)
            {
                selectedBaitc = baitcList.ElementAt(0);
            }
            else if(baitcList.Count > 1 && abilitySelectSeed >= baitcList[0].value && abilitySelectSeed < (baitcList[0].value + baitcList[1].value))
            {
                selectedBaitc = baitcList.ElementAt(1);
            }
            else if(baitcList.Count > 2 && abilitySelectSeed >= (baitcList[0].value + baitcList[1].value) && abilitySelectSeed < (baitcList[0].value + baitcList[1].value + baitcList[2].value))
            {
                selectedBaitc = baitcList.ElementAt(2);
            }
            else
            {
                Debug.LogError("WARNING: Did not find proper ability. Setting to best ability.");
                selectedBaitc = baitcList.ElementAt(0);
            }

            //If we're waiting on using an ability, let's check to see if the selected ability is weaker/less useful than it
            if(waitingToUseAbility.battleAbility != null)
            {
                if(selectedBaitc.value < waitingToUseAbility.value)
                {
                    selectedBaitc = waitingToUseAbility;
                }
            }

            //Must reach our minimum value to be worth using, otherwise wait a bit longer
            if (selectedBaitc.value <= 0)
            {
                //wait some time before trying again
                timeBetweenActionsExpected += 0.5f; //TODO fix time to constant
                return;
            }
            else if (!correspondingBattleCharacter.CanPerformAbility(selectedBaitc.battleAbility))
            {
                SetWaitingToUseAbility(selectedBaitc);
                timeBetweenActionsExpected += 0.5f; //TODO fix time to constant
                return;
            }


            timeSinceLastAction = 0.0f;
            timeBetweenActionsExpected = correspondingBattleCharacterAIType.GetTimeBetweenActions();
            correspondingBattleCharacter.PerformAbility(selectedBaitc.battleCharacter, selectedBaitc.battleAbility, InputTypes.AI, selectedBaitc.battleAbility.IsCharge());
            timeOfLastAbility = Time.time;
            ClearWaitingToUseAbility();
        }

        /// <summary>
        /// Modifies the value of an ability based on AI-specific parameters to cause certain targets to be prioritized over others.
        /// </summary>
        public float GetValueMod(AbilityTargetTypes abilityTargetType, BattleCharacter targetOfAction, int requiredEnergyBars)
        {
            float valueMod = 1.0f;

            valueMod -= (1.0f - (.15f * (requiredEnergyBars - 1)));

            if(abilityTargetType == correspondingBattleCharacterAIType.GetAbilityTargetPriority())
            {
                valueMod += 0.3f;
            }

            switch (correspondingBattleCharacterAIType.GetBattleAIFocusPriority())
            {
                case (BattleAIFocusPriorities.LowestHealth):
                    if (targetOfAction.IsHighestHealthInParty())
                        valueMod -= 0.2f;
                    else if (targetOfAction.IsLowestHealthInParty())
                        valueMod += 0.5f;
                    break;
                case (BattleAIFocusPriorities.WellRounded):
                    if (!targetOfAction.IsHighestHealthInParty() && !targetOfAction.IsLowestHealthInParty())
                        valueMod += 0.25f;
                    break;
                case (BattleAIFocusPriorities.HighestHealth):
                    if (targetOfAction.IsHighestHealthInParty())
                        valueMod += 0.5f;
                    else if (targetOfAction.IsLowestHealthInParty())
                        valueMod -= 0.2f;
                    break;
            }

            return valueMod;
        }

        #endregion Determine Action

        #region Action Helpers
        /// <summary>
        /// Sets the ability that we are waiting on energy bars or another parameter to use
        /// </summary>
        private void SetWaitingToUseAbility(BattleAITargetContainer waitingToUseBaitc)
        {
            waitingToUseAbility.battleAbility = waitingToUseBaitc.battleAbility;
            waitingToUseAbility.battleCharacter = waitingToUseBaitc.battleCharacter;
            waitingToUseAbility.value = (int) (waitingToUseBaitc.value * 1.2f); //TODO fix mod
        }

        /// <summary>
        /// Clears our the waiting to use ability
        /// </summary>
        private void ClearWaitingToUseAbility()
        {
            waitingToUseAbility.battleAbility = null;
            waitingToUseAbility.battleCharacter = null;
            waitingToUseAbility.value = 0;
        }

        #endregion Action Helpers



    }
}