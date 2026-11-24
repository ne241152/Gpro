using UnityEngine;
using UnityEngine.UI; 
using TMPro;


public class WizardPlayerController : MonoBehaviour
{
    public bool hasPasskey = false;
    [Header("移動・アニメーション設定")]
    public float moveSpeed = 3.3f;
    public bool invertMovement = false;
    [Header("スマホ操作")]
    public MobileJoystick mobileJoystick;
    public Sprite front;
    public Sprite[] walkLeft;
    public Sprite[] walkRight;
    public float animationSpeed = 0.15f;

    [Header("ステータス設定")]
    public int maxHp = 100; 
    public int hp = 100;
    public int currentExp = 0;
    public int expToNextLevel = 40;
    public int currentLevel = 1;
    public int BattlePhase = 1;
    public float Level3AcquiredTime { get; private set; } = -1f;

    public void MarkLevel3Acquired()
    {
        if (Level3AcquiredTime < 0f) Level3AcquiredTime = Time.time;
    }
    [Range(0, 100)] public int passkeyExperienceReductionPercent = 40;
    private int experienceRemainder;

    [Header("UI割り当て")]
    public TextMeshProUGUI hpText;
    public Transform hpFillTransform; 
    public Slider expSlider; 
    public UIManager uiManager; 

    [Header("武器プレハブ")]
    public GameObject bulletPrefab;
    public GameObject dronePrefab;
    public GameObject delayBombPrefab;
    public Texture2D riskScanTexture;
    public Texture2D riskScanResultTexture;

    public float attackPower = 3f;
    public float gatlingInterval = 0.45f;
    public float damageRate = 1.0f;
    public float invincibleTime = 3f;
    [Min(0f)] public float hitInvincibilityDuration = 0.3f;
    
    private float burstTimer = 0f;
    private float invincibleTimer = 0f;
    private float hitInvincibilityUntil;

    private bool hasBurst = false;
    private bool isInvincible = false;
    public bool hasRevive = false;

    private GameObject currentDrone;
    private GameObject currentSmsCannon;
    private SpectralCompanion currentCompanion;
    private RiskBasedScanner riskScanner;
    public float EffectiveMoveSpeed => moveSpeed * (riskScanner != null ? riskScanner.MoveMultiplier : 1f);
    public float RiskAttackMultiplier => riskScanner != null ? riskScanner.AttackMultiplier : 1f;
    public float EffectiveAttackPower => attackPower * RiskAttackMultiplier;
    public float EffectiveDamageRate => damageRate * (riskScanner != null ? riskScanner.DamageMultiplier : 1f);
    public bool HasDrone => currentDrone != null;
    public bool HasSmsCannon => currentSmsCannon != null;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private float animTimer;
    private int frameIndex;
    private Sprite[] lastWalkDirection;
    private bool facingLeft;

    // ★追加：最初のゲージの大きさを記憶しておく変数
    private Vector3 initialHpFillScale;
    private Vector3 initialHpFillPosition;

    void Awake()
    {
        if (mobileJoystick == null)
{
    mobileJoystick = FindFirstObjectByType<MobileJoystick>();
}
        // プレイヤーの画像だけを取得する
        Transform wizardSprite = transform.Find("WizardSprite");
        spriteRenderer = null;

        if (wizardSprite != null)
        {
            spriteRenderer = wizardSprite.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogError("WizardSpriteのSpriteRendererが取得できません");
        }
        else
        {
            Debug.Log("取得成功: " + spriteRenderer.gameObject.name);
        }
        rb = GetComponent<Rigidbody2D>();
        lastWalkDirection = walkRight;

        // 自動で HPFill を探す
if (hpFillTransform != null)
{
    float hpRatio = Mathf.Clamp01((float)hp / maxHp);

    hpFillTransform.localScale = new Vector3(
        initialHpFillScale.x * hpRatio,
        initialHpFillScale.y,
        initialHpFillScale.z
    );

    float lostWidth = initialHpFillScale.x * (1f - hpRatio);

    hpFillTransform.localPosition = new Vector3(
        initialHpFillPosition.x - lostWidth / 2f,
        initialHpFillPosition.y,
        initialHpFillPosition.z
    );
}

        // ★ゲーム開始直後の「本来のサイズ」を記憶する
        if (hpFillTransform != null)
        {
            initialHpFillScale = hpFillTransform.localScale;
            initialHpFillPosition = hpFillTransform.localPosition;
        }
    }

