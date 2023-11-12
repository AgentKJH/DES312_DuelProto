using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Unity.VisualScripting;
using UnityEditor;
using NUnit.Framework.Constraints;

public class HeroController : MonoBehaviour, IDamageable
{

    [SerializeField] float      m_speed = 4.0f;
    //[SerializeField] float      m_jumpForce = 7.5f;
    //[SerializeField] float      m_rollForce = 6.0f;
    [SerializeField] bool       m_noBlood = false;
    [SerializeField] GameObject m_blockFlash;

    // Attack Sensor postion vars
    private Vector3 m_attackSensorPosRight = new Vector3(0.915000021f, 0.722000003f, 0);
    private Vector3 m_attackSensorPosLeft = new Vector3(-0.984000027f, 0.722000003f, 0);

    public int PlayerNumber;

    // ++ character stats ++
    // public
    public EplayerState m_playerState = EplayerState.Default;
    public float m_damage = 20f;
    public float m_maxHealth = 100f;
    public float m_health;
    public float m_maxEnergy = 100f;
    public float m_energy = 100f;
    public bool m_might = false;
    public bool m_doClash = false;

    private float m_clashDamageMultiplier = 0.5f;
    private bool m_grounded = false;
    private int m_facingDirection = 1;
    private float m_clashForce = 5000f;

    // attack
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private float m_energyAttackCost = 20f;


    // energy
    private float m_timeSinceEnergyGain = 0.0f;
    private float m_energyGainInterval = 0.5f;
    private float m_energyGainAmount = 5f;

    // block 
    private float m_blockDamageMultiplier = 0.5f;
    private float m_blockEnergyDamageMultiplier = 2f;


    private float m_delayToIdle = 0.0f;

    // roll
    //private float m_rollDuration = 8.0f / 14.0f;
    //private float m_rollCurrentTime;

    // compoent refs
    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_HeroKnight   m_groundSensor;
    private Sensor_HeroAttack   m_attackSensor;
    [SerializeField] ResourceBar m_healthBar;
    [SerializeField] ResourceBar m_energyBar;

    //private bool                m_isWallSliding = false;
    private bool                m_rolling = false;


    // Movement var
    private float m_moveDir;
    private Vector2 m_moveVector;
    public bool m_canMove;
   

    /// <summary>
    /// Enum for tracking player state
    /// </summary>
    public enum EplayerState
    {
        Default,
        Attacking,
        Blocking,
        Dead
    }

    // Initialization, get compoent refs
    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        //m_attackSensor = transform.Find("AttackSensor").GetComponent<Sensor_HeroAttack>();
        m_attackSensor = GetComponentInChildren<Sensor_HeroAttack>();

        m_health = m_maxHealth;

