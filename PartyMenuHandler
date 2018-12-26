using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SynodicArc.Champions.Battle.CombatField;
using SynodicArc.Champions.Characters;
using SynodicArc.Champions.Core;
using SynodicArc.Champions.Field;
using SynodicArc.Champions.Game;
using SynodicArc.Champions.Inputs;
using SynodicArc.Champions.Inputs.Menu;
using SynodicArc.Champions.Inputs.Menu.Display;
using SynodicArc.Champions.PartyMenu.PrefabObjects;
using SynodicArc.Champions.PartyMenu.PrefabObjects.Characters;
using SynodicArc.Champions.PartyMenu.PrefabObjects.Characters.CharacterInfoPanels;
using SynodicArc.Champions.PartyMenu.Screens.Inventory;
using SynodicArc.Champions.PartyMenu.Screens.Inventory.Accessories;
using SynodicArc.Champions.PartyMenu.Screens.Inventory.BattleItems;
using SynodicArc.Champions.PartyMenu.Screens.Inventory.GeneralItems;
using SynodicArc.Champions.PartyMenu.Screens.Main;
using SynodicArc.Champions.PartyMenu.Screens.Status;
using SynodicArc.Champions.PartySystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SynodicArc.Champions.PartyMenu.Screens;
using SynodicArc.Champions.Items.Categories.Accessories;
using SynodicArc.Champions.PartyMenu.Screens.Controls;
using SynodicArc.Champions.Battle;

namespace SynodicArc.Champions.PartyMenu
{
    /// <summary>
    /// Initializes the party menu.
    /// </summary>
    public class PartyMenuHandler : BaseHandler
    {
        //---Public variables for prefab objects---//
        public GameObject ButtonInfoDisplayPrefabObject_PO;

        //---Public variables for finding references---//
        public Animator PartyMenuAnimator;
        public Transform MenuButtonInfoPanel;
        public List<ButtonInfoDisplayData> PartyMenuButtonInfoDisplayDatas;
        public Transform PopupButtonInfoPanel;
        public List<ButtonInfoDisplayData> PopupButtonInfoDisplayDatas;

        public PartyMenuMainScreen MainScreen;
        public PartyMenuBattleItemsScreen BattleItemsScreen;
        public PartyMenuGeneralItemsScreen GeneralItemsScreen;
        public PartyMenuAccessoriesScreen AccessoriesScreen;

        public PartyMenuStatusScreen StatusScreen;
        public PartyMenuControlsScreen ControlsScreen;


        //---Private variables for finding references---//

        //---Private variables for initialization---//
        private GameObject partyMenuInputConverterContainer;
        private PartyMenuScreens currentPartyMenuScreen = PartyMenuScreens.Closed;

        //---Private variables for data---//
        private Party correspondingParty;

        //---Private variables for functionality---//
        private bool partyMenuOpen = false; //whether or not the party menu is open.
        private Button highlightedButton = null;
        private List<PartyMenuScreens> previousScreens; //the list of previous screens we were on
        private List<ButtonInfoDisplayPrefabObject> partyMenuButtonInfoDisplays;
        private List<ButtonInfoDisplayPrefabObject> popupButtonInfoDisplays;


        public override void Initialize()
        {
            previousScreens = new List<PartyMenuScreens>();
            partyMenuButtonInfoDisplays = new List<ButtonInfoDisplayPrefabObject>();
            popupButtonInfoDisplays = new List<ButtonInfoDisplayPrefabObject>();

            correspondingParty = GameObject.FindGameObjectWithTag(GameConstants.PartyTag).GetComponent<Party>(); //TODO Fix
            inputManager = GameObject.FindObjectOfType<InputManager>();

            InitializeButtonIconDisplays();
            InitializeControlSystem();
            InitializeScreens();

            highlightedButton = MainScreen.GetDefaultHighlightedButton();
            EventSystem.current.SetSelectedGameObject(highlightedButton.gameObject);

            initialized = true;
        }
    	