    void Start() 
    { 
        UpdateHPText();
        UpdateExpBar();
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

if (mobileJoystick != null && mobileJoystick.IsActive)
{
    // スマホ操作
    moveInput = mobileJoystick.Direction;
}
else
{
    // 従来のPC操作
    moveInput.x = Input.GetAxisRaw("Horizontal");
    moveInput.y = Input.GetAxisRaw("Vertical");
    moveInput = moveInput.normalized;
}

// 宇宙人だけ移動方向を完全に反転
if (invertMovement)
{
    moveInput *= -1f;
}

        UpdateSprite();

        // 通常攻撃は BasicAttack（例: BulletBasicAttack）側で行う

        if (hasBurst) {
            burstTimer += Time.deltaTime;
            if (burstTimer >= 3.0f) {
                FireBurst();
                burstTimer = 0f;
            }
        }

        if (isInvincible){
            invincibleTimer += Time.deltaTime;
            if (invincibleTimer >= invincibleTime){
                isInvincible = false;
                invincibleTimer = 0f;
            }
        }


    }

    void FixedUpdate()
    {
        if (rb == null) return;

        rb.MovePosition(
            rb.position + moveInput * EffectiveMoveSpeed * Time.fixedDeltaTime
        );
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        // 左歩行画像が無いキャラは、右歩行画像を左右反転して使う
        bool useFlipForLeft = (walkLeft == null || walkLeft.Length == 0);

        // キャラ選択で歩行画像が差し替えられた場合、古い画像を使い続けないようにする
        if (lastWalkDirection != walkLeft && lastWalkDirection != walkRight)
        {
            lastWalkDirection = walkRight;
        }

        if (moveInput.x < 0)
        {
            lastWalkDirection = useFlipForLeft ? walkRight : walkLeft;
            facingLeft = true;
        }
        else if (moveInput.x > 0)
        {
            lastWalkDirection = walkRight;
            facingLeft = false;
        }

        if (moveInput == Vector2.zero)
        {
            spriteRenderer.sprite = front;
            spriteRenderer.flipX = false;
            frameIndex = 0;
            animTimer = 0f;
            return;
        }

        Sprite[] currentWalk = lastWalkDirection;
        if (currentWalk == null || currentWalk.Length == 0)
        {
            spriteRenderer.sprite = front;
            spriteRenderer.flipX = false;
            return;
        }

        if (frameIndex >= currentWalk.Length) frameIndex = 0;
        spriteRenderer.flipX = useFlipForLeft && facingLeft;

        animTimer += Time.deltaTime;
        if (animTimer >= animationSpeed)
        {
            animTimer = 0f;
            frameIndex = (frameIndex + 1) % currentWalk.Length;
        }

        spriteRenderer.sprite = currentWalk[frameIndex];
    }

