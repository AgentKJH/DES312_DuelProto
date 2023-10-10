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
    [SerializeField] float      m_jumpForce = 7.5f;
    [SerializeField] float      m_rollForce = 6.0f;
    [SerializeField] bool       m_noBlood = false;

    private Vector3 m_attackSensorPosRight = new Vector3(0.915000021f, 0.722000003f, 0);
    private Vector3 m_attackSensorPosLeft = new Vector3(-0.984000027f, 0.722000003f, 0);

    public float damage = 20;
    public float maxHealth = 100;
    public float health;
    private float blockMultiplier = 0.1f;
    public EplayerState playerState = EplayerState.standing;

    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_HeroKnight   m_groundSensor;
    private Sensor_HeroAttack   m_attackSensor;
    private ResourceBar         m_healthBar;
    private bool                m_isWallSliding = false;
    private bool                m_grounded = false;
    private bool                m_rolling = false;
    private int                 m_facingDirection = 1;
    private int                 m_currentAttack = 0;
    private float               m_timeSinceAttack = 0.0f;
    private float               m_delayToIdle = 0.0f;
    private float               m_rollDuration = 8.0f / 14.0f;
    private float               m_rollCurrentTime;

    private float moveDir;
    private Vector2 moveVector;

    public enum EplayerState
    {
        standing,
        attacking,
        moving,
        blocking
    }

    // Use this for initialization
    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        //m_attackSensor = transform.Find("AttackSensor").GetComponent<Sensor_HeroAttack>();
        m_attackSensor = GetComponentInChildren<Sensor_HeroAttack>();
        m_healthBar = GetComponentInChildren<ResourceBar>();

        health = maxHealth;
    }

    // ++ Inputs ++
    void OnMovement(InputValue value)
    {
        if (playerState != EplayerState.attacking || playerState != EplayerState.blocking)
        {
            moveDir = value.Get<float>();
            moveVector = new Vector2(moveDir * m_speed, m_body2d.velocity.y);
            playerState = EplayerState.moving;
        }
    }

    void OnAttack()
    {
        if (m_timeSinceAttack > 0.25f && !m_rolling)
        {
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
            playerState = EplayerState.attacking;

            // Reset timer
            m_timeSinceAttack = 0.0f;
        }
    }

    private void FixedUpdate()
    {
        m_body2d.MovePosition(m_body2d.position + moveVector * Time.fixedDeltaTime);
    }

    //Update is called once per frame
    void Update()
    {
        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;

        if (m_timeSinceAttack > 0.15 && playerState == EplayerState.attacking)
        {
            playerState = EplayerState.standing;
        }

        // Increase timer that checks roll duration
        if (m_rolling)
            m_rollCurrentTime += Time.deltaTime;

        // Disable rolling if timer extends duration
        if (m_rollCurrentTime > m_rollDuration)
            m_rolling = false;

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

        // Swap direction of sprite depending on walk direction
        if (moveDir > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
            m_attackSensor.transform.localPosition = m_attackSensorPosRight;
        }
        else if (moveDir < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
            m_attackSensor.transform.localPosition = m_attackSensorPosLeft;
        }

        // Move
        //if (!m_rolling)
        //    m_body2d.velocity = new UnityEngine.Vector2(inputX * m_speed, m_body2d.velocity.y);

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        //// -- Handle Animations --
        ////Wall Slide
        //m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        //m_animator.SetBool("WallSlide", m_isWallSliding);



        //Death
        if (Input.GetKeyDown("e") && !m_rolling)
        {
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");
        }
        //Hurt
        else if (Input.GetKeyDown("q") && !m_rolling)
            m_animator.SetTrigger("Hurt");


        ////Attack
        //else if (Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling)
        //{
        //    m_currentAttack++;

        //    // Loop back to one after third attack
        //    if (m_currentAttack > 3)
        //        m_currentAttack = 1;

        //    // Reset Attack combo if time since last attack is too large
        //    if (m_timeSinceAttack > 1.0f)
        //        m_currentAttack = 1;

        //    // Call one of three attack animations "Attack1", "Attack2", "Attack3"
        //    m_animator.SetTrigger("Attack" + m_currentAttack);
        //    AttackEnable();
        //    Invoke("AttackDisable", 0.1f);

        //    // Reset timer
        //    m_timeSinceAttack = 0.0f;
        //}

        //// Block
        //else if (Input.GetMouseButtonDown(1) && !m_rolling)
        //{
        //    m_animator.SetTrigger("Block");
        //    m_animator.SetBool("IdleBlock", true);
        //}

        //else if (Input.GetMouseButtonUp(1))
        //    m_animator.SetBool("IdleBlock", false);

        // Roll
        //else if (Input.GetKeyDown("left shift") && !m_rolling && !m_isWallSliding)
        //{
        //    m_rolling = true;
        //    m_animator.SetTrigger("Roll");
        //    m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
        //}


        //Jump
        //else if (Input.GetKeyDown("space") && m_grounded && !m_rolling)
        //{
        //    m_animator.SetTrigger("Jump");
        //    m_grounded = false;
        //    m_animator.SetBool("Grounded", m_grounded);
        //    m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
        //    m_groundSensor.Disable(0.2f);
        //}

        //Run
        else if (Mathf.Abs(moveDir) > Mathf.Epsilon)
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

    // damage taken interface
    public void Damage(float damageAmount)
    {
        print("controller Damaged " + playerState);
        switch (playerState)
        {
            case EplayerState.standing:
                TakeDamage(damageAmount);
                break;
            case EplayerState.moving:
                TakeDamage(damageAmount);
                break;
            case EplayerState.attacking:
                print("clash");
                break;
            case EplayerState.blocking:
                TakeDamage(damageAmount * blockMultiplier);
                break;
        }
    }

    private void TakeDamage(float damageAmount)
    {
        if (health - damageAmount > 0)
        {
            health -= damageAmount;
            m_healthBar.UpdateResourceBar(health, maxHealth);
            print("Damage Taken: " + damageAmount + " Health: " + health);
        }
        else
            health = 0;
            m_healthBar.UpdateResourceBar(health, maxHealth);
            print("Player " + this.gameObject.name + " is Dead");
            
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