        #region Initialization
        /// <summary>
        /// Initializes the button icon displays.
        /// </summary>
        private void InitializeButtonIconDisplays()
        {
            //These turn on and off based on buttons
            foreach (ButtonInfoDisplayData bidd in PartyMenuButtonInfoDisplayDatas.OrderBy(x => x.DisplayPriority))
            {
                ButtonInfoDisplayPrefabObject bidd_po = Instantiate(ButtonInfoDisplayPrefabObject_PO).GetComponent<ButtonInfoDisplayPrefabObject>();
                bidd_po.Initialize(bidd);
                bidd_po.name = bidd_po.LocalizeDisplayName();
                bidd_po.transform.SetParent(MenuButtonInfoPanel, false);
                partyMenuButtonInfoDisplays.Add(bidd_po); //Add the newly instantiated object to a saved list.

                bidd_po.gameObject.SetActive(false);
            }

            //These are always on (Popup)
            foreach (ButtonInfoDisplayData bidd in PopupButtonInfoDisplayDatas.OrderBy(x => x.DisplayPriority))
            {
                ButtonInfoDisplayPrefabObject bidd_po = Instantiate(ButtonInfoDisplayPrefabObject_PO).GetComponent<ButtonInfoDisplayPrefabObject>();
                bidd_po.Initialize(bidd);
                bidd_po.name = bidd_po.LocalizeDisplayName();
                bidd_po.transform.SetParent(PopupButtonInfoPanel, false);
                popupButtonInfoDisplays.Add(bidd_po); //Add the newly instantiated object to a saved list.
            }
        }

        /// <summary>
        /// Initializes the control system
        /// </summary>
        protected override void InitializeControlSystem()
        {
            inputConverterContainer = GameObject.FindGameObjectWithTag(InputConstants.InputConverterContainerTag);
            InputReader[] inputReaders = GameObject.FindObjectsOfType<InputReader>().Where(x => x.IsActive()).ToArray();

            if (inputManager.GetPartyMenuInputConverters() != null && inputManager.GetPartyMenuInputConverters().Count > 0)
            {
                foreach (PartyMenuInputConverter pmic in inputManager.GetPartyMenuInputConverters())
                {
                    pmic.InitializeForScene();
                }

                return;
            }

        }

        /// <summary>
        /// Initializes all party menu screens.
        /// </summary>
        private void InitializeScreens()
        {
            MainScreen.Initialize();
            BattleItemsScreen.Initialize();
            GeneralItemsScreen.Initialize();
            AccessoriesScreen.Initialize();
            StatusScreen.Initialize();
            ControlsScreen.Initialize();
            //OptionsScreen.Initialize();
        }

        #endregion Initialization

        #region Update

        /// <summary>
        /// Updates the party menu for new information, such as changing screens.
        /// </summary>
        public void UpdatePartyMenu()
        {
            MainScreen.UpdateScreen();
            BattleItemsScreen.UpdateScreen(); //unused
            GeneralItemsScreen.UpdateScreen(); //unused
            AccessoriesScreen.UpdateScreen(); //unused
            StatusScreen.UpdateScreen();
            ControlsScreen.UpdateScreen(); //TODO better implementation suggested
        }

        /// <summary>
        /// Updates the character stats panels based on a party set type and accessory.
        /// </summary>
        public void UpdateCharacterStatsPanels(PartySetTypes partySetType, Accessory newAccessory = null)
        {
            MainScreen.UpdateCharacterStatsPanels(partySetType, newAccessory);
        }

        #endregion Update

        #region Open/Close
        /// <summary>
        /// Opens the party menu.
        /// </summary>
        public void OpenPartyMenu()
        {
            PartyMenuAnimator.SetBool("Open", true);

            OpenPartyScreen(PartyMenuScreens.Main);
            partyMenuOpen = true;

            GameState.PauseGame();

            StartCoroutine(FinishOpenPartyMenu());
        }

        /// <summary>
        /// Waits a frame before finishing the rest of the opening.
        /// </summary>
        private IEnumerator FinishOpenPartyMenu()
        {
            yield return new WaitForSecondsRealtime(.15f); //TODO fix as constant
            MainScreen.AssignDefaultButton();
        }

