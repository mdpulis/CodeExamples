using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GameHandler : MonoBehaviour
{
    private RecycleBin aluminumRB;
    private RecycleBin compostRB;
    private RecycleBin electronicRB;
    private RecycleBin glassRB;
    private RecycleBin nonRecyclableRB;
    private RecycleBin paperRB;
    private RecycleBin plasticRB;

    private bool initialized = false;
    private bool playingGame = false;

    private TrashSpawner trashSpawner;
    private USMap usMap;

    public AudioClip backgroundAudio;
    public AudioClip startSound;   
    
    public GameObject keyboard;
    public GameObject rightMallet;
    public GameObject leftMallet;    

    private void Awake()
    {
        aluminumRB = GameObject.FindObjectsOfType<RecycleBin>().Where(x => x.BinTrashType == TrashTypes.Aluminum).FirstOrDefault();
        compostRB = GameObject.FindObjectsOfType<RecycleBin>().Where(x => x.BinTrashType == TrashTypes.Compost).FirstOrDefault();
        electronicRB = GameObject.FindObjectsOfType<RecycleBin>().Where(x => x.BinTrashType == TrashTypes.Electronic).FirstOrDefault();
        glassRB = GameObject.FindObjectsOfType<RecycleBin>().Where(x => x.BinTrashType == TrashTypes.Glass).FirstOrDefault();
        nonRecyclableRB = GameObject.FindObjectsOfType<RecycleBin>().Where(x => x.BinTrashType == TrashTypes.NonRecyclable).FirstOrDefault();
        paperRB = GameObject.FindObjectsOfType<RecycleBin>().Where(x => x.BinTrashType == TrashTypes.Paper).FirstOrDefault();
        plasticRB = GameObject.FindObjectsOfType<RecycleBin>().Where(x => x.BinTrashType == TrashTypes.Plastic).FirstOrDefault();

        trashSpawner = GameObject.FindObjectOfType<TrashSpawner>();
        usMap = GameObject.FindObjectOfType<USMap>();
        setKeyboardEnabled(false);
        AudioManager.Instance.PlayLoop(backgroundAudio, transform);
    }

    /// <summary>
    /// Resets parameters to default
    /// </summary>
    public void RestartGame()
    {
        aluminumRB.gameObject.SetActive(true);
        compostRB.gameObject.SetActive(true);
        electronicRB.gameObject.SetActive(true);
        glassRB.gameObject.SetActive(true);
        nonRecyclableRB.gameObject.SetActive(true);
        paperRB.gameObject.SetActive(true);
        plasticRB.gameObject.SetActive(true);

        setKeyboardEnabled(false);
        usMap.SetUSMapCondition(USMapConditions.SelectingState);
        RestartUIButton.SetRestartUIButtonToNull();
    }

    /// <summary>
    /// Sets up the game by county info
    /// </summary>
    public void SetupGame(County countyInfo)
    {
        if (!countyInfo.RecyclesAluminum)
            aluminumRB.gameObject.SetActive(false);
        if (!countyInfo.RecyclesCompost)
            compostRB.gameObject.SetActive(false);
        if (!countyInfo.RecyclesElectronic)
            electronicRB.gameObject.SetActive(false);
        if (!countyInfo.RecyclesGlass)
            glassRB.gameObject.SetActive(false);
        if (!countyInfo.RecyclesNonRecyclable)
            nonRecyclableRB.gameObject.SetActive(false);
        if (!countyInfo.RecyclesPaper)
            paperRB.gameObject.SetActive(false);
        if (!countyInfo.RecyclesPlastic)
            plasticRB.gameObject.SetActive(false);

        trashSpawner.StartTrashSpawner(countyInfo);
        AudioManager.Instance.Play(startSound, transform);

        setKeyboardEnabled(false);

        initialized = true;
        playingGame = true;
    }


    /// <summary>
    /// Ends the gameplay
    /// </summary>
    public void EndGame()
    {
        playingGame = false;
        trashSpawner.StopTrashSpawner();
        usMap.SetUSMapCondition(USMapConditions.GameOver);

        setKeyboardEnabled(true);
    }
    public void setKeyboardEnabled (bool enabled)
    {
        if(enabled)
        {
            keyboard.GetComponent<VRKeys.Keyboard>().Enable();
        }
        else
        {
            keyboard.GetComponent<VRKeys.Keyboard>().Disable();            
        }    
      
        rightMallet.SetActive(enabled);
        leftMallet.SetActive(enabled);
    }


    /// <summary>
    /// Checks to see if the game handler is initialized
    /// </summary>
    public bool IsInitialized()
    {
        return initialized;
    }

    /// <summary>
    /// Checks to see if the game handler is in the playing game state
    /// </summary>
    public bool IsPlayingGame()
    {
        return playingGame;
    }

}