    void FireBurst()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 3.0f);
        foreach (var c in cols) {
            if (c.CompareTag("Enemy")) {
                EnemyHealth enemy = c.GetComponent<EnemyHealth>();
                if (enemy != null) {
                    enemy.TakeDamage(2f * RiskAttackMultiplier);
                    Vector2 kbDir = (c.transform.position - transform.position).normalized;
                    enemy.transform.position += (Vector3)kbDir * 1.5f;
                }
            }
        }
    }

    public void AddExp(int amount)
    {
        if (BattlePhase > 4)
        {
            UpdateExpBar();
            return;
        }

        if (amount <= 0) return;
        if (hasPasskey)
        {
            // 整数の百分率で端数を保持し、5個なら正確に3経験値にする。
            int scaledExperience = amount * (100 - passkeyExperienceReductionPercent) + experienceRemainder;
            amount = scaledExperience / 100;
            experienceRemainder = scaledExperience % 100;
        }
        currentExp += amount;
        if (currentExp >= expToNextLevel) {
            currentExp -= expToNextLevel;
            currentLevel++;
            if (currentLevel == 2) expToNextLevel = 60;
            else if (currentLevel == 3) expToNextLevel = 60;
            else expToNextLevel += 5;
            
            if (uiManager != null) {
                uiManager.ShowLevelUp(BattlePhase);
            } else {
                Time.timeScale = 0;
            }
        }
        UpdateExpBar();
    }

    public void UpdateExpBar()
    {
        if (BattlePhase > 4) currentExp = expToNextLevel;
        if (expSlider != null)
        {
            expSlider.maxValue = expToNextLevel;
            expSlider.value = currentExp;
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isInvincible || Time.time < hitInvincibilityUntil) return;

        dmg = Mathf.RoundToInt(dmg * EffectiveDamageRate);
        if (dmg <= 0) return;

        hitInvincibilityUntil = Time.time + hitInvincibilityDuration;
        hp -= dmg;
        if (hp <= 0) {
            if (hasRevive){
                hasRevive = false;
                hp = maxHp / 2;
                isInvincible = true;
                invincibleTimer = 0f;
            }else{
                hp = 0;
                if (uiManager != null) {
                    uiManager.ShowResult(false);
                } else {
                    Time.timeScale = 0;
                }
            }
        }
        UpdateHPText();
    }

    void UpdateHPText()
    {
        if (hpText != null) {
            hpText.text = "HP: " + hp.ToString();
        }

        // ★強制的に「1」にするのではなく、「最初の大きさ × HPの割合」で計算する
        if (hpFillTransform != null) {
            float hpRatio = (float)hp / maxHp;
            hpFillTransform.localScale = new Vector3(initialHpFillScale.x * hpRatio, initialHpFillScale.y, initialHpFillScale.z);
        }
    }

    public void IncreaseHP(int amount) 
    { 
        hp += amount; 
        if (hp > maxHp) hp = maxHp; 
        UpdateHPText(); 
    }
    
    public void IncreaseAttackPower(int amount) { attackPower += amount; }
    public void IncreaseAttackPowerPercent(float percent) { attackPower *= 1f + percent / 100f; }
    public void DecreaseAttackPower(int amount) { attackPower -= amount; if (attackPower < 1) attackPower = 1; }
    public void IncreaseMaxHP(int amount)
    {
        maxHp += amount;
        hp += amount;
        UpdateHPText();
    }
    public void IncreaseDefense(float percent) { damageRate *= (100f - percent) / 100f; }
    public void IncreaseMoveSpeed(float percent) { moveSpeed *= (100f + percent) / 100f; }
    public void DecreaseMoveSpeed(float percent) { moveSpeed *= (100f - percent) / 100f; }
    public void EnableRevive() { hasRevive = true; }
    public void EnablePasskey() { hasPasskey = true; }
    public void EnableDelayBomb()
    {
        if (delayBombPrefab == null) {
            Debug.LogError("SMSキャノン: Delay Bomb Prefabが未設定です。", this);
            return;
        }
        if (currentSmsCannon != null) return;
        currentSmsCannon = SpawnSmsCannonFor(transform, 1f);
    }

    public GameObject SpawnSmsCannonFor(Transform owner, float opacity, float damageMultiplier = 1f)
    {
        if (delayBombPrefab == null) return null;
        GameObject cannon = Instantiate(delayBombPrefab, owner.position, Quaternion.identity);
        DelayBomb weapon = cannon.GetComponent<DelayBomb>();
        weapon.visualOpacity = opacity;
        weapon.damageMultiplier = damageMultiplier;
        weapon.attackSource = this;
        weapon.Initialize(owner);
        return cannon;
    }

    void OnDestroy()
    {
        if (currentSmsCannon != null) Destroy(currentSmsCannon);
        if (currentDrone != null) Destroy(currentDrone);
        if (currentCompanion != null) Destroy(currentCompanion.gameObject);
    }
    public void IncreaseAttackSpeed(float percent) { gatlingInterval /= 1f + percent / 100f; }
    public void DecreaseAttackSpeed(float percent) { gatlingInterval *= (100f + percent) / 100f; }

    public void CreateDrone()
    {
        if (bulletPrefab == null || dronePrefab == null) return;
        if (currentDrone != null) return;
        currentDrone = SpawnDroneFor(transform, 1f);
    }

    public GameObject SpawnDroneFor(Transform owner, float opacity, float damageMultiplier = 1f)
    {
        if (dronePrefab == null) return null;
        GameObject instance = Instantiate(dronePrefab, owner.position, Quaternion.identity);
        DroneWeapon drone = instance.GetComponent<DroneWeapon>();
        drone.player = owner;
        drone.visualOpacity = opacity;
        drone.damageMultiplier = damageMultiplier;
        drone.attackSource = this;
        if (drone.bulletPrefab == null) drone.bulletPrefab = bulletPrefab;
        SpectralAppearance.Apply(instance, opacity);
        return instance;
    }

    public void CreateCompanion()
    {
        if (currentCompanion != null) return;
        GameObject companion = new GameObject("SynchroCompanion");
        currentCompanion = companion.AddComponent<SpectralCompanion>();
        currentCompanion.Initialize(this);
    }

    public void EnableRiskScanner()
    {
        if (riskScanner != null) return;
        riskScanner = gameObject.AddComponent<RiskBasedScanner>();
        riskScanner.Initialize(this, riskScanTexture, riskScanResultTexture);
    }
}
