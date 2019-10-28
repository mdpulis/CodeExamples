using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SynodicArc.Champions.Inputs.Menu;
using UnityEngine.EventSystems;
using SynodicArc.Champions.PartyMenu.Screens.Inventory;
using System.Linq;

namespace SynodicArc.Champions.PartyMenu.Screens
{
    /// <summary>
    /// Represents the base of/and functions for each party menu screen.
    /// </summary>
    public abstract class BasePartyMenuScreen : MonoBehaviour {
    
        //---Public variables for assigning data---//
        public PartyMenuScreens PartyMenuScreenName;

        //---Public variables for finding references---//
        public Button DefaultHighlightedButton;
        public Button DefaultReturnToHighlightButton;
        protected Button temporaryReturnToHighlightButton;

        //---Private variables for initialization---//
        protected bool initialized = false;
        protected bool screenOpen = false;

        //---Private variables for finding references---//
        protected PartyMenuHandler partyMenuHandler;

        //---Private constants---//
        protected const string Highlighted = "Highlighted";


        #region Initialization
        /// <summary>
        /// Initializes a screen.
        /// </summary>
        public virtual void Initialize()
        {
            partyMenuHandler = GameObject.FindObjectOfType<PartyMenuHandler>();
        }

        public abstract void UpdateScreen();

        /// <summary>
        /// Ends the initialization process.
        /// </summary>
        protected virtual void EndInitialization()
        {
            initialized = true;
        }

        #endregion Initialization

        #region Open/Close
        /// <summary>
        /// Opens the requested screen.
        /// </summary>
        public virtual void OpenScreen()
        {
            Debug.Log("Opening screen.");
            this.gameObject.SetActive(true);
            screenOpen = true;

            if (GetDefaultHighlightedButton() != null)
            {
                StartCoroutine(SetHighlightedButton(GetDefaultHighlightedButton().gameObject));
            }
        }

        /// <summary>
        /// Assigns the default button to be highlighted.
        /// </summary>
        public virtual void AssignDefaultButton()
        {
            if (temporaryReturnToHighlightButton != null)
            {
                StartCoroutine(SetHighlightedButton(temporaryReturnToHighlightButton.gameObject));
                temporaryReturnToHighlightButton = null; //set to null after using it
            }
            else if (DefaultHighlightedButton != null)
            {
                StartCoroutine(SetHighlightedButton(DefaultHighlightedButton.gameObject));
            }
        }

        /// <summary>
        /// Closes the requested screen.
        /// </summary>
        public virtual void CloseScreen()
        {
            screenOpen = false;
        }

        #endregion Open/Close

        #region Inputs
        /// <summary>
        /// Handles specific screen inputs.
        /// </summary>
        public abstract void HandleScreenInputs(MenuInputs menuInput);

        /// <summary>
        /// Sets the highlighted button after a short wait time.
        /// </summary>
        protected IEnumerator SetHighlightedButton(GameObject newHighlightedObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
            partyMenuHandler.AssignHighlightedButtonDirectly(null);
            yield return new WaitForEndOfFrame();
            EventSystem.current.SetSelectedGameObject(newHighlightedObject);
            partyMenuHandler.AssignHighlightedButtonDirectly(newHighlightedObject.GetComponent<Button>());
            Debug.Log("<color=blue>Now setting highlighted object as: " + EventSystem.current.currentSelectedGameObject.name + ".</color>");
        }

        #endregion Inputs

        #region Getters
        /// <summary>
        /// Returns the default button that is to be highlighted for this screen.
        /// </summary>
        public Button GetDefaultHighlightedButton()
        {
            if (temporaryReturnToHighlightButton != null)
            {
                return temporaryReturnToHighlightButton;
            }

            return DefaultHighlightedButton;
        }

        /// <summary>
        /// Returns whether or not this screen is open.
        /// </summary>
        public bool IsScreenOpen()
        {
            return screenOpen;
        }

        #endregion Getters


        #region Setters
        /// <summary>
        /// Sets a button to be the new temporary return to highlight button.
        /// </summary>
        public void SetTemporaryReturnToHighlightButton(Button newButton)
        {
            temporaryReturnToHighlightButton = newButton;
        }

        /// <summary>
        /// Clears out the temporary return to highlight button.
        /// </summary>
        public void ClearTemporaryReturnToHighlightButton()
        {
            temporaryReturnToHighlightButton = null;
        }
        #endregion Setters
    }
}