        /// <summary>
        /// Closes the party menu.
        /// </summary>
        public void ClosePartyMenu()
        {
            inputManager.CloseHandler();

            PartyMenuAnimator.SetBool("Open", false);
            MainScreen.CloseScreen();
            previousScreens.Clear();
            currentPartyMenuScreen = PartyMenuScreens.Closed;

            GameState.UnpauseGame();

            StartCoroutine(FinishClosePartyMenu()); //need to wait for end of frame due to menu reopening bug in OpenPartyMenu
        }

        /// <summary>
        /// Waits a frame before unpausing.
        /// </summary>
        private IEnumerator FinishClosePartyMenu()
        {
            yield return new WaitForEndOfFrame();
            partyMenuOpen = false;
        }

        /// <summary>
        /// Opens a specified party screen.
        /// </summary>
        public void OpenPartyScreen(PartyMenuScreens newPartyMenuScreen)
        {
            string animName = GetAnimNameForPartyMenuScreen(newPartyMenuScreen);
            PartyMenuAnimator.SetBool(animName, true);
            currentPartyMenuScreen = newPartyMenuScreen;

            if (previousScreens.Count > 0)
            {
                ClosePartyScreen(previousScreens[previousScreens.Count - 1]);
                PartyMenuAnimator.SetBool(GetAnimNameForPartyMenuScreen(previousScreens[previousScreens.Count - 1]), false);
            }
            previousScreens.Add(newPartyMenuScreen);


            switch (newPartyMenuScreen)
            {
                case(PartyMenuScreens.Main):
                    MainScreen.OpenScreen();
                    break;
                case(PartyMenuScreens.GeneralItems):
                    GeneralItemsScreen.OpenScreen();
                    break;
                case(PartyMenuScreens.BattleItems):
                    BattleItemsScreen.OpenScreen();
                    break;
                case(PartyMenuScreens.Accessories):
                    AccessoriesScreen.OpenScreen();
                    break;
                case(PartyMenuScreens.Status):
                    StatusScreen.OpenScreen();
                    break;
                case(PartyMenuScreens.Controls):
                    ControlsScreen.OpenScreen();
                    break;
                case(PartyMenuScreens.Options):
                    //OptionsScreen.OpenScreen();
                    break;
            }

        }

