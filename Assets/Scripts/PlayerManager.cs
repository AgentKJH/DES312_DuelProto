using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class PlayerManager : MonoBehaviour
{
    // ++ Refs ++
    // Compoents
    [SerializeField] PlayerInputManager InputManager;
    [SerializeField] TextMeshProUGUI bannerText;

    // Player starting location trasforms
    [SerializeField] Transform player1StartPositon;
    [SerializeField] Transform player2StartPositon;

    /// <summary>
    /// List of player Controllers
    /// </summary>
    public List<HeroController> HeroControllers;
    /// <summary>
    /// Singleton Instance of the PlayerManager
    /// </summary>
    public static PlayerManager Instance;

    // ++ Clash ++
    private float m_clashWindowTimer;
    private float m_clashWindowDuration = 0.15f;
    public bool m_doClash = false;

    // ++ Data Tracking ++ 
    /// <summary>
    /// Stores duel instance data
    /// </summary>
    public Dictionary<string, object> DuelOverData = new Dictionary<string, object>();
    /// <summary>
    /// bool to check in PM (player manager) should track data
    /// </summary>
    bool d_PMtrackData = false; 
    public float d_totalDuelTime = 0;
    int d_numberOfClashes = 0;


    private void Awake()
    {
        Instance = this; // Set up singleton
    }


    /// <summary>
    /// Called when a input device is connected, handles player number allocation, storing player refs and starting the game when both players are in.
    /// </summary>
    /// <param name="playerCharacterRef"></param>
    /// <returns></returns>
    public int PlayerJoined(HeroController playerCharacterRef)
    {
        if (HeroControllers.Count < 2)
        {
            List<HeroController> list = HeroControllers; list.Add(playerCharacterRef);
            if (HeroControllers.Count == 1)
            {
                playerCharacterRef.transform.position = player1StartPositon.transform.position; 
            }
            if (HeroControllers.Count == 2)
            {
                playerCharacterRef.transform.position = player2StartPositon.transform.position;
                foreach (HeroController controller in HeroControllers) { controller.m_playerUnlocked = false; } // disenables movement for characters
                Invoke("StartGame", 0.5f); // start game with a short delay
            } 
            return HeroControllers.Count;
        }
        else 
        {
            Debug.LogError("ERROR - to many players joined - PlayerManager");
            return 0;
        }

    }

    /// <summary>
    /// Start the duel and start tracking data;
    /// </summary>
    public void StartGame()
    {
        HeroControllers[0].transform.position = player1StartPositon.transform.position;
        HeroControllers[1].transform.position = player2StartPositon.transform.position;
        d_PMtrackData = true;
        foreach (HeroController controller in HeroControllers) { controller.m_playerUnlocked = true; controller.d_trackData = true;  } // enable movement and start tracking data on each character
        bannerText.text = "Fight!"; //update banner
    }

    /// <summary>
    /// Called when a character dies, displays victory text and sends game data
    /// </summary>
    /// <param name="deadPlayerNumber"></param>
    public void DuelOver(int deadPlayerNumber)
    {
        if (deadPlayerNumber == 1)
        {
            bannerText.text = "Player 2 Wins";
        } else if (deadPlayerNumber == 2)
        {
            bannerText.text = "Player 1 Wins";
        }
        // data
        d_PMtrackData = false;
        foreach (HeroController controller in HeroControllers) { controller.StoreData(); } // store collected data in HeroControllers

        DuelOverData.Add("PlayerDefeated", deadPlayerNumber);
        DuelOverData.Add("TotalDuelTime", d_totalDuelTime);
        DuelOverData.Add("NumberOfClashes", d_numberOfClashes);

        GameManager.Instance.SendGameData("DuelOverData", DuelOverData);
    }

    private int clashPlayerNumber = 0;
    public void CanClash(int playerNumber)
    {
        if (m_doClash && clashPlayerNumber != playerNumber)
        {
            foreach (HeroController controller in HeroControllers)
            {
                if (d_PMtrackData) { d_numberOfClashes++; }
                controller.ClashReceive();
            }
        } else
        {
            m_clashWindowTimer = 0;
            clashPlayerNumber = playerNumber;
            m_doClash = true;
        }

    }

    /// <summary>
    /// Takes in the playerNumber and calls ClashReceive on the other player
    /// </summary>
    /// <param name="playerNumber"></param>
    public void BlockClash(int playerNumber)
    {
        if (playerNumber == 1)
        {
            HeroControllers[1].ClashReceive();
        } else if (playerNumber == 2)
        {
            HeroControllers[0].ClashReceive();
        }
    }

    private void Update()
    {
        // update clash timer
        if (m_doClash)
        {
            m_clashWindowTimer += Time.deltaTime;
            if (m_clashWindowTimer > m_clashWindowDuration)
            {
                m_doClash = false;
                m_clashWindowTimer = 0;
            }
        }

        // track time dueling
        if (d_PMtrackData) { d_totalDuelTime += Time.deltaTime; }

    }
}