        // add player to player manager
        PlayerNumber =  PlayerManager.Instance.PlayerJoined(this);
        gameObject.name = "Hero - Player" + PlayerNumber;
        print("Player Joined: Player " + PlayerNumber);
    }

    // ++ Inputs ++
    /// <summary>
    /// Gets movement value from Movement Input Action
    /// </summary>
    /// <param name="value"></param>
    public void OnMovement(InputAction.CallbackContext context)
    {
        if (m_playerState == EplayerState.Default && m_canMove) // Sets movement value when playerState is Default
        {
            m_moveDir = context.ReadValue<float>();
            m_moveVector = new Vector2(m_moveDir * m_speed, m_body2d.velocity.y); 
            m_playerState = EplayerState.Default;
        }
    }

    /// <summary>
    /// Called by Attack Input Action
    /// </summary>
    public void OnAttack()
    {
        if (m_timeSinceAttack > 0.25f && m_playerState != EplayerState.Dead && m_playerState != EplayerState.Blocking && m_energy - m_energyAttackCost > -0.1f)
        {
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
            m_attackSensor.DoAttack();
            m_playerState = EplayerState.Attacking;

            // Reset timer
            m_timeSinceAttack = 0.0f;
        }
    }

    /// <summary>
    /// Called by Block Input Action
    /// </summary>
    public void OnBlock(InputAction.CallbackContext context)
    {

        switch (context.phase)
        {
            case InputActionPhase.Performed:
                print("Performed");
                break;
            case InputActionPhase.Started:
                print("Started");
                if (m_playerState == EplayerState.Default)
                {
                    m_playerState = EplayerState.Blocking;
                    m_animator.SetBool("IdleBlock", true);
                }
                break;
            case InputActionPhase.Canceled:
                print("Canceled");
                m_playerState = EplayerState.Default;
                m_animator.SetBool("IdleBlock", false);
                break;
        }

    }

    private void FixedUpdate()
    {
        // Movement 
        if (m_playerState == EplayerState.Default)
        {
            m_body2d.MovePosition(m_body2d.position + m_moveVector * Time.fixedDeltaTime);
        } else m_body2d.velocity = Vector2.zero; // Stops velocity if not in default state
    }

    //Update is called once per frame
    void Update()
    {
        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;
        m_timeSinceEnergyGain += Time.deltaTime;

        if (m_timeSinceAttack > 0.15 && m_playerState == EplayerState.Attacking)
        {
            m_playerState = EplayerState.Default;
        }
        

        // energy regen
        if (m_timeSinceEnergyGain > m_energyGainInterval && m_energy != m_maxEnergy && m_playerState != EplayerState.Dead)
        {
            m_energy += m_energyGainAmount;
            m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy);
            if  (m_energy > m_maxEnergy)
            {
                m_energy = m_maxEnergy;
            }
            m_timeSinceEnergyGain = 0.0f;
            //print("Energy Updated - Player: " + PlayerNumber);
        }

        // Increase timer that checks roll duration
        //if (m_rolling)
        //    m_rollCurrentTime += Time.deltaTime;
        // Disable rolling if timer extends duration
        //if (m_rollCurrentTime > m_rollDuration)
        //    m_rolling = false;

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
        if (m_moveDir > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false; // flip sprite
            m_facingDirection = 1;
            m_attackSensor.transform.localPosition = m_attackSensorPosRight; // move attack sensor
        }
        else if (m_moveDir < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true; // flip sprite
            m_facingDirection = -1;
            m_attackSensor.transform.localPosition = m_attackSensorPosLeft; //move attack sensor
        }

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        //Run
        if (Mathf.Abs(m_moveDir) > Mathf.Epsilon)
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

    public float m_lastDamageTaken;

    // damage taken interface, check player state and 
    public void Damage(float damageAmount, GameObject attackerRef)
    {
        print("controller Damaged " + m_playerState);
        switch (m_playerState)
        {
            case EplayerState.Default:
                PlayerManager.Instance.CanClash();
                m_lastDamageTaken = damageAmount;
                TakeDamage(damageAmount);
                break;
            case EplayerState.Attacking:
                PlayerManager.Instance.CanClash();
                m_lastDamageTaken = damageAmount;
                TakeDamage(damageAmount);

                //ClashReceive();
                //ClashSend(attackerRef); // calls clash receive on other character
                //Instantiate(m_blockFlash, this.transform.position + new Vector3(-0.2f, 0.7f, 0), UnityEngine.Quaternion.identity);
                //TakeDamage(damageAmount);
                break;
            case EplayerState.Blocking:
                if (m_energy - (damageAmount * m_blockEnergyDamageMultiplier) >= 0)
                {
                    m_energy -= damageAmount * m_blockEnergyDamageMultiplier;
                    m_animator.SetTrigger("Block");

                }
                else
                {
                    float adjustedDamage = (damageAmount * m_blockEnergyDamageMultiplier) - m_energy;
                    m_energy = 0;
                    m_energyBar.UpdateResourceBar(m_energy, m_maxEnergy);
                    TakeDamage(adjustedDamage * m_blockDamageMultiplier);
                }
                //m_animator.SetTrigger("Block");
                break;
            case EplayerState.Dead:
                break;
        }
    }

    public void ClashReceive()
    {
        m_body2d.AddForceX(m_clashForce * (m_facingDirection * -1)); // apply knockback
        m_health += m_lastDamageTaken * m_clashDamageMultiplier;  // gain health back
        Instantiate(m_blockFlash, this.transform.position + new Vector3(-0.2f, 0.7f, 0), UnityEngine.Quaternion.identity);
        print("clashed Player" + PlayerNumber);
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
        }
        else //Death
        {
            m_playerState = EplayerState.Dead;
            m_health = 0;
            m_healthBar.UpdateResourceBar(m_health, m_maxHealth);
            print("Player " + PlayerNumber + " is Dead");
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");

            PlayerManager.Instance.GameOver(PlayerNumber);
        }
    }

    // Animation Events
    // Called in slide animation.
    //void AE_SlideDust()
    //{
    //    Vector3 spawnPosition;

    //    if (m_facingDirection == 1)
    //        spawnPosition = m_wallSensorR2.transform.position;
    //    else
    //        spawnPosition = m_wallSensorL2.transform.position;

    //    if (m_slideDust != null)
    //    {
    //        // Set correct arrow spawn position
    //        GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
    //        // Turn arrow in correct direction
    //        dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
    //    }
    //}
}
