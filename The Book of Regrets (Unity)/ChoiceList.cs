using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SynodicArc.BookOfRegrets.PrefabObjects;
using SynodicArc.BookOfRegrets.Data;
using SynodicArc.BookOfRegrets.Localizing;

namespace SynodicArc.VisualNovel.Dialogues{
	//The entirety of a choice select case
	public class ChoiceList : MonoBehaviour {

		public bool timedChoice;
		public float choiceTime;
		public List<Choice> choiceList;
		[HideInInspector]
		public List<GameObject> visibleChoiceList;
		public string comments;

		//Need this to reference the instance of the class so we can use a coroutine when on button mode
		private static ChoiceList thisChoiceList;

		//No need to have more than one instance of the GameObject's location
		private static GameObject choiceLayoutGroup;
		private static ChoiceTimer choiceTimer;
		private static AudioSource buttonPressedAudio; //for the random choice auto selection
		private static Animator choiceLayoutGroupAnimator;
		private static GameObject dialogueContainer;
		private static SettingsHandler settingsHandler;
		private static EventSystem eventSystem;
		private static Font currentFont;

		#region Constants
		//Prefab strings
		private const string SINGLE_CHOICE_PANEL_PATH = "Prefabs/UIPrefabs/SingleChoicePanel";
		private const string DIALOGUE_BLOCK_PATH = "Prefabs/UIPrefabs/DialogueBlock";

		//Animator strings
		private const string CHOICES_VISIBLE = "ChoicesVisible";
		//Game object strings
		private const string BUTTON_SFX_HELPER = "UIHelpers";
		#endregion

		#region Set Up
		void OnEnable(){
			EventManager.onSendLastSelectedChoice += this.SetChoiceSelectedViaEvent; //AnimatorFunctions.Close__()->
			EventManager.onLanguageChanged += this.ReLocalizeChoicesWhileActive; //Localization.Localize()->
			EventManager.onChoiceCountdownTimedOut += this.RandomlySelectChoice; //ChoiceTimer.ChoiceTimerCountdown()->
		}

		void OnDisable(){
			EventManager.onSendLastSelectedChoice -= this.SetChoiceSelectedViaEvent;
			EventManager.onLanguageChanged -= this.ReLocalizeChoicesWhileActive;
			EventManager.onChoiceCountdownTimedOut -= this.RandomlySelectChoice;
		}

		/// Gets the references we need without Awake()
		void GetReferences(){
			//Set up lists
			visibleChoiceList = new List<GameObject> ();
			
			//Find our panels where our choices and other things will be instantiated
			if (choiceLayoutGroup == null) {
				choiceLayoutGroup = GameObject.Find ("ChoiceLayoutGroup");
				choiceLayoutGroupAnimator = choiceLayoutGroup.GetComponent<Animator>();
			}
			if (choiceTimer == null) {
				choiceTimer = GameObject.Find ("ChoiceTimerPanel").GetComponent<ChoiceTimer>();
			}
			if (buttonPressedAudio == null) {
				buttonPressedAudio = GameObject.Find ("ButtonPressedAudio").GetComponent<AudioSource>();
			}
			if (dialogueContainer == null) {
				dialogueContainer = GameObject.Find ("DialogueContainer");
			}
			if (settingsHandler == null) {
				settingsHandler = GameObject.Find ("SettingsHandler").GetComponent<SettingsHandler>();
			}
			if (eventSystem == null) {
				eventSystem = GameObject.Find ("EventSystem").GetComponent<EventSystem>();
			}

            //Set our font based on current language
            currentFont = Localization.GetLocalizedFont();
		}
		#endregion Set Up

		#region Functions
		//Sort choices in list by priority
		public void SortChoiceList(){
			choiceList = choiceList.OrderBy (choice => choice.priority).ToList ();
		}

		//Displays all choices in list based on if their conditions pass or not
		public void DisplayChoices(){
			thisChoiceList = Instantiate (this.gameObject).GetComponent<ChoiceList>();
			thisChoiceList.GetReferences ();
			GameState.selectingChoice = true;

			//Similar to awake--gets our references
			GetReferences ();
			//Sort choices based on priority levels
			SortChoiceList ();

			//Make choices visible
			for (int i = 0; i < choiceList.Count; i++) {
				if(choiceList[i].ChoiceVisible()){
					MakeChoiceVisibleOnScreen(choiceList[i]);
				}
			}

			//If controller/keyboard mode, set first in list as selected
			if (GameSettings.controlType == ControlTypes.Controller || GameSettings.controlType == ControlTypes.Keyboard) {
				thisChoiceList.StartCoroutine(SetChoiceSelected(visibleChoiceList [0]));
			}

			//Start animation
			choiceLayoutGroupAnimator.SetBool (CHOICES_VISIBLE, true);

			//If a countdown version, turn on the countdown timer
			if(timedChoice)
				choiceTimer.StartChoiceTimerCountdown(choiceTime);
		}