        /// <summary>
        /// Goes back one screen.
        /// </summary>
        public void ReturnToPreviousScreen(PartyMenuScreens closePartyMenuScreen)
        {
            string animName = GetAnimNameForPartyMenuScreen(closePartyMenuScreen);
            PartyMenuAnimator.SetBool(animName, false);

            switch (closePartyMenuScreen)
            {
                case(PartyMenuScreens.Main):
                    MainScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.GeneralItems):
                    GeneralItemsScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.BattleItems):
                    BattleItemsScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Accessories):
                    AccessoriesScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Status):
                    StatusScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Controls):
                    ControlsScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Options):
                    //OptionsScreen.CloseScreen();
                    break;
            }

            previousScreens.Remove(closePartyMenuScreen);
            if (previousScreens.Count > 0)
            {
                currentPartyMenuScreen = previousScreens[previousScreens.Count - 1];

                animName = GetAnimNameForPartyMenuScreen(currentPartyMenuScreen);
                PartyMenuAnimator.SetBool(animName, true);

                switch (currentPartyMenuScreen)
                {
                    case(PartyMenuScreens.Main):
                        MainScreen.OpenScreen();
                        MainScreen.ClearTemporaryReturnToHighlightButton();
                        break;
                    case(PartyMenuScreens.GeneralItems):
                        GeneralItemsScreen.OpenScreen();
                        GeneralItemsScreen.ClearTemporaryReturnToHighlightButton();
                        break;
                    case(PartyMenuScreens.BattleItems):
                        BattleItemsScreen.OpenScreen();
                        BattleItemsScreen.ClearTemporaryReturnToHighlightButton();
                        break;
                    case(PartyMenuScreens.Accessories):
                        AccessoriesScreen.OpenScreen();
                        AccessoriesScreen.ClearTemporaryReturnToHighlightButton();
                        break;
                    case(PartyMenuScreens.Status):
                        StatusScreen.OpenScreen();
                        StatusScreen.ClearTemporaryReturnToHighlightButton();
                        break;
                    case(PartyMenuScreens.Controls):
                        ControlsScreen.OpenScreen();
                        ControlsScreen.ClearTemporaryReturnToHighlightButton();
                        break;
                    case(PartyMenuScreens.Options):
                        //OptionsScreen.OpenScreen();
                        //OptionsScreen.ClearTemporaryReturnToHighlightButton();
                        break;
                }
            }
            else
            {
                currentPartyMenuScreen = PartyMenuScreens.Closed;
            }
        }

        /// <summary>
        /// Closes a specified party screen.
        /// </summary>
        public void ClosePartyScreen(PartyMenuScreens closePartyMenuScreen)
        {
            string animName = GetAnimNameForPartyMenuScreen(closePartyMenuScreen);
            PartyMenuAnimator.SetBool(animName, false);

            switch (closePartyMenuScreen)
            {
                case(PartyMenuScreens.Main):
                    MainScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.GeneralItems):
                    GeneralItemsScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.BattleItems):
                    BattleItemsScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Accessories):
                    AccessoriesScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Status):
                    StatusScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Controls):
                    ControlsScreen.CloseScreen();
                    break;
                case(PartyMenuScreens.Options):
                    //OptionsScreen.CloseScreen();
                    break;
            }

        }

        public void UpdateStatusScreen(CharacterData newCharData)
        {
            StatusScreen.SetCurrentCharacterData(newCharData);
        }

        /// <summary>
        /// Sets the data from the status screen to the accessory screen.
        /// </summary>
        public void SetStatusToAccessoryScreenData(CharacterData newCharData, int newItemSlot)
        {
            AccessoriesScreen.SetCurrentCharacterDataAndItemSlot(newCharData, newItemSlot);
        }
        #endregion Open/Close


        #region Party Menu State Management
        /// <summary>
        /// Gets the animation name for a specified party menu screen.
        /// </summary>
        public string GetAnimNameForPartyMenuScreen(PartyMenuScreens specifiedPartyMenuScreen)
        {
            switch (specifiedPartyMenuScreen)
            {
                case(PartyMenuScreens.Main):
                    return PartyMenuConstants.PartyMenuAnimMain;
                case(PartyMenuScreens.GeneralItems):
                case(PartyMenuScreens.BattleItems):
                case(PartyMenuScreens.Accessories):
                case(PartyMenuScreens.Provisions):
                    return PartyMenuConstants.PartyMenuAnimInventory;
                case(PartyMenuScreens.Status):
                    return PartyMenuConstants.PartyMenuAnimStatus;
                case(PartyMenuScreens.Controls):
                    return PartyMenuConstants.PartyMenuAnimControls;
                //case(PartyMenuScreens.Options):
                //    return null;
                default:
                    Debug.LogError("Not obtaining a proper animation name for the " + specifiedPartyMenuScreen.ToString() + " screen.");
                    return "";
            }
        }
        #endregion Party Menu State Management

        #region Main Controls
        /// <summary>
        /// Sends an input to the party menu.
        /// </summary>
        public void SendPartyMenuInput(MenuInputs menuInput)
        {
            switch (menuInput)
            {
                case(MenuInputs.Up):
                    if (highlightedButton.navigation.selectOnUp != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.navigation.selectOnUp.gameObject);
                        highlightedButton = highlightedButton.navigation.selectOnUp.GetComponent<Button>();
                    }
                    else if (highlightedButton.FindSelectableOnUp() != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.FindSelectableOnUp().gameObject);
                        highlightedButton = highlightedButton.FindSelectableOnUp().GetComponent<Button>();
                    }
                    break;
                case(MenuInputs.Right):
                    if (highlightedButton.navigation.selectOnRight != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.navigation.selectOnRight.gameObject);
                        highlightedButton = highlightedButton.navigation.selectOnRight.GetComponent<Button>();
                    }
                    else if (highlightedButton.FindSelectableOnRight() != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.FindSelectableOnRight().gameObject);
                        highlightedButton = highlightedButton.FindSelectableOnRight().GetComponent<Button>();
                    }
                    break;
                case(MenuInputs.Left):
                    if (highlightedButton.navigation.selectOnLeft != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.navigation.selectOnLeft.gameObject);
                        highlightedButton = highlightedButton.navigation.selectOnLeft.GetComponent<Button>();
                    }
                    else if (highlightedButton.FindSelectableOnLeft() != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.FindSelectableOnLeft().gameObject);
                        highlightedButton = highlightedButton.FindSelectableOnLeft().GetComponent<Button>();
                    }
                    break;
                case(MenuInputs.Down):
                    if (highlightedButton.navigation.selectOnDown != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.navigation.selectOnDown.gameObject);
                        highlightedButton = highlightedButton.navigation.selectOnDown.GetComponent<Button>();
                    }
                    else if (highlightedButton.FindSelectableOnDown() != null)
                    {
                        EventSystem.current.SetSelectedGameObject(highlightedButton.FindSelectableOnDown().gameObject);
                        highlightedButton = highlightedButton.FindSelectableOnDown().GetComponent<Button>();
                    }
                    break;
                case(MenuInputs.Confirm):
                    if (highlightedButton != null)
                        highlightedButton.onClick.Invoke();
                    break;
            }

            switch (currentPartyMenuScreen)
            {
                case(PartyMenuScreens.Main):
                    MainScreen.HandleScreenInputs(menuInput);
                    break;
                case(PartyMenuScreens.GeneralItems):
                    GeneralItemsScreen.HandleScreenInputs(menuInput);
                    break;
                case(PartyMenuScreens.BattleItems):
                    BattleItemsScreen.HandleScreenInputs(menuInput);
                    break;
                case(PartyMenuScreens.Accessories):
                    AccessoriesScreen.HandleScreenInputs(menuInput);
                    break;
                case(PartyMenuScreens.Status):
                    StatusScreen.HandleScreenInputs(menuInput);
                    break;
                case (PartyMenuScreens.Controls):
                    ControlsScreen.HandleScreenInputs(menuInput);
                    break;
                //case (PartyMenuScreens.Options):
                //    OptionsScreen.HandleScreenInputs(menuInput);
                //    break;
            }
        }

        #endregion Main Controls


        #region Getters
        /// <summary>
        /// Checks for irregularities to see if we should use the alternate button icon display.
        /// </summary>
        public bool UseAlternateButtonIconDisplay()
        {
            switch (currentPartyMenuScreen)
            {
                case(PartyMenuScreens.Main):
                    if (MainScreen.IsSwapping())
                        return true;
                    else
                        return false;
            }

            return false;
        }

        /// <summary>
        /// Returns the party attached to this party menu system.
        /// </summary>
        public Party GetCorrespondingParty()
        {
            return correspondingParty;
        }

        /// <summary>
        /// Returns whether or not the party menu is open.
        /// </summary>
        public bool IsPartyMenuOpen()
        {
            return partyMenuOpen;
        }

        /// <summary>
        /// Gets the button currently highlighted.
        /// </summary>
        public Button GetHighlightedButton()
        {
            return highlightedButton;
        }

        /// <summary>
        /// Gets the party menu button info displays.
        /// </summary>
        public List<ButtonInfoDisplayPrefabObject> GetPartyMenuButtonInfoDisplays()
        {
            return partyMenuButtonInfoDisplays;
        }

        /// <summary>
        /// Gets the popup button info displays.
        /// </summary>
        public List<ButtonInfoDisplayPrefabObject> GetPopupButtonInfoDisplays()
        {
            return popupButtonInfoDisplays;
        }

        #endregion Getters

        #region Special Functions
        /// <summary>
        /// Sets the button as our highlighted button directly.
        /// </summary>
        public void AssignHighlightedButtonDirectly(Button newHighlightedButton)
        {
            highlightedButton = newHighlightedButton;
        }

        #endregion Special Functions

    }
}
