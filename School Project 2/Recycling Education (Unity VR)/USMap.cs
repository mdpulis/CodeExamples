using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum USMapConditions
{
    SelectingState = 0,
    SelectingCounty = 1,
    PlayingGame = 2,
    GameOver = 3,
    Unselectable = 7,
}


/// <summary>
/// The US Map object
/// </summary>
public class USMap : MonoBehaviour
{
    public Transform CountyListLocation;
    public GameObject CountyOnMapPrefabObject_PO;

    private USMapConditions currentUSMapCondition = USMapConditions.SelectingState;
    private State selectedState = null;
    private County selectedCounty = null;

    private GameHandler gameHandler;

    private List<CountyOnMapPrefabObject> spawnedCountyOnMapPrefabObjects;

    private const float COUNTY_SPAWN_DIFF_Y = 0.6f;

    public AudioClip clickSound;
    private string audioFallbackResourcePath = "sounds/click";

    private void Awake()
    {
        spawnedCountyOnMapPrefabObjects = new List<CountyOnMapPrefabObject>();
        gameHandler = GameObject.FindObjectOfType<GameHandler>();
        if(clickSound==null)
        {
            clickSound = Resources.Load<AudioClip>(audioFallbackResourcePath);
        }
        
    }

    /// <summary>
    /// Checks for any presses on the map
    /// </summary>
    public void CheckForPressOnMap()
    {
        switch(currentUSMapCondition)
        {
            case (USMapConditions.SelectingState):
                if (StateOnMap.GetHighlightedWorldUIButton() != null)
                {
                    AudioManager.Instance.Play(clickSound, transform);
                    SelectState(StateOnMap.GetHighlightedWorldUIButton());
                }
                break;
            case (USMapConditions.SelectingCounty):
                if (CountyOnMapPrefabObject.GetHighlightedWorldUIButton() != null)
                {
                    AudioManager.Instance.Play(clickSound, transform);
                    SelectCounty(CountyOnMapPrefabObject.GetHighlightedWorldUIButton());
                }
                    
                break;
        }
        
    }


    /// <summary>
    /// Selects the state
    /// </summary>
    private void SelectState(StateOnMap som)
    {
        Debug.Log("state");

        selectedState = som.StateInfo;
        int spawnedObjects = 0;
        
        if(som.StateInfo.HasCounties())
        {
            foreach (County county in som.StateInfo.Counties)
            {
                CountyOnMapPrefabObject com_po = Instantiate(CountyOnMapPrefabObject_PO).GetComponent<CountyOnMapPrefabObject>();
                com_po.transform.SetParent(CountyListLocation, false);
                com_po.transform.position -= new Vector3(0.0f, COUNTY_SPAWN_DIFF_Y * spawnedObjects, 0.0f); //moves down the county list
                com_po.Initialize(county);
                spawnedCountyOnMapPrefabObjects.Add(com_po);
                spawnedObjects++;
            }

            //Initialize the back button too
            CountyOnMapPrefabObject com_backbutton = Instantiate(CountyOnMapPrefabObject_PO).GetComponent<CountyOnMapPrefabObject>();
            com_backbutton.transform.SetParent(CountyListLocation, false);
            com_backbutton.transform.position -= new Vector3(0.0f, COUNTY_SPAWN_DIFF_Y * spawnedObjects, 0.0f); //moves down the county list
            com_backbutton.Initialize(true);
            spawnedCountyOnMapPrefabObjects.Add(com_backbutton);
            spawnedObjects++;

            currentUSMapCondition = USMapConditions.SelectingCounty;
        }
        else
        {
            Debug.LogWarning("There were no counties for the selected state!");
        }

    }

    /// <summary>
    /// Goes back to the selecting state screen
    /// </summary>
    public void GoBackToSelectState()
    {
        selectedState = null;
        currentUSMapCondition = USMapConditions.SelectingState;
        ClearCountyOnMapPrefabObjects();
    }

    /// <summary>
    /// Selects the state
    /// </summary>
    private void SelectCounty(CountyOnMapPrefabObject com_po)
    {
        Debug.Log("county");

        //If we hit the back button, go back instead
        if(com_po.IsBackButton())
        {
            GoBackToSelectState();
            return;
        }


        selectedCounty = com_po.GetCountyInfo();

        if(gameHandler != null)
        {
            gameHandler.SetupGame(com_po.GetCountyInfo());
            currentUSMapCondition = USMapConditions.PlayingGame;
            ClearCountyOnMapPrefabObjects();
        }
        else
        {
            Debug.LogError("No Game Handler found!");
        }

    }

    /// <summary>
    /// Destroys all county on map prefab objects
    /// </summary>
    public void ClearCountyOnMapPrefabObjects()
    {
        foreach(CountyOnMapPrefabObject com_po in spawnedCountyOnMapPrefabObjects)
        {
            Destroy(com_po.gameObject);
        }

        spawnedCountyOnMapPrefabObjects.Clear();
    }

    /// <summary>
    /// Progresses the US map condition
    /// </summary>
    public void ProgressUSMapCondition()
    {
        switch(currentUSMapCondition)
        {
            case (USMapConditions.SelectingState):
                currentUSMapCondition = USMapConditions.SelectingCounty;
                break;
            case (USMapConditions.SelectingCounty):
                currentUSMapCondition = USMapConditions.PlayingGame;
                break;
            case (USMapConditions.PlayingGame):
                currentUSMapCondition = USMapConditions.GameOver;
                break;
            case (USMapConditions.GameOver):
                currentUSMapCondition = USMapConditions.SelectingState;
                break;
        }
    }

    /// <summary>
    /// Sets the US map condition to a specific selection
    /// </summary>
    public void SetUSMapCondition(USMapConditions newUSMapCondition)
    {
        currentUSMapCondition = newUSMapCondition;
    }
}