		//Instantiates choice and makes it visible on screen
		public void MakeChoiceVisibleOnScreen(Choice currentChoice){
			if (choiceLayoutGroup == null)
				choiceLayoutGroup = GameObject.Find ("ChoiceLayoutGroup");
			GameObject choiceInList = (GameObject) Instantiate (Resources.Load (SINGLE_CHOICE_PANEL_PATH));

			#region Event Triggers/SFX
			EventTrigger choiceEventTrigger = choiceInList.GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerEnter;
			entry.callback.AddListener( (eventData) => { GameObject go = GameObject.Find(BUTTON_SFX_HELPER);
				go.GetComponent<ButtonSoundEffects>().SelectObject();} );
			EventTrigger.Entry entry2 = new EventTrigger.Entry();
			entry2.eventID = EventTriggerType.PointerClick;
			entry2.callback.AddListener( (eventData) => { GameObject go = GameObject.Find(BUTTON_SFX_HELPER);
				go.GetComponent<ButtonSoundEffects>().PressObject();} );
			EventTrigger.Entry entry3 = new EventTrigger.Entry();
			entry3.eventID = EventTriggerType.Select;
			entry3.callback.AddListener( (eventData) => { GameObject go = GameObject.Find(BUTTON_SFX_HELPER);
				go.GetComponent<ButtonSoundEffects>().SelectObject();} );
			EventTrigger.Entry entry4 = new EventTrigger.Entry();
			entry4.eventID = EventTriggerType.Submit;
			entry4.callback.AddListener( (eventData) => { GameObject go = GameObject.Find(BUTTON_SFX_HELPER);
				go.GetComponent<ButtonSoundEffects>().PressObject();} );

			choiceEventTrigger.triggers.Add(entry);
			choiceEventTrigger.triggers.Add(entry2);
			choiceEventTrigger.triggers.Add(entry3);
			choiceEventTrigger.triggers.Add(entry4);
			#endregion Event Triggers/SFX

			//Places choice in correct spot in the list, under parent choiceLayoutGroup, then add to GameObject list
			choiceInList.transform.SetParent(choiceLayoutGroup.transform, false);
			visibleChoiceList.Add (choiceInList); 
			thisChoiceList.visibleChoiceList.Add (choiceInList);


			SingleChoiceObject sco = choiceInList.GetComponent<SingleChoiceObject> ();
			
			//Call another method to return localized text
			string choiceText = GetLocalizedText (currentChoice);
			//Assigns choice name to text
			sco.SingleChoiceText.text = choiceText;
			sco.SingleChoiceText.font = currentFont;
			//Assigns the choice itself to the object
			sco.CorrespondingChoice = currentChoice;

			//Assigns onClickListener
			choiceInList.GetComponent<Button>().onClick.AddListener(() => {
				if(GameState.state == GameState.States.ItemGetScreen){
					GameState.state = GameState.States.Normal; //Set back to normal after clicking our choice so we can progress with inputs
				}
				GoToNewDialogueBlock(currentChoice);
				if(currentChoice.additionalCase != AdditionalCase.None)
					PerformChoiceAdditionalCase(currentChoice);
				if(currentChoice.dimlyLitHallwayCase != DimlyLitHallwayCase.None)
					PerformChoiceDimlyLitHallwayCase(currentChoice);
				if(currentChoice.achievementCase != AchievementCase.None)
					PerformAchievementCase(currentChoice);
				ClearVisibleChoices();
				if(GameState.state != GameState.States.ItemGetScreen){
					GameState.state = GameState.States.Normal; //Set back to normal after clicking our choice so we can progress with inputs
				}
				thisChoiceList.StartCoroutine(DestroyChoiceListAndContinue()); //Destroy and handle controller inputs
			});
			
		}

		/// Sets the choice as the selected game object after waiting for a short period of time
		public IEnumerator SetChoiceSelected(GameObject selectedChoice){
			yield return new WaitForSeconds (0.5f);
			eventSystem.SetSelectedGameObject (selectedChoice);
		}

		/// Destroys the current choice list and sets the Game Choice State back to not selecting choice
		public IEnumerator DestroyChoiceListAndContinue(){
			yield return new WaitForEndOfFrame();
			choiceTimer.TurnOffTimerCountdown(); //ends the choice timer if it was on
			Destroy(thisChoiceList.gameObject);
			GameState.selectingChoice = false;
		}

