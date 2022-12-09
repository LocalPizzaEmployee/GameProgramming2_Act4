using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class Entity : MonoBehaviour, IHealthDamageHandler, ISkillHandler
{
    /// <summary>
    /// To initialize entity by values given by EntityInfo
    /// </summary>
    /// <param name="info"></param>
    /// <param name="isPlayer"></param>
    public void InitializeEntity(EntityInfo info, bool isPlayer = false)
    {
        Type = info.Type;
        IsPlayer = isPlayer;
        _hp = info.MaxHealth;
        MaxHealth = info.MaxHealth;
        AttackDamage = info.AttackDamage;
        AttackRange = info.AttackRange;
        AttackRate = info.AttackRate;
        Skills = info.Skills;
        _spriteRenderer.sprite = info.Sprite;
        _moveSpeed = info.MoveSpeed;
        _basicProjectile = info.BasicProjectileInfo;
    }

    public EntityType Type { get; private set; }
    public bool IsPlayer { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    //TODO return to private
    protected int _hp;

    public int CurrentHealth
    {
        get => _hp;
        private set => _hp = Mathf.Clamp(value, 0, MaxHealth);
    }
    public int MaxHealth { get; protected set;}
    public int AttackDamage { get; protected set;}
    public float AttackRange { get; protected set;}
    public float AttackRate { get; protected set;}
    private float _nextAttackTime;
    public Skill ActiveSkill { get; protected set; }
    private float _nextActiveSkillTime;
    public Skill[] Skills { get; protected set; }

    private TargetIndicator _indicator;
    //TODO add projectile to object pooling
    private ProjectileInfo _basicProjectile;
    protected Rigidbody2D _rigidbody;
    protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected float _moveSpeed;
    [SerializeField] protected Vector2 _faceDirection;

    public virtual void Apply(ApplyType type, IHealthDamageHandler agent)
    {
        if (!IsAlive) return;

        switch (type)
        {
            case ApplyType.PrimaryDamage:
                CurrentHealth -= agent.AttackDamage;
                Debug.Log($"Agent Damage {agent.AttackDamage}");
                Debug.Log($"Current HP {CurrentHealth}");
                break;
            case ApplyType.SkillDamage:
                CurrentHealth -= agent.ActiveSkill.Damage;
                break;
        }
        
        if(!IsAlive)
            OnDie(this);
    }

    public virtual void OnDie(IHealthDamageHandler agent)
    {
        
        switch (agent.Type)
        {
            case EntityType.Dwarf:
            case EntityType.Wizard:
            case EntityType.Monk:
                _rigidbody.velocity = Vector2.zero;
                this.TryGetComponent(out Collider2D col);
                col.isTrigger = true;
                _spriteRenderer.enabled = false;
                Invoke(nameof(RestartScene), 2f);
                return;
        }
        //TODO dead state for enemy AI or going back to object pool
        gameObject.SetActive(false);
        // Destroy(this.gameObject);
    }

    private void RestartScene()
    {
        Debug.Log($"Restart Scene");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public virtual void AutoAttack()
    {
        switch (Type)
        {
            case EntityType.Dwarf:
            case EntityType.Wizard:
            case EntityType.Monk:
                if (Time.time > _nextAttackTime)
                {
                    var tr = _indicator.OffsetTransform.transform;
                    var projectile = ObjectPoolManager.Instance.GetPoolObject<Projectile>(PoolType.BasicProjectile);
                    projectile.InitializeProjectile(_basicProjectile, _faceDirection);
                    var projTransform = projectile.transform;
                    projTransform.position = tr.position;
                    projTransform.rotation = _indicator.transform.rotation;
                    _nextAttackTime = Time.time + 1 / AttackRate;
                }
                break;
        }
    }

    public virtual void CastSkill(SkillType type)
    {
        ActiveSkill = Skills.FirstOrDefault(s => s.Type == type);
        if (ActiveSkill == null)
            return;
        if (Time.time <= _nextActiveSkillTime) 
            return;
        
        switch (type)
        {
            case SkillType.AxeNova:
                var axeNova = ObjectPoolManager.Instance.GetPoolObject<Projectile>(PoolType.AxeNovaProjectile);
                axeNova.InitializeProjectile(ActiveSkill.ProjectileInfo, Vector2.one);
                axeNova.transform.position = transform.position;
                break;
            case SkillType.Teleport:
                var newPosition = (Vector2)transform.position + (_faceDirection * ActiveSkill.Range);
                transform.position = newPosition;
                break;
            case SkillType.MultiShot:
                var count = (int)ActiveSkill.Range;
                var multiShot = ObjectPoolManager.Instance.GetPoolObject<Projectile>(PoolType.MultiShotProjectile);
                var multiShotTransform = multiShot.transform;
                multiShotTransform.position = _indicator.OffsetTransform.position;
                multiShotTransform.rotation = _indicator.transform.rotation;
                multiShot.InitializeProjectile(ActiveSkill.ProjectileInfo, _faceDirection);
                break;
        }

        _nextActiveSkillTime = Time.time + ActiveSkill.Cooldown;
    }

    protected virtual  void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        _indicator = this.transform.GetComponentInChildren<TargetIndicator>();
    }

    protected virtual void Update()
    {
        UpdateEntity();
        AutoAttack();
    }
    
    /// <summary>
    /// Will call this function every frame
    /// </summary>
    protected abstract void UpdateEntity();
}

public enum EntityType
{
    Unassigned,
    //Playable Characters
    Dwarf = 1,
    Wizard,
    Monk,
    
    //Enemy
    BigGoblin = 100,
    FastGoblin,
    NormalGoblin,
    
    SampleTarget = 999,
}