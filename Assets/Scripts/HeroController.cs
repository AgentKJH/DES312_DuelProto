using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class HeroController : MonoBehaviour, IDamageable
{
    // ++ Declaration ++
    public UnityEvent DoAttack;

    // ++ Refs ++ ---------------------------------------------------------------------------------------------
    // Attack Sensor postion vars
    private Vector3 m_attackSensorPosRight = new Vector3(0.915000021f, 0.722000003f, 0);
    private Vector3 m_attackSensorPosLeft = new Vector3(-0.984000027f, 0.722000003f, 0);

    [SerializeField] GameObject m_blockFlash; //block flash asset
    [SerializeField] Material m_SpriteDefaultMaterial;
    [SerializeField] Material m_SpriteFlashMaterial;

    // component refs
    private Animator m_animator;
    private Rigidbody2D m_body2d;
    private Sensor_HeroKnight m_groundSensor;
    private Sensor_HeroAttack m_attackSensor;
    [SerializeField] ResourceBar m_healthBar;
    [SerializeField] ResourceBar m_energyBar;
    [SerializeField] SpriteRenderer m_spriteRenderer;
    [SerializeField] GameObject m_iconObj;
    [SerializeField] SpriteRenderer m_iconRenderer;
    [SerializeField] SoundManager m_soundManager;

    // ++ Player Stat Trackers ++ -----------------------------------------------------------------------------
    /// <summary>
    /// Tracks the number of times the action was taken
    /// </summary>
    int d_attacks = 0, d_blocks = 0, d_hits = 0, d_blockedHits = 0;
    float d_totalEnergyUsed = 0, d_totalEnergyGenerated = 0, d_energyAverage = 0, d_duelTimeAtHalfHealthReached = 0;
    float d_totalDistanceTraveled = 0;
    bool d_playerDefeated = false, reachedHalfHealth = false;
    public Dictionary<string, object> HeroDuelData = new Dictionary<string, object>();
    /// <summary>
    /// Stores previous player location for total distance traveled calculation
    /// </summary>
    Vector2 d_previousLocation;

    // ++ Character Stats ++ ----------------------------------------------------------------------------------
    // Player
    public int PlayerNumber;
    public bool d_trackData = false;
    public bool m_playerUnlocked = true;
    public EplayerState m_playerState = EplayerState.Default;

    // Base stats
    public float m_damage = 10f;
    public float m_maxHealth = 100f;
    public float m_health;
    public float m_maxEnergy = 100f;
    public float m_energy = 100f;
    [SerializeField] float m_speed = 4.0f;

    [SerializeField] bool m_noBlood = false;

    private float m_clashDamageMultiplier = 0.5f;
    private int m_facingDirection = 1;

    // movement
    private float m_moveDir;
    private Vector2 m_moveVector;

    // attack
    /// <summary>
    /// Attack number used to change animation
    /// </summary>
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private float m_energyAttackCost = 30f;
    private float m_attackKnockbackForce = 1000f;

    // energy
    private float m_timeSinceEnergyGain = 0.0f;
    private float m_energyGainInterval = 0.5f;
    private float m_energyGainAmount = 10f;

    // block 
    /// <summary>
    /// Used to calculate the chip damage taken when blocking
    /// </summary>
    private float m_blockDamageMultiplier = 0.25f;
    /// <summary>
    /// Used to calulate the energy lost when blocking an attack based on the damage of the attack
    /// </summary>
    private float m_blockEnergyCostMultiplier = 1f;
    private bool m_didBlockAttack = false;
    /// <summary>
    /// Delay before block can be triggered again
    /// </summary>
    private float m_blockDelay;
    private float m_blockKnockbackForce = 5000f;

    // roll
    private float m_rollDuration = 8.0f / 14.0f;
    private float m_rollCurrentTime;
    private float m_rollEnergyCost = 15f;
    private float m_rollForce = 6f;

    // vulnerable
    private float m_vulnerableTime;
    private float m_vulnerableDamageMultiplier = 1.5f;

    // clash
    public bool m_doClash = false;

    // misc
    private float m_delayToIdle = 0.0f;
    private bool m_grounded = false;


    // ++ More Set Up ++ ----------------------------------------------------------------------------------------
    /// <summary>
    /// Enum for tracking player state
    /// </summary>
    public enum EplayerState
    {
        Default,
        Attacking,
        Blocking,
        Rolling,
        Vulnerable,
        Dead
    }

    // Initialization, get compoent refs
    void Start()
    {
        // get component refs
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_attackSensor = GetComponentInChildren<Sensor_HeroAttack>();

        CharacterSetup();       
    }

    private void CharacterSetup()
    {
        // set starting stats
        m_health = m_maxHealth;

        // add player to player manager
        PlayerNumber = PlayerManager.Instance.PlayerJoined(this);
        gameObject.name = "Hero - Player" + PlayerNumber;
        print("Player Joined: Player " + PlayerNumber);

        // Set up player direction
        if (PlayerNumber == 1)
        {
            m_facingDirection = 1;
            m_attackSensor.transform.localPosition = m_attackSensorPosRight; // move attack sensor
            m_healthBar.transform.localPosition = new Vector3(-112, 70, 0);
            m_energyBar.transform.localPosition = new Vector3(-130, 70, 0);

        }
        else if (PlayerNumber == 2)
        {
            print("Player2 setup");
            m_facingDirection = -1;
            GetComponent<SpriteRenderer>().flipX = true; // flip sprite
            m_attackSensor.transform.localPosition = m_attackSensorPosLeft; // move attack sensor
            m_healthBar.transform.localPosition = new Vector3(112, 70, 0);
            m_energyBar.transform.localPosition = new Vector3(130, 70, 0);
        }
        else
        {
            Debug.LogError("CharacterSetup/HeroController PlayerNumber is an unexpected value"); // log error
        }
    }

    // ++ Inputs ++ ----------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Gets movement value from Movement Input Action
    /// </summary>
    /// <param name="value"></param>
    public void OnMovement(InputAction.CallbackContext context)
    {
        m_moveDir = context.ReadValue<float>();
        m_moveVector = new Vector2(m_moveDir * m_speed, m_body2d.velocity.y);        
    }

    /// <summary>
    /// Called by Attack Input Action
    /// </summary>
    public void OnAttack()
    {
        if (m_timeSinceAttack > 0.25f && m_playerState == EplayerState.Default && m_energy - m_energyAttackCost >= 0 && m_playerUnlocked)
        {
            if (d_trackData) { d_attacks++; d_totalEnergyUsed += m_energyAttackCost; } // track attack action and energy used 

            m_energy -= m_energyAttackCost;
            m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy);
            m_currentAttack++;

            // Loop back to one after third attack
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            // Reset Attack combo if time since last attack is too large
            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            // Call one of three attack animations "Attack1", "Attack2", "Attack3"
            m_animator.SetTrigger("Attack" + m_currentAttack);
            m_playerState = EplayerState.Attacking;
            m_soundManager.PlayAttackSound();

            // Reset timer
            m_timeSinceAttack = 0.0f;
        } else if (m_playerUnlocked && m_playerState == EplayerState.Default && m_timeSinceAttack > 0.25f) { m_energyBar.BackgroundFlash(0.1f); } // flash energy bar when not enough energy

    }
    
    /// <summary>
    /// Invokes Unity Event triggered by attack animations, setup to call DoAttack() on Sensor_HeroAttack
    /// </summary>
    public void TriggerAttack()
    {
        DoAttack?.Invoke();
    }

    /// <summary>
    /// Called by Block Input Action
    /// </summary>
    public void OnBlock(InputAction.CallbackContext context)
    {
        bool blockExcuted = false;
        if (m_playerUnlocked)
        {
            switch (context.phase)
            {
                case InputActionPhase.Started: // Started blocking
                    //print("Started");
                    if (m_playerState == EplayerState.Default && m_blockDelay <= 0)
                    {
                        if (d_trackData) { d_blocks++; } // track action

                        m_didBlockAttack = false;
                        m_playerState = EplayerState.Blocking;
                        blockExcuted = true;
                        m_animator.SetBool("IdleBlock", true);
                    }
                    break;
                case InputActionPhase.Canceled:
                    //print("Canceled");
                    if (m_didBlockAttack && m_playerState != EplayerState.Dead) 
                    {
                        m_playerState = EplayerState.Default;
                        m_animator.SetBool("IdleBlock", false);
                        if (blockExcuted)
                        {
                            m_blockDelay = 0.3f;
                        }

                    }
                    else if (m_playerState != EplayerState.Dead)
                    {
                        Vulnerable(0.2f);
                        m_animator.SetBool("IdleBlock", false);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Called by Roll Input Action. Handles Roll mechanic logic
    /// </summary>
    /// <param name="context"></param>
    public void OnRoll()
    {
        int rollDir;
        if (m_playerState == EplayerState.Default && m_energy - m_rollEnergyCost > 0 && m_playerUnlocked)
        {
            m_playerState = EplayerState.Rolling;
            m_animator.SetTrigger("Roll");
            if (m_moveDir == 0) { rollDir = m_facingDirection * -1; } else if (m_moveDir > 0) { rollDir = 1; } else { rollDir = -1; } // set up roll direction based on movement direction input, if no input roll backwards. Set to int values to keep set distance regardless input.
            m_body2d.velocity = new Vector2(rollDir * m_rollForce, m_body2d.velocity.y);
            // update energy
            m_energy -= m_rollEnergyCost;
            m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy);
        } else if (m_playerUnlocked && m_playerState == EplayerState.Default)
        {
            m_energyBar.BackgroundFlash(0.2f);  // flash energy bar when not enough energy
        } 
       
    }

    // ++ Data Collection ++ -----------------------------------------------------------------------------------------
    /// <summary>
    /// Updated d_totalDistanceTraveled for data collection
    /// </summary>
    void RecordDistance()
    {
        d_totalDistanceTraveled += Vector2.Distance(transform.position, d_previousLocation);
        d_previousLocation = transform.position;
    }

    /// <summary>
    /// Stops tracking data and stores all character data in the HeroDuelData Dictionary
    /// </summary>
    public void StoreData()
    {
        d_trackData = false; // stop tracking data

        HeroDuelData.Add("PlayerNumber", PlayerNumber);
        HeroDuelData.Add("PlayerDefeated", d_playerDefeated);
        HeroDuelData.Add("NumberOfAttacksInput", d_attacks);
        HeroDuelData.Add("NumberOfBlocksInput", d_blocks);
        HeroDuelData.Add("NumberOfHitsBlocked", d_blockedHits);
        HeroDuelData.Add("NumberOfHitsTaken", d_hits);
        HeroDuelData.Add("TotalEnergyUsed", d_totalEnergyUsed);
        HeroDuelData.Add("TotalEnergyGenerated", d_totalEnergyGenerated);
        d_energyAverage = d_totalEnergyUsed / PlayerManager.Instance.d_totalDuelTime; // calculate average energy used
        HeroDuelData.Add("AverageEnergyUsed", d_energyAverage);
        HeroDuelData.Add("TotalDistanceTraveled", d_totalDistanceTraveled);
        HeroDuelData.Add("TimeAtHalfHealthReached", d_duelTimeAtHalfHealthReached);

        GameManager.Instance.SendGameData("PlayerDuelOverData", HeroDuelData);
    }


    // ++ Updates ++ ------------------------------------------------------------------------------------------------------------
    private void FixedUpdate()
    {
        // Movement 
        if (m_playerState == EplayerState.Default && m_playerUnlocked)
        {
            if (d_trackData) { RecordDistance(); } // track action

            m_body2d.MovePosition(m_body2d.position + m_moveVector * Time.fixedDeltaTime); // move character with rb
        }
        else if (m_playerState != EplayerState.Rolling)
        {
            m_body2d.velocity = Vector2.zero; // Stops velocity if not in default state
        }
    }

    bool setVulerableFromEnergy = false;

    void Update()
    {
        // ++ Debug ++ -------------------------------------------------------------------------------------------------------------------
        if (Input.GetKeyDown(KeyCode.P))
        {
            print(name + " State: " + m_playerState);
        }

        // ++ Attack ++ -----------------------------------------------------------------------------------------------------------
        m_timeSinceAttack += Time.deltaTime; // Increase timer that controls attack combo

        if (m_timeSinceAttack > 0.15 && m_playerState == EplayerState.Attacking) // Set player state back to default after attack timer
        {
            if (m_playerState != EplayerState.Dead)
            {
                m_playerState = EplayerState.Default;
            }
        }

        // ++ Vulernable ++ ----------------------------------------------------------------------------------------------------------------
        // vulernable time update
        if (m_playerState == EplayerState.Vulnerable)
        {
            m_vulnerableTime -= Time.deltaTime;
            if (m_vulnerableTime <= 0)
            {
                m_playerState = EplayerState.Default;
                m_spriteRenderer.material = m_SpriteDefaultMaterial;
                m_iconObj.SetActive(false);
                if (setVulerableFromEnergy) { setVulerableFromEnergy = false; }
            }
        }

        //// set Vulnerable when at 0 Stamina
        //if (m_energy <= 0f && m_playerState == EplayerState.Default && !setVulerableFromEnergy)
        //{
        //    Vulnerable(0.5f);
        //    m_energyBar.BackgroundFlash(0.5f);
        //    setVulerableFromEnergy = true;
        //}

        // ++ Block ++ ------------------------------------------------------------------------------------------------------------------------
        // count down block delay
        if (m_blockDelay > 0) { m_blockDelay -= Time.deltaTime; }

        // ++ Rolling ++-----------------------------------------------------------------------------------------------------------------------
        // Increase timer that checks roll duration
        if (m_playerState == EplayerState.Rolling)
        {
            m_rollCurrentTime += Time.deltaTime;
        }
        // Disable rolling if timer extends duration
        if (m_rollCurrentTime > m_rollDuration)
        {
            m_playerState = EplayerState.Default;
            m_rollCurrentTime = 0;
        }

        // ++ Energy Regen ++ -------------------------------------------------------------------------------------------------------------------
        m_timeSinceEnergyGain += Time.deltaTime; // Increase timer that controls energy gain delay

        if (m_timeSinceEnergyGain > m_energyGainInterval && m_energy != m_maxEnergy && m_playerState != EplayerState.Dead && m_playerState != EplayerState.Blocking)
        {
            if (m_energy + m_energyGainAmount >= m_maxEnergy)
            {
                d_totalEnergyGenerated += (m_maxEnergy - m_energy); // add remainder energy to tracker
                m_energy = m_maxEnergy; // set energy value to max
                m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy); // update energy bar UI element
            }
            else
            {
                d_totalEnergyGenerated += m_energyGainAmount; // add gained energy to tracker
                m_energy += m_energyGainAmount; // update energy value
                m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy); // update energy bar UI element
            }
            m_timeSinceEnergyGain = 0.0f; // reset time since last energy update
            //print("Energy Updated - Player: " + PlayerNumber);
        }


        // ++ Animation ++ -----------------------------------------------------------------------------------------------------------------------
        //Check if character just landed on the ground
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
            //print("Just landed " + m_grounded);
        }

        //Check if character just started falling
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            //print("Started Falling " + m_grounded);
        }

        // -- Handle input and movement --
        //float inputX = Input.GetAxis("Horizontal");

        // Handle direction swap of character on movement direction
        //if (m_moveDir > 0)
        //{
        //    GetComponent<SpriteRenderer>().flipX = false; // flip sprite
        //    m_facingDirection = 1;
        //    m_attackSensor.transform.localPosition = m_attackSensorPosRight; // move attack sensor
        //}
        //else if (m_moveDir < 0)
        //{
        //    GetComponent<SpriteRenderer>().flipX = true; // flip sprite
        //    m_facingDirection = -1;
        //    m_attackSensor.transform.localPosition = m_attackSensorPosLeft; //move attack sensor
        //}

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        //Run
        if (Mathf.Abs(m_moveDir) > Mathf.Epsilon && m_playerState == EplayerState.Default)
        {
            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }

        //Idle
        else
        {
            // Prevents flickering transitions to idle
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }
    }


    // ++ Damage ++-----------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Saved damage taken that can be recoved in a clash
    /// </summary>
    public float m_lastDamageTaken;

    // damage taken interface, check player state and apply damage accordling 
    public void Damage(float damageAmount, GameObject attackerRef)
    {
        //print("controller Damaged " + m_playerState);
        if (d_trackData && m_playerState != EplayerState.Dead) { d_hits++; } // track action
        switch (m_playerState)
        {
            case EplayerState.Default: 
                PlayerManager.Instance.CanClash(PlayerNumber); // enables potential to clash
                m_lastDamageTaken = damageAmount;

                m_soundManager.PlayHitSound();
                TakeDamage(damageAmount);
                PlayerManager.Instance.HitStop(0.2f);
                ReceiveKnockback(m_attackKnockbackForce, false);
                break;
            case EplayerState.Attacking:
                PlayerManager.Instance.CanClash(PlayerNumber); // enables potential to clash
                m_lastDamageTaken = damageAmount;

                m_soundManager.PlayHitSound();
                TakeDamage(damageAmount);
                PlayerManager.Instance.HitStop(0.15f);
                ReceiveKnockback(m_attackKnockbackForce, false);

                //ClashReceive();
                //ClashSend(attackerRef); // calls clash receive on other character
                //Instantiate(m_blockFlash, this.transform.position + new Vector3(-0.2f, 0.7f, 0), UnityEngine.Quaternion.identity);
                //TakeDamage(damageAmount);
                break;
            case EplayerState.Blocking:
                if (d_trackData) { d_blockedHits++; } // track block action

                m_didBlockAttack = true;

                if (m_energy - (damageAmount * m_blockEnergyCostMultiplier) >= 0) // remove chunck of energy based on damage and multiplier
                {
                    if (d_trackData) { d_totalEnergyUsed += damageAmount * m_blockEnergyCostMultiplier; } // track energy lost

                    // update energy
                    m_energy -= damageAmount * m_blockEnergyCostMultiplier;
                    m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy);

                    m_soundManager.PlayClashSound();
                    PlayerManager.Instance.HitStop(0.15f);
                    PlayerManager.Instance.KnockbackOther(PlayerNumber, m_blockKnockbackForce, false); // Calls BlockClash to knockback other player
                    m_animator.SetTrigger("Block");
                }
                else // lose remaining energy
                {
                    //float adjustedDamage = (damageAmount * m_blockEnergyDamageMultiplier) - m_energy;
                    if (d_trackData) { d_totalEnergyUsed += m_energy; } // track energy lost

                    // update energy
                    m_energy = 0;
                    m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy);

                    //TakeDamage(adjustedDamage * m_blockDamageMultiplier); // take adjusted damage
                }
                
                TakeDamage(damageAmount * m_blockDamageMultiplier);

                break;
            case EplayerState.Vulnerable:
                m_soundManager.PlayHitSound();
                PlayerManager.Instance.HitStop(0.2f);
                TakeDamage(damageAmount * m_vulnerableDamageMultiplier); // take vulnerable modified damage
                break;
        }
    }
    //public void ClashSend(GameObject attackRef)
    //{
    //    HeroController controller = gameObject.GetComponent<HeroController>();
    //    controller.ClashReceive();
    //}

    /// <summary>
    /// Handles damage taken and death when health reaches 0
    /// </summary>
    /// <param name="damageAmount"></param>
    private void TakeDamage(float damageAmount)
    {
        if (m_health - damageAmount > 0)
        {
            m_health -= damageAmount;
            m_healthBar.UpdateResourceBar(m_health, m_maxHealth);
            m_animator.SetTrigger("Hurt");
            print("Player" + PlayerNumber + " Damage Taken: " + damageAmount + " Health: " + m_health);

            if (m_health <= (m_maxHealth * 0.5) && !reachedHalfHealth) { d_duelTimeAtHalfHealthReached = PlayerManager.Instance.d_totalDuelTime; reachedHalfHealth = true; }
        }
        else //Death
        {
            d_playerDefeated = true; // track deafeated player
            PlayerManager.Instance.DuelOver(PlayerNumber);

            m_playerState = EplayerState.Dead;
            m_health = 0;
            m_healthBar.UpdateResourceBar(m_health, m_maxHealth);
            print("Player " + PlayerNumber + " is Dead");
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");

        }
    }

    // ++ Knockback ++ ------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Knocks back this player and displays blockFlash effect
    /// </summary>
    public void ReceiveKnockback(float knockbackForce, bool isClash)
    {
        m_body2d.AddForceX(knockbackForce * (m_facingDirection * -1)); // apply knockback
        if (isClash)
        {
            m_health += m_lastDamageTaken * m_clashDamageMultiplier; // gain health back from clash
            Instantiate(m_blockFlash, this.transform.position + new Vector3(-0.2f, 0.7f, 0), Quaternion.identity); // show block flash effect
            print("clashed Player" + PlayerNumber);
        }
    }

    // ++ Vulnerable ++ -------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Move to Vulerable player state
    /// </summary>
    private void Vulnerable(float timeVulnerable)
    {
        print("V");
        m_playerState = EplayerState.Vulnerable;
        m_iconObj.SetActive(true);
        m_spriteRenderer.material = m_SpriteFlashMaterial;
        m_vulnerableTime = timeVulnerable;
   }
}