		/// Sets the choice selected via event, setting to the last position we were at before entering a menu
		/// AnimatorFunctions.Close__()->here
		public void SetChoiceSelectedViaEvent(GameObject lastSelectedChoice){
			//If we didn't match anything, set to first object
			eventSystem.SetSelectedGameObject (lastSelectedChoice);
			//If our last selected choice was nothing (e.g. mouse click) or not one of the eligible game objects, set to top choice
			if((lastSelectedChoice == null) || (lastSelectedChoice.transform.parent != choiceLayoutGroup.transform))
				eventSystem.SetSelectedGameObject (thisChoiceList.visibleChoiceList[0]);
		}


		/// Performs the choice's additional case if it exists.
		public void PerformChoiceAdditionalCase(Choice currentChoice){
			switch (currentChoice.additionalCase) {
			case(AdditionalCase.AreaAlteration):
				//
				break;
			case(AdditionalCase.BossDefeat):
				goto case(AdditionalCase.ChangeParam);
			case(AdditionalCase.ChangeParam):
				DataContainer.UpdateDataVariable(currentChoice.editableParam, currentChoice.newParamCond);
				if(currentChoice.additionalCase == AdditionalCase.BossDefeat)
					EventManager.RaiseBossDefeated();
				break;
			case(AdditionalCase.ChangeParamInt):
				DataContainer.UpdateDataVariable(currentChoice.editableParamInt, currentChoice.amount,
				                                 currentChoice.changeParamIntIncrementType);
				break;
			case(AdditionalCase.CharacterAnim):
				EventManager.RaiseAnimateCharacter(currentChoice.character, currentChoice.characterAnim); //->AnimatorFunctions.AnimateCharacter()
				break;
			case(AdditionalCase.ItemRemove):
				EventManager.RaiseChoiceWithItemRemoveClicked(currentChoice.removeItem); //->SettingsHandler.RemoveItemFromInventory()
				break;
			case(AdditionalCase.ScreenEffect):
				EventManager.RaiseScreenEffect(currentChoice.screenEffectType);
				break;
			case(AdditionalCase.ChangeSpecialParam):
				DataContainer.UpdateSpecialDataVariable (currentChoice.editableSpecialParam, currentChoice.newParamCond);
				break;
			case(AdditionalCase.ChangePermanentParam):
				DataContainer.UpdatePermanentVariable (currentChoice.editablePermanentParam, currentChoice.newParamCond);
				switch (currentChoice.editablePermanentParam) { //Update the menu state of journals
				case(PermanentVariables.PermJournalSetA):
				case(PermanentVariables.PermJournalSetB):
				case(PermanentVariables.PermJournalSetC):
				case(PermanentVariables.PermJournalSetD):
				case(PermanentVariables.PermJournalSetE):
				case(PermanentVariables.PermJournalSetF):
					settingsHandler.DisplayAndLocalizeJournalEntries ();
					break;
				}
				break;
			case(AdditionalCase.EndBackgroundSFX):
				EventManager.RaiseEndBackgroundSfx (); //->TextProgressor.EndBackgroundSfx()
				break;
			default:
				Debug.Log("<color=red><b>Invalid Additional Case selected.</b></color>");
				break;
			}
		}

		/// Performs the special Dimly Lit Hallway case.
		private void PerformChoiceDimlyLitHallwayCase(Choice currentChoice){
			if (currentChoice.dimlyLitHallwayCase != DimlyLitHallwayCase.None) {
				//Turn on the dimly lit hallway movement functioning so we can't continue Text Progressor yet
				GameState.dimlyLitHallwayFunctioning = true;

				switch(currentChoice.dimlyLitHallwayCase){
				case(DimlyLitHallwayCase.PlayerNorth):
					EventManager.RaiseUpdateLocation (0, 1);
					break;
				case(DimlyLitHallwayCase.PlayerEast):
					EventManager.RaiseUpdateLocation (1, 0);
					break;
				case(DimlyLitHallwayCase.PlayerSouth):
					EventManager.RaiseUpdateLocation (0, -1);
					break;
				case(DimlyLitHallwayCase.PlayerWest):
					EventManager.RaiseUpdateLocation (-1, 0);
					break;
				case(DimlyLitHallwayCase.PlayerHide):
					DimlyLitHallwayMap.ChangeHidingStatus (); //Change hidden status
					EventManager.RaiseUpdateLocation (0, 0);
					break;
				case(DimlyLitHallwayCase.PlayerExitHide):
					DimlyLitHallwayMap.ChangeHidingStatus (); //Change hidden status
					GameState.dimlyLitHallwayFunctioning = false;
					break;
				case(DimlyLitHallwayCase.PlayerRemainHidden):
					EventManager.RaiseUpdateLocation (0, 0);
					break;
				default:
					Debug.Log("<color=red><b>Invalid DimlyLitHallway Case selected.</b></color>");
					break;
				}
			}
		}


