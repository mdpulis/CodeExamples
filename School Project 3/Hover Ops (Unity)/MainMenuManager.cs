using EAE.Race.Player;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EAE.Race.MainMenu
{
    /// <summary>
    /// The different main menu screen states
    /// </summary>
    public enum MainMenuStates
    {
        MainMenu = 0,

        SelectingLevel = 10,

        SelectingCustomize = 20,
        SelectingCharacter = 21,
        SelectingHoverboard = 22,

        Options = 30,
    }


    /// <summary>
    /// Manages the main menu
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        //Selecting Level
        public SelectableLevelPrefabObject SelectableLevelPrefabObject_PO;
        public Transform LevelList;
        public List<LevelInfo> SelectableLevelInfos;

        //Selecting Character
        public SelectableCharacterPrefabObject SelectableCharacterPrefabObject_PO;
        public Transform CharacterList;
        public Transform CharacterDisplayLocation;
        public List<CharacterInfo> SelectableCharacterInfos;
        private GameObject currentlyDisplayingCharacterModel;

        //Selecting Hoverboard
        public SelectableHoverboardPrefabObject SelectableHoverboardPrefabObject_PO;
        public Transform HoverboardList;
        public Transform HoverboardDisplayLocation;
        public List<HoverboardInfo> SelectableHoverboardInfos;
        private GameObject currentlyDisplayingHoverboardModel;

        //Options
        public Toggle GyroToggle;

        //Ads
        public GameObject AdHolder;
        private AdVideoPlayer adVideoPlayer;

        //Private variables for management
        private MainMenuStates mainMenuState = MainMenuStates.MainMenu;


        //Private variables for references
        private Animator mainMenuAnimator;
        private PlayerSettings playerSettings;

        private const string MAIN = "Main";
        private const string SELECTING_LEVEL = "Selecting Level";
        private const string SELECTING_CUSTOMIZATION = "Selecting Customization";
        private const string SELECTING_CHARACTER = "Selecting Character";
        private const string SELECTING_HOVERBOARD = "Selecting Hoverboard";
        private const string OPTIONS = "Options";


        private void Awake()
        {
            mainMenuAnimator = this.GetComponent<Animator>();
            playerSettings = GameObject.FindObjectsOfType<PlayerSettings>().Where(x => x.IsInitialized()).FirstOrDefault();
            adVideoPlayer = GameObject.FindObjectOfType<AdVideoPlayer>();

            OpenMainMenuScreen();

            InitializeLevels();
            InitializeCharacters();
            InitializeHoverboards();
            InitializePlayerSettings();
            ShowAd();
        }

        /// <summary>
        /// Initializes all of the levels for the select level screen
        /// </summary>
        private void InitializeLevels()
        {
            if (SelectableLevelInfos == null || SelectableLevelInfos.Count < 1)
                return;
         
            foreach(LevelInfo lo in SelectableLevelInfos)
            {
                if (lo == null)
                    continue;

                SelectableLevelPrefabObject slpo = Instantiate(SelectableLevelPrefabObject_PO).GetComponent<SelectableLevelPrefabObject>();
                slpo.Initialize(lo.GetLocalizedLevelName(), lo.SceneName, lo.LevelSprite, lo.DefaultBestTime);
                slpo.transform.SetParent(LevelList, false);
            }
        }

        /// <summary>
        /// Initializes all of the characters for the select character screen
        /// </summary>
        private void InitializeCharacters()
        {
            if (SelectableCharacterInfos == null || SelectableCharacterInfos.Count < 1)
                return;

            foreach (CharacterInfo co in SelectableCharacterInfos)
            {
                if (co == null)
                    continue;

                SelectableCharacterPrefabObject scpo = Instantiate(SelectableCharacterPrefabObject_PO).GetComponent<SelectableCharacterPrefabObject>();
                scpo.Initialize(co.GetLocalizedCharacterName(), co.CharacterSprite, co.LockedByDefault, co.ElonCoinCost, co.DollarCost, co.CharacterModel);
                scpo.transform.SetParent(CharacterList, false);

                scpo.SelectCharacterButton.onClick.AddListener(() =>
                {
                    if (scpo.IsUnlocked())
                    {
                        DisplayCharacterModel(co.CharacterModel);
                        if (playerSettings != null)
                            playerSettings.SetSelectedCharacter(co.CharacterModel);
                        else
                            Debug.Log("No player settings.");
                    }

                }); //displays the character model on click too


                //right now this just selects the first character in the list by default
                if (currentlyDisplayingCharacterModel == null)
                {
                    scpo.SelectCharacterButton.onClick.Invoke();
                }
            }
        }

        /// <summary>
        /// Initializes all of the hoverboards for the select hoverboard screen
        /// </summary>
        private void InitializeHoverboards()
        {
            if (SelectableHoverboardInfos == null || SelectableHoverboardInfos.Count < 1)
                return;

            foreach (HoverboardInfo hi in SelectableHoverboardInfos)
            {
                if (hi == null)
                    continue;

                SelectableHoverboardPrefabObject shpo = Instantiate(SelectableHoverboardPrefabObject_PO).GetComponent<SelectableHoverboardPrefabObject>();
                shpo.Initialize(hi.GetLocalizedHoverboardName(), hi.HoverboardSprite, hi.LockedByDefault, hi.ElonCoinCost, hi.DollarCost, hi.HoverboardModel);
                shpo.transform.SetParent(HoverboardList, false);

                shpo.SelectHoverboardButton.onClick.AddListener(() =>
                {
                    if (shpo.IsUnlocked())
                    {
                        DisplayHoverboardModel(hi.HoverboardModel);
                        playerSettings.SetSelectedHoverboard(hi.HoverboardModel);
                    }
                }); //displays the hoverboard model on click too


                //right now this just selects the first hoverboard in the list by default
                if (currentlyDisplayingHoverboardModel == null)
                {
                    shpo.SelectHoverboardButton.onClick.Invoke();
                }
            }
        }

        /// <summary>
        /// Sets the player settings in the main menu
        /// </summary>
        private void InitializePlayerSettings()
        {
            GyroToggle.isOn = playerSettings.GetGyroControls();
        }

        /// <summary>
        /// Shows the best ad ;)
        /// </summary>
        private void ShowAd()
        {
            StartCoroutine(ShowAdCR());
        }

        private IEnumerator ShowAdCR()
        {
            yield return new WaitForSeconds(7.0f);
            AdHolder.SetActive(true);
        }

        /// <summary>
        /// Shows the video ad
        /// </summary>
        public void ShowVideoAd()
        {
            if (adVideoPlayer != null)
                adVideoPlayer.PlayVideo();
        }

        /// <summary>
        /// Displays a character model and gets rid of the previous one
        /// </summary>
        public void DisplayCharacterModel(GameObject newCharModel)
        {
            if (currentlyDisplayingCharacterModel != null)
                Destroy(currentlyDisplayingCharacterModel);

            GameObject ncm = Instantiate(newCharModel);
            ncm.transform.SetParent(CharacterDisplayLocation, false);

            currentlyDisplayingCharacterModel = ncm;
        }

        /// <summary>
        /// Displays a hoverboard model and gets rid of the previous one
        /// </summary>
        public void DisplayHoverboardModel(GameObject newHoverboardModel)
        {
            if (currentlyDisplayingHoverboardModel != null)
                Destroy(currentlyDisplayingHoverboardModel);

            GameObject nhm = Instantiate(newHoverboardModel);
            nhm.transform.SetParent(HoverboardDisplayLocation, false);

            currentlyDisplayingHoverboardModel = nhm;
        }

        #region Options
        /// <summary>
        /// Changes the gyro controls
        /// </summary>
        public void ChangeGyroControls(bool onOff)
        {
            playerSettings.SetGyroControls(onOff);
        }

        #endregion Options

        #region Screens
        public void OpenMainMenuScreen()
        {
            CloseScreen();
            mainMenuState = MainMenuStates.MainMenu;
            mainMenuAnimator.SetBool(MAIN, true);
        }

        public void OpenSelectingLevelScreen()
        {
            CloseScreen();
            mainMenuState = MainMenuStates.SelectingLevel;
            mainMenuAnimator.SetBool(SELECTING_LEVEL, true);
        }

        public void OpenSelectingCustomizationScreen()
        {
            CloseScreen();
            mainMenuState = MainMenuStates.SelectingCustomize;
            mainMenuAnimator.SetBool(SELECTING_CUSTOMIZATION, true);
        }

        public void OpenSelectingCharacterScreen()
        {
            CloseScreen();
            mainMenuState = MainMenuStates.SelectingCharacter;
            mainMenuAnimator.SetBool(SELECTING_CHARACTER, true);
        }

        public void OpenSelectingHoverboardScreen()
        {
            CloseScreen();
            mainMenuState = MainMenuStates.SelectingHoverboard;
            mainMenuAnimator.SetBool(SELECTING_HOVERBOARD, true);
        }

        public void OpenOptionsScreen()
        {
            CloseScreen();
            mainMenuState = MainMenuStates.Options;
            mainMenuAnimator.SetBool(OPTIONS, true);
        }
        /// <summary>
        /// Goes back one screen
        /// </summary>
        public void GoBack()
        {
            switch(mainMenuState)
            {
                case (MainMenuStates.MainMenu):
                    Debug.LogWarning("Cannot go back from the base main menu screen.");
                    break;
                case (MainMenuStates.SelectingLevel):
                    OpenMainMenuScreen();
                    break;
                case (MainMenuStates.SelectingCustomize):
                    OpenMainMenuScreen();
                    break;
                case (MainMenuStates.SelectingCharacter):
                    OpenSelectingCustomizationScreen();
                    break;
                case (MainMenuStates.SelectingHoverboard):
                    OpenSelectingCustomizationScreen();
                    break;
                case (MainMenuStates.Options):
                    OpenMainMenuScreen();
                    break;
                default:
                    Debug.LogWarning("Unknown main menu state. Cannot go back.");
                    break;
            }
        }

        /// <summary>
        /// Closes the current screen
        /// </summary>
        public void CloseScreen()
        {
            switch (mainMenuState)
            {
                case (MainMenuStates.MainMenu):
                    mainMenuAnimator.SetBool(MAIN, false);
                    break;
                case (MainMenuStates.SelectingLevel):
                    mainMenuAnimator.SetBool(SELECTING_LEVEL, false);
                    break;
                case (MainMenuStates.SelectingCustomize):
                    mainMenuAnimator.SetBool(SELECTING_CUSTOMIZATION, false);
                    break;
                case (MainMenuStates.SelectingCharacter):
                    mainMenuAnimator.SetBool(SELECTING_CHARACTER, false);
                    break;
                case (MainMenuStates.SelectingHoverboard):
                    mainMenuAnimator.SetBool(SELECTING_HOVERBOARD, false);
                    break;
                case (MainMenuStates.Options):
                    mainMenuAnimator.SetBool(OPTIONS, false);
                    break;
                default:
                    Debug.LogWarning("Unknown main menu state. Cannot close.");
                    break;
            }
        }

        #endregion Screens

    }
}