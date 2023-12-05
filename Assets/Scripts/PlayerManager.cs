using Abertay.Analytics;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class PlayerManager : MonoBehaviour
{
    // ++ Refs ++ -------------------------------------------------------------------------------------------
    // Compoents
    [SerializeField] PlayerInputManager InputManager;
    [SerializeField] TextMeshProUGUI bannerText;
    [SerializeField] SoundManager soundManager;

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

    // Clash
    private float m_clashWindowTimer;
    private float m_clashWindowDuration = 0.15f;
    public bool m_doClash = false;
    private float m_clashKnockbackForce = 5000f;

    // ++ Data Tracking ++ ----------------------------------------------------------------------------------------------
    /// <summary>
    /// Stores duel instance data
    /// </summary>
    public Dictionary<string, object> DuelOverData = new Dictionary<string, object>();
    int d_matchID;
    /// <summary>
    /// bool to check in PM (player manager) should track data
    /// </summary>
    bool d_PMtrackData = false; 
    public float d_totalDuelTime = 0;
    int d_numberOfClashes = 0;


    private void Awake()
    {
        Instance = this; // Set up singleton

        // create random matchID
        System.Random rnd = new System.Random();
        d_matchID = rnd.Next(0, 100000); // Range from 0 to 
    }

    private void Start()
    {
        print("matchID: " + d_matchID);
        AnalyticsManager.InitialiseWithCustomID(d_matchID.ToString());
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
        foreach (HeroController controller in HeroControllers) { controller.m_playerUnlocked = true; controller.d_trackData = true; } // enable movement and start tracking data on each character
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

        //DuelOverData.Add("PlayerDefeated", deadPlayerNumber);
        DuelOverData.Add("TotalDuelTime", d_totalDuelTime);
        DuelOverData.Add("NumberOfClashes", d_numberOfClashes);

        GameManager.Instance.SendGameData("DuelOverData", DuelOverData);
    }

    private int clashPlayerNumber = 0;
    public void CanClash(int playerNumber)
    {
        if (m_doClash && clashPlayerNumber != playerNumber) // Run Clash if called by other player within time frame
        {
            soundManager.PlayClashSound();
            foreach (HeroController controller in HeroControllers) // Run Clash effects
            {
                if (d_PMtrackData) { d_numberOfClashes++; }
                controller.ReceiveKnockback(m_clashKnockbackForce, true);
            }
        } else
        {
            m_clashWindowTimer = 0;
            clashPlayerNumber = playerNumber;
            m_doClash = true;
        }

    }

    /// <summary>
    /// Triggers knockback on the other player based on player number
    /// </summary>
    /// <param name="playerNumber"></param>
    /// <param name="knockbackForce"></param>
    /// <param name="isClash"></param>
    public void KnockbackOther(int playerNumber, float knockbackForce, bool isClash)
    {
        if (playerNumber == 1)
        {
            HeroControllers[1].ReceiveKnockback(knockbackForce, isClash);
        } else if (playerNumber == 2)
        {
            HeroControllers[0].ReceiveKnockback(knockbackForce, isClash);
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

    // ++ HitStop ++ -------------------------------------------------------------------------------------------------------------
    public void HitStop(float stopTime)
    {
        StartCoroutine(DoHitStop(stopTime));
    }

    IEnumerator DoHitStop(float stopTime)
    {
        print("hitstop");
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(stopTime);
        Time.timeScale = 1f;
    }
}