		/// Performs the choice's additional case if it exists.
		public void PerformAchievementCase(Choice currentChoice){
			switch (currentChoice.achievementCase) {
			case(AchievementCase.CheckForUnlock):
				//Checks to see if we can unlock the achievement
				SynodicArc.BookOfRegrets.BORAchievements.AchievementUnlock.CheckForUnlock (currentChoice.checkForAchievement);
				break;
			default:
				Debug.Log("<color=red><b>Invalid Achievement Case selected.</b></color>");
				break;
			}
		}

		///Sets all data to the new dialogue block we are instantiating, and removes the old one
		private void GoToNewDialogueBlock(Choice currentChoice){
			GameObject newDialogueBlock = (GameObject) Instantiate (currentChoice.goToDialogueBlock);
			newDialogueBlock.transform.SetParent (dialogueContainer.transform, false);

			EventManager.RaiseChoiceClicked (); //->TextProgressor.Continue();
		}

		//Clear out all the currently visible choices
		public void ClearVisibleChoices(){
			choiceLayoutGroupAnimator.SetBool (CHOICES_VISIBLE, false);

			foreach (GameObject choice in visibleChoiceList) {
				Destroy(choice);
			}
			visibleChoiceList.Clear ();
		}

		/// If we didn't choose a choice in time, the game does it for us.
		private void RandomlySelectChoice(){
			int randomChoice = UnityEngine.Random.Range (0, visibleChoiceList.Count);
			visibleChoiceList [randomChoice].GetComponent<Button> ().onClick.Invoke ();
			buttonPressedAudio.Play (); //play our pressed audio
		}
		#endregion Functions

		#region Localization
		//Gets the localized text of the choice
		private string GetLocalizedText(Choice currentChoice){
			string localizedText = "";

			switch (GameSettings.language) {
			case(Languages.English):
				localizedText = currentChoice.choiceTextEN;
				break;
			case(Languages.French):
				localizedText = currentChoice.choiceTextFR;
				break;
			case(Languages.Spanish):
				localizedText = currentChoice.choiceTextSP;
				break;
			case(Languages.German):
				localizedText = currentChoice.choiceTextGR;
				break;
			case(Languages.Italian):
				localizedText = currentChoice.choiceTextIT;
				break;
			case(Languages.Czech):
				localizedText = currentChoice.choiceTextCZ;
				break;
			case(Languages.Russian):
				localizedText = currentChoice.choiceTextRS;
				break;
			case(Languages.Japanese):
				localizedText = currentChoice.choiceTextJP;
				break;
			case(Languages.Chinese):
				localizedText = currentChoice.choiceTextCN;
				break;
			case(Languages.ChineseTraditional):
				localizedText = currentChoice.choiceTextCN_T;
				break;
			case(Languages.Korean):
				localizedText = currentChoice.choiceTextKR;
				break;
			default:
				localizedText = currentChoice.choiceTextEN;
				Debug.Log("<color=red><b>Improper language selected. Setting choice to defaults.</b></color>");
				break;
			}

			localizedText = EnsureFieldsAreFilled (currentChoice, localizedText);

			return localizedText.Trim();
		}

		/// Ensures the fields are filled with some sort of data and sends it back.
		public string EnsureFieldsAreFilled(Choice currentChoice, string localizedText){
			//If the text is empty, we set it to English.
			if (string.IsNullOrEmpty (localizedText)) {
				Debug.Log ("<color=red><b>Empty choice text at </b></color>" + gameObject.name);
				localizedText = currentChoice.choiceTextEN;
			}

			return localizedText;
		}

		/// Relocalizes the choices if they are active.
		/// Localization.Localize()->here
		private void ReLocalizeChoicesWhileActive(){
            currentFont = Localization.GetLocalizedFont();
			
			if (visibleChoiceList.Count != 0) {
				foreach (GameObject gmobj in visibleChoiceList) {
					SingleChoiceObject sco = gmobj.GetComponent<SingleChoiceObject> ();
					sco.SingleChoiceText.text = GetLocalizedText (sco.CorrespondingChoice); //assigns the corresponding choice's localization
					sco.SingleChoiceText.font = currentFont;
				}
			}
		}
			
		#endregion Localization
	}
}