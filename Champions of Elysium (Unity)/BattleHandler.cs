using SynodicArc.Champions.Battle.CombatField;
using SynodicArc.Champions.Core;
using SynodicArc.Champions.Inputs;
using SynodicArc.Champions.PartySystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace SynodicArc.Champions.Battle
{
    /// <summary>
    /// Initializes the battle system and its main components.
    /// </summary>
    public class BattleHandler : BaseHandler
    {
        public List<Party> parties;
        public GameObject characterBasePrefab;
		public GameObject LeftSideBattlePartyUI;
		public GameObject RightSideBattlePartyUI;

        public GameObject CharacterBattleEndPanelPrefabObject_PO;
        public GameObject UIBattleItemPrefabObject_PO;

        //---Private variables for initialization---//
        private GameObject battlePartyContainer;
		private GameObject battlePartyUIContainer;

        //---Private variables for basic functionality---//
        private BattleLiason battleLiason;

        // Use this for initialization
        public override void Initialize()
        {
            Debug.LogError("Warning: Cannot use standard Initialize method for BattleHandler.");
        }

        public void Initialize(Party playerParty, Party enemyParty)
        {
            inputManager = GameObject.FindObjectOfType<InputManager>();

            InitializeBattleParties(playerParty, enemyParty);
            InitializeBattleLiason();
            InitializeControlSystem();

            initialized = true;
        }


        #region Initialization
        /// <summary>
        /// Initializes the battle parties.
        /// </summary>
        private void InitializeBattleParties(Party playerParty, Party enemyParty)
        {
            battlePartyContainer = GameObject.FindGameObjectWithTag(BattleConstants.BattlePartyContainerTag);
			battlePartyUIContainer = GameObject.FindGameObjectWithTag(BattleConstants.BattlePartyUIContainerTag);
            List<Sides> assignedSides = new List<Sides>();

            if(playerParty != null)
            {
                GameObject newBattleParty = new GameObject();
                Sides side = DetermineSide(playerParty, assignedSides);
                newBattleParty.AddComponent<BattleParty>().ConvertPartyToBattleParty(playerParty, characterBasePrefab, newBattleParty.transform, side, 2);
                CreateBattlePartyUI(newBattleParty.GetComponent<BattleParty>(), side);
                newBattleParty.AddComponent<BattlePartyStateManager>().InitializeBattlePartyStateManager(newBattleParty.GetComponent<BattleParty>());

                newBattleParty.transform.SetParent(battlePartyContainer.transform, false);
                newBattleParty.name = playerParty.GetOwnerID() + BattleConstants.PartyNamingConvention;
            }


            if (enemyParty != null)
            {
                GameObject newBattleParty = new GameObject();
                Sides side = DetermineSide(enemyParty, assignedSides);
                newBattleParty.AddComponent<BattleParty>().ConvertPartyToBattleParty(enemyParty, characterBasePrefab, newBattleParty.transform, side, 2);
                CreateBattlePartyUI(newBattleParty.GetComponent<BattleParty>(), side);
                newBattleParty.AddComponent<BattlePartyStateManager>().InitializeBattlePartyStateManager(newBattleParty.GetComponent<BattleParty>());
            
                newBattleParty.transform.SetParent(battlePartyContainer.transform, false);
                newBattleParty.name = enemyParty.GetOwnerID() + BattleConstants.PartyNamingConvention;
            }

        }

		/// <summary>
		/// Determines the side for this party to appear on.
		/// </summary>
        private Sides DetermineSide(Party party, List<Sides> assignedSides)
        {
			//if the preferred side isn't taken, we can assign it
            if (!assignedSides.Contains(party.SidePreference))
            {
                assignedSides.Add(party.SidePreference);
                return party.SidePreference;
            }
            else
            {
                Sides[] allPossibleSides = (Sides[])Enum.GetValues(typeof(Sides));
                foreach(Sides possibleSide in allPossibleSides)
                {
                    if(assignedSides.Contains(possibleSide) && possibleSide != Sides.Unassigned)
                    {
                        continue;
                    }
                    else if (!assignedSides.Contains(possibleSide) && possibleSide != Sides.Unassigned)
                    {
                        assignedSides.Add(possibleSide);
                        return possibleSide;
                    }
                    else
                    {
                        return Sides.Unassigned;
                    }
                }
            }

            Debug.LogError("Error: Did not assign a side successfully.");
            return Sides.Unassigned;

        }

		/// <summary>
		/// Creates the battle party UI for the respective side.
		/// </summary>
		private void CreateBattlePartyUI(BattleParty battleParty, Sides side)
		{
			GameObject battlePartyUI = null;

			if (side == Sides.Left)
			{
				battlePartyUI = Instantiate(LeftSideBattlePartyUI);
			}
			else if (side == Sides.Right)
			{
				battlePartyUI = Instantiate(RightSideBattlePartyUI);
			}
			else
			{
				Debug.LogError("Cannot assign a battle party UI. Side attempted: " + side.ToString());
			}

			if (battlePartyUI != null)
			{
                battlePartyUI.GetComponent<BattlePartyUI>().InitializeBattlePartyUI(battleParty, UIBattleItemPrefabObject_PO);
				battlePartyUI.transform.SetParent(battlePartyUIContainer.transform, false);
			}
		}

        /// <summary>
        /// Gets and assigns the parties for this battle.
        /// </summary>
        private void AssignParties()
        {
            parties = null;
        }

        /// <summary>
        /// Initializes the battle liason.
        /// </summary>
        private void InitializeBattleLiason()
        {
            battleLiason = GameObject.FindObjectOfType<BattleLiason>();
            battleLiason.InitializeBattleLiason();
        }

        /// <summary>
        /// Initializes the control system.
        /// </summary>
        protected override void InitializeControlSystem()
        {
            inputConverterContainer = GameObject.FindGameObjectWithTag(InputConstants.InputConverterContainerTag);
            InputReader[] inputReaders = GameObject.FindObjectsOfType<InputReader>().Where(x => x.IsActive()).ToArray();
            
            if (inputManager.GetBattleInputConverters() != null && inputManager.GetBattleInputConverters().Count > 0)
            {
                foreach (BattleInputConverter bic in inputManager.GetBattleInputConverters())
                {
                    bic.InitializeForScene();
                }
            
                inputManager.OpenHandler(Game.Handlers.Battle);
            
                return;
            }

            inputManager.OpenHandler(Game.Handlers.Battle);
        }
        #endregion Initialization

    }
}