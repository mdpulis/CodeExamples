using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SynodicArc.BookOfRegrets.Data;
using SynodicArc.BookOfRegrets.Localizing;

namespace SynodicArc.BookOfRegrets.Minigames.ControlRoom{
	public class ControlRoomMinigameRefs : MonoBehaviour {

		#region Constants
		public const int NUMBER_LENGTH = 4; //the length of the number we put into the control panel
		public const int NUMBER_TO_SET_OFF_ALARM = 3; //after 3 failed attempts, we set off the alarm
		public const float MULT_MOD = 0.75f; //multiplication mod for data display panel view
		//Inputs
		public const string CANCEL = "Cancel";
		public const string ESCAPE = "Escape";
		#endregion Constants

		public Text NumberText; //the text field where we store our number
		public Text DataDisplayText; //the text field where we put information about what's happening on the control panel
		public RectTransform DataDisplayParent; //the rect transform of the data display panel console
		public Image ControlPanelPowerStatus; //the light that is red if off, green if on
		public GameObject FirstToHighlightButton; //the first button we want to highlight in controller/keyboard modes
		public GameObject DataDisplayContent; //where the data display text will be kept
		public GameObject DataDisplayTextPrefab; //the prefab of the data display text object

		private string currentNumber = ""; //the number we are currently entering into the control panel
		private int currentNumberLength = 0; //how many numbers do we currently have in our string?
		private bool powerOn = false; //is the control panel powered on?
		private bool processing = false; //is the display processing currently? If so, we can't press buttons.
		[HideInInspector]
		public List<GameObject> DataDisplayObjects; //list of the strings we put in the data display panel

		//Audio Refs
		public List<AudioSource> SFX;

		//Localization Refs
		public Text ControlPanelDescriptionText;
		public Text ButtonExitText;
		public Text PowerOnButtonText;
		public Text PressButtonDescriptorText;
		public Text BackspaceButtonDescriptorText;
		public Text ExitButtonDescriptorText;
		private Font currentFont;

		//Icon + Button Refs
		public GameObject PressButtonInfoPanel;
		public Image PressButtonImage;
		public GameObject BackspaceButtonInfoPanel;
		public Image BackspaceButtonImage;
		public GameObject ExitButtonInfoPanel;
		public Image ExitButtonImage;


		#region SetUp
		void Awake(){
			//Ensure our display fields are empty on startup
			NumberText.text = "";
			DataDisplayText.text = "";
			//Ensure our number values are also at 0
			currentNumberLength = 0;
			//Set list to new
			DataDisplayObjects = new List<GameObject>();
		}

		/// Sets up basic references.
		public void SetUp(){
			//If on a controller/keyboard scheme, set our first to highlight button
			if (GameSettings.controlType == ControlTypes.Controller || GameSettings.controlType == ControlTypes.Keyboard) {
				EventSystem.current.SetSelectedGameObject (FirstToHighlightButton);
			}

			SetSoundVolumes ();
			SetLocalizedText ();
			ControllerIconSetUp ();
		}

		/// Sets the sound volumes.
		public void SetSoundVolumes(){
			foreach (AudioSource audio in SFX) {
				audio.volume = DataContainer.SFXVolume;
			}
		}

		/// Sets the localized text where needed.
		public void SetLocalizedText(){
            //Set our font based on current language
            currentFont = Localization.GetLocalizedFont();
			
			ControlPanelDescriptionText.text = Localization.UseTheControlPanelBelow;
			ControlPanelDescriptionText.font = currentFont;
			ButtonExitText.text = Localization.ExitCPButton;
            if(!Localization.IsRomanLanguage())
			    ButtonExitText.font = currentFont; //Only change font for non-Stencil usable objects
			PowerOnButtonText.text = Localization.PowerCPButton;
            if (!Localization.IsRomanLanguage())
                PowerOnButtonText.font = currentFont;
            PressButtonDescriptorText.text = Localization.PressButton;
			PressButtonDescriptorText.font = currentFont;
			BackspaceButtonDescriptorText.text = Localization.Backspace;
			BackspaceButtonDescriptorText.font = currentFont;
			ExitButtonDescriptorText.text = Localization.ExitMinigame;
			ExitButtonDescriptorText.font = currentFont;
            if (!Localization.IsRomanLanguage())
                DataDisplayText.font = currentFont;
		}

		/// Sets up controller icons and such based on control type
		private void ControllerIconSetUp(){
			switch (GameSettings.controlType) {
			//Turn off all button panels on Touch mode
			case(ControlTypes.TouchMode):
				ExitButtonInfoPanel.SetActive (false);
				PressButtonInfoPanel.SetActive (false);
				BackspaceButtonInfoPanel.SetActive (false);
				break;
			case(ControlTypes.Mouse):
				PressButtonInfoPanel.SetActive (false);
				ExitButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.KEYBOARD_PATH +
					ConstantData.KEY_ESCAPE);
				BackspaceButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.KEYBOARD_PATH +
					ConstantData.KEY_BACKSPACE);
				break;
			case(ControlTypes.Keyboard):
				PressButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.KEYBOARD_PATH +
					ConstantData.KEY_ENTER);
				BackspaceButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.KEYBOARD_PATH +
					ConstantData.KEY_BACKSPACE);
				ExitButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.KEYBOARD_PATH +
					ConstantData.KEY_ESCAPE);
				break;
			case(ControlTypes.Controller):
				switch (GameSettings.consoleType) {
				case(ConsoleTypes.PlayStation3):
				case(ConsoleTypes.PlayStation4):
					PressButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.PLAYSTATION_PATH +
						ConstantData.PS_CROSS);
					BackspaceButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.PLAYSTATION_PATH +
						ConstantData.PS_CIRCLE);
					ExitButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.PLAYSTATION_PATH +
						ConstantData.PS_SQUARE);
					break;
				case(ConsoleTypes.Xbox360):
				case(ConsoleTypes.XboxOne):
					PressButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.XBOX_PATH +
						ConstantData.XBOX_A);
					BackspaceButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.XBOX_PATH +
						ConstantData.XBOX_B);
					ExitButtonImage.sprite = Resources.Load<Sprite> (ConstantData.CONTROLLER_ICON_PATH + ConstantData.XBOX_PATH +
						ConstantData.XBOX_X);
					break;
				default:
					Debug.Log ("<color=red><b>Unsupported Console Type in ControlRoomMinigameRefs ControllerSetUp.</b></color>");
					break;
				}
				break;
			default:
				Debug.Log ("<color=red><b>Invalid control type when setting up controller icon references in ControlRoomMinigameRefs.</b></color>");
				break;
			}
		}
		#endregion SetUp


		#region Update
		void Update(){
			if (!processing) {
				//if we press escape, leave the control panel
				if (Input.GetButtonDown (ESCAPE)) {
					ExitControlPanel ();
				}

				if (powerOn) {
					//if we press cancel, backspace
					if (Input.GetButtonDown (CANCEL)) {
						Backspace ();
					}
				}
			}

		}

		#endregion Update


		#region Control Panel Button Press
		/// Presses the button with number.
		public void PressButtonWithNumber(string number){
			//If the power is on and we aren't processing data atm, we perform actions
			if (powerOn && !processing) {
				//Debug.Log ("Adding: " + number);
				AddNumberToDisplay (number);

				//If the value is now 4 numbers long, we check if the number is a correct value
				if (currentNumberLength == NUMBER_LENGTH) {
					CheckForMatch (); //Now that we have our full 4-digit number, check for a match!
				}
			}
		}

		/// Powers on (or off) the display 
		public void PowerButton(){
			//if power off, turn on
			if (!powerOn) {
				ControlPanelPowerStatus.color = new Color32 (0, 255, 0, 255); //turn light to green

				processing = true;
				powerOn = true; //power is now on
				AddDataToDisplay(Localization.NowPoweringOnControlPanel);
				StartCoroutine (PowerOn()); //powers on the console
				StartCoroutine (PowerOnStarDisplays ()); //slowly displays the stars for the number panel
			} else { //if powered on, turn off
				//Can't turn off while processing!
				if (!processing) {
					processing = true;
					AddDataToDisplay (Localization.NowPoweringOff);
					StartCoroutine (PowerOff ());
				}
			}
		}

		private IEnumerator PowerOn(){
			yield return new WaitForSeconds (2.0f);
			AddDataToDisplay (Localization.PowerUpComplete);
			processing = false; //done processing
		}

		private IEnumerator PowerOnStarDisplays(){
			yield return new WaitForSeconds (0.35f);
			NumberText.text = "*";
			yield return new WaitForSeconds (0.35f);
			NumberText.text = "* *";
			yield return new WaitForSeconds (0.35f);
			NumberText.text = "* * *";
			yield return new WaitForSeconds (0.35f);
			NumberText.text = "* * * *";
		}

		private IEnumerator PowerOff(){
			yield return new WaitForSeconds (1.7f);
			//reset all data
			NumberText.text = "";
			currentNumber = "";
			DataDisplayText.text = "";
			currentNumberLength = 0;
			foreach (GameObject dataDisplay in DataDisplayObjects) {
				Destroy (dataDisplay);
			}

			ControlPanelPowerStatus.color = new Color32(255, 0, 0, 255); //turn light to red
			powerOn = false; //power is now off
			processing = false; //no longer processing
		}

		/// Removes the most recently pressed number.
		public void Backspace(){
			//Make sure power is on and we aren't processing before backspacing
			if (powerOn && !processing) {
				if (string.IsNullOrEmpty (currentNumber)) {
					AddDataToDisplay (Localization.CannotRemoveFromEmptyNumber);
				} else {
					string rmvNum = "";

					//If we only have one number pressed, we don't remove the space
					if (currentNumberLength == 1) {
						rmvNum = currentNumber.Substring (0); //get the value of the last position
						currentNumber = currentNumber.Remove (0);
					} else {
						rmvNum = currentNumber.Substring (currentNumber.Length - 2); //get the value of the last position
						currentNumber = currentNumber.Remove (currentNumber.Length - 2);
					}
					currentNumberLength--; //subtract length of numbers by 1
					AddDataToDisplay (Localization.Removing + rmvNum);

					switch (currentNumberLength) {
					case(0):
						NumberText.text = currentNumber + "* * * *";
						break;
					case(1):
						NumberText.text = currentNumber + "* * *";
						break;
					case(2):
						NumberText.text = currentNumber + "* *";
						break;
					case(3):
						NumberText.text = currentNumber + "*";
						break;
					}
				}
			}
		}

		/// Exits the control panel.
		public void ExitControlPanel(){
			//As long as we're not processing data, we can leave the control panel
			if (!processing) {
				EventManager.RaiseControlRoomCompleteNumberSent (0); //-> ControlRoomMinigame.ResolveMinigame(0) for fail
			}
		}
		#endregion Control Panel Button Press


		#region Data Display Panel
		/// Adds the number to the display.
		public void AddNumberToDisplay(string number){
			//NumberText.text = string.Format ("* * * *");

			switch (currentNumberLength) {
			case(0):
				currentNumber = number + " ";
				NumberText.text = currentNumber + "* * *";
				AddDataToDisplay (Localization.Adding + number);
				currentNumberLength++;
				break;
			case(1):
				currentNumber += number + " ";
				NumberText.text = currentNumber + "* *";
				AddDataToDisplay (Localization.Adding + number);
				currentNumberLength++;
				break;
			case(2):
				currentNumber += number + " ";
				NumberText.text = currentNumber + "*";
				AddDataToDisplay (Localization.Adding + number);
				currentNumberLength++;
				break;
			case(3):
				currentNumber += number;
				NumberText.text = currentNumber;
				AddDataToDisplay (Localization.Adding + number);
				currentNumberLength++;
				break;
			case(4):
				AddDataToDisplay (Localization.ErrorCannotAddNumber);
				Debug.Log ("<color=red>Invalid number length; currentNumber: " + currentNumber + ", length: " + currentNumberLength + "</color>");
				break;
			default:
				Debug.Log ("<color=red>Invalid number length; currentNumber: " + currentNumber + ", length: " + currentNumberLength + "</color>");
				break;

			}

		}

		/// Adds the data to display.
		public void AddDataToDisplay(string data){
			GameObject dataDisplayText = Instantiate (DataDisplayTextPrefab);
			dataDisplayText.transform.SetParent (DataDisplayContent.transform, false);
			dataDisplayText.GetComponent<Text>().text = Localization.GetFormattedText(data, 12);
            if(!Localization.IsRomanLanguage())
                dataDisplayText.GetComponent<Text>().font = Localization.GetLocalizedFont();
			DataDisplayObjects.Add (dataDisplayText);
			//Debug.Log ("Parent height: " + DataDisplayParent.rect.height);
			float totalDataDisplayHeight = GetTotalDataDisplayTextObjectsHeight ();
			while (totalDataDisplayHeight > DataDisplayParent.rect.height * MULT_MOD) {
				Destroy (DataDisplayObjects [0]);
				DataDisplayObjects.RemoveAt (0);
				totalDataDisplayHeight = GetTotalDataDisplayTextObjectsHeight ();
			}
		}

		private float GetTotalDataDisplayTextObjectsHeight(){
			float height = 0.0f;
			foreach (GameObject dataDisplayObj in DataDisplayObjects) {
				height += dataDisplayObj.GetComponent<RectTransform> ().rect.height;
			}
			return height;
		}
		#endregion Data Display Panel


		#region Finish Functions
		/// Checks to see if the entered number matches one of the correct values
		private void CheckForMatch(){
			AddDataToDisplay (Localization.Processing);
			processing = true;
			StartCoroutine (DelayedDisplay ());
		}

		/// Displays the data after a short delay
		private IEnumerator DelayedDisplay(){
			yield return new WaitForSeconds (1.3f);

			switch (currentNumber) {
			case("3 2 1 9"):
				if (!DataContainer.DataVariablesDictionary [DataVariables.panelThreeTwoOneNine]) {
					AddDataToDisplay (Localization.NowOpeningDoorToFurnace);
					DataContainer.DataVariablesDictionary [DataVariables.panelThreeTwoOneNine] = true; //set 3219 used to true
					EventManager.RaiseControlRoomCompleteNumberSent(3219);
				} else {
					AddDataToDisplay (Localization.FurnaceDoorHasAlreadyBeenOpened);
					ResetControlPanelValues (); //Reset control panel values
				}
				break;
			case("1 3 3 7"):
				if (!DataContainer.DataVariablesDictionary [DataVariables.panelOneThreeThreeSeven]) {
					AddDataToDisplay (Localization.NowReleasingHiddenOrb);
					DataContainer.DataVariablesDictionary [DataVariables.panelOneThreeThreeSeven] = true; //set 1337 used to true
					EventManager.RaiseControlRoomCompleteNumberSent(1337);
				} else {
					AddDataToDisplay (Localization.OrbHasAlreadyBeenRemoved);
					ResetControlPanelValues (); //Reset control panel values
				}
				break;
			case("6 9 6 9"):
				if (!DataContainer.DataVariablesDictionary [DataVariables.panelSixNineSixNine]) {
					AddDataToDisplay (Localization.NowReleasingHiddenCache);
					DataContainer.DataVariablesDictionary [DataVariables.panelSixNineSixNine] = true; //set 6969 used to true
					EventManager.RaiseControlRoomCompleteNumberSent(6969);
				} else {
					AddDataToDisplay (Localization.HiddenCacheHasAlreadyBeenReleased);
					ResetControlPanelValues (); //Reset control panel values
				}
				break;
			case("8 2 5 1"):
				AddDataToDisplay (Localization.OpeningLogs);
				StartCoroutine (DisplaySpecialLog()); //Display the special story logs
				break;
			default:
				AddDataToDisplay (Localization.IncorrectInput);
				DataContainer.DataVariableIntegersDictionary [DataVariableIntegers.panelInputAttempts]++;
				//If we reached our third error
				if (DataContainer.DataVariableIntegersDictionary [DataVariableIntegers.panelInputAttempts] == NUMBER_TO_SET_OFF_ALARM) {
					AddDataToDisplay (Localization.NumberOfIncorrectInputsHasExceededAllowedLimit);
					EventManager.RaiseControlRoomCompleteNumberSent(3); //3 sends to three fails
				} else {
					ResetControlPanelValues ();
				}
				break;
			}
		}

		/// Resets the control panel values so we can perform another input
		private void ResetControlPanelValues(){
			//reset all data
			NumberText.text = "* * * *";
			currentNumber = "";
			currentNumberLength = 0;
			//Set processing back to false
			processing = false;
		}

		#endregion Finish Functions


		#region Special Log
		/// Displays special story information.
		private IEnumerator DisplaySpecialLog(){
			yield return new WaitForSeconds (2.5f);
			//Clear out the data display objects
			foreach (GameObject dataDisplayObj in DataDisplayObjects) {
				Destroy (dataDisplayObj);
			}
			DataDisplayObjects.Clear ();
			AddDataToDisplay (Localization.ControlRoomHiddenMessage1);
			yield return new WaitForSeconds (8.0f);
			Destroy (DataDisplayObjects [0]);
			DataDisplayObjects.Clear ();
			AddDataToDisplay (Localization.ControlRoomHiddenMessage2);
			yield return new WaitForSeconds (9.0f);
			Destroy (DataDisplayObjects [0]);
			DataDisplayObjects.Clear ();
			AddDataToDisplay (Localization.ControlRoomHiddenMessage3);
			yield return new WaitForSeconds (10.0f);
			Destroy (DataDisplayObjects [0]);
			DataDisplayObjects.Clear ();
			AddDataToDisplay (Localization.ControlRoomHiddenMessage4);
			yield return new WaitForSeconds (9.0f);
			Destroy (DataDisplayObjects [0]);
			DataDisplayObjects.Clear ();
			ResetControlPanelValues ();
			AddDataToDisplay (Localization.ClosingLogs);
		}
		#endregion Special Log
	}
}