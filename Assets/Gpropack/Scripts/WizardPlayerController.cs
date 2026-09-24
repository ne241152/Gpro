using UnityEngine;
using UnityEngine.UI; 
using TMPro;


public class WizardPlayerController : MonoBehaviour
{
    public bool hasPasskey = false;
    [Header("移動・アニメーション設定")]
    public float moveSpeed = 3f;
    public Sprite front;
    public Sprite[] walkLeft;
    public Sprite[] walkRight;
    public float animationSpeed = 0.15f;

    [Header("ステータス設定")]
    public int maxHp = 100; 
    public int hp = 100;
    public int currentExp = 0;
    public int expToNextLevel = 10;
    public int currentLevel = 1;
    public int BattlePhase = 1;

    [Header("UI割り当て")]
    public TextMeshProUGUI hpText;
    public Transform hpFillTransform; 
    public Slider expSlider; 
    public UIManager uiManager; 

    [Header("武器プレハブ")]
    public GameObject bulletPrefab;
    public GameObject dronePrefab;
    public GameObject delayBombPrefab;

    public int attackPower = 1;
    public float gatlingInterval = 0.5f;
    public float delayBombInterval = 8f;
    public float damageRate = 1.0f;
    public float invincibleTime = 3f;
    
    private float gatlingTimer = 0f;
    private float burstTimer = 0f;
    private float delayBombTimer = 0f;
    private float invincibleTimer = 0f;

    private bool hasBurst = false;
    private bool hasDelayBomb = false;
    private bool isInvincible = false;
    public bool hasRevive = false;

    private GameObject currentDrone;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private float animTimer;
    private int frameIndex;
    private Sprite[] lastWalkDirection;

    // ★追加：最初のゲージの大きさを記憶しておく変数
    private Vector3 initialHpFillScale;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lastWalkDirection = walkRight;

        // 自動で HPFill を探す
        if (hpFillTransform == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name == "HPFill")
                {
                    hpFillTransform = child;
                    break;
                }
            }
        }

        // ★ゲーム開始直後の「本来のサイズ」を記憶する
        if (hpFillTransform != null)
        {
            initialHpFillScale = hpFillTransform.localScale;
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

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
        transform.position += (Vector3)(moveInput * moveSpeed * Time.deltaTime);

        UpdateSprite();

        gatlingTimer += Time.deltaTime;
        if (gatlingTimer >= gatlingInterval) {
            FireGatling();
            gatlingTimer = 0f;
        }

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

        if (hasDelayBomb) {
            delayBombTimer += Time.deltaTime;
            if (delayBombTimer >= delayBombInterval) {
                FireDelayBomb();
                delayBombTimer = 0f;
            }
        }
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        if (moveInput.x < 0) lastWalkDirection = walkLeft;
        else if (moveInput.x > 0) lastWalkDirection = walkRight;

        if (moveInput == Vector2.zero)
        {
            spriteRenderer.sprite = front;
            frameIndex = 0;
            animTimer = 0f;
            return;
        }

        Sprite[] currentWalk = lastWalkDirection;
        if (currentWalk == null || currentWalk.Length == 0)
        {
            spriteRenderer.sprite = front;
            return;
        }

        animTimer += Time.deltaTime;
        if (animTimer >= animationSpeed)
        {
            animTimer = 0f;
            frameIndex = (frameIndex + 1) % currentWalk.Length;
        }

        spriteRenderer.sprite = currentWalk[frameIndex];
    }

    void FireGatling()
    {
        if (bulletPrefab == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var e in enemies) {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist) { minDist = dist; nearest = e; }
        }

        if (nearest != null) {
            GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            b.GetComponent<Bullet>().damage = attackPower;
            Vector2 dir = (nearest.transform.position - transform.position).normalized;
            b.GetComponent<Rigidbody2D>().linearVelocity = dir * 10f;
        }
    }

    void FireBurst()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 3.0f);
        foreach (var c in cols) {
            if (c.CompareTag("Enemy")) {
                EnemyHealth enemy = c.GetComponent<EnemyHealth>();
                if (enemy != null) {
                    enemy.TakeDamage(2);
                    Vector2 kbDir = (c.transform.position - transform.position).normalized;
                    enemy.transform.position += (Vector3)kbDir * 1.5f;
                }
            }
        }
    }

    void FireDelayBomb()
    {
        if (delayBombPrefab == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return;

        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist) { minDist = dist; nearest = e; }
        }
        if (nearest != null)
        {
            Instantiate(delayBombPrefab, nearest.transform.position, Quaternion.identity);
        }
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        if (currentExp >= expToNextLevel) {
            currentExp -= expToNextLevel;
            currentLevel++;

            if (currentLevel == 2) {
                // 2回目のレベルアップを防ぐため、必要経験値をカンストさせる(デモ用)
                expToNextLevel = 80; 
            } else {
                expToNextLevel += 5; 
            }
            
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
        if (expSlider != null)
        {
            expSlider.maxValue = expToNextLevel;
            expSlider.value = currentExp;
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isInvincible) return;

        dmg = Mathf.RoundToInt(dmg * damageRate);
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
    public void EnableDelayBomb() { hasDelayBomb = true; FireDelayBomb(); delayBombTimer = 0f; }
    public void IncreaseAttackSpeed(float percent) { gatlingInterval *= (100f - percent) / 100f; }
    public void DecreaseAttackSpeed(float percent) { gatlingInterval *= (100f + percent) / 100f; }

    public void CreateDrone()
    {
        if (bulletPrefab == null || dronePrefab == null) return;
        if (currentDrone != null) return;
        currentDrone = Instantiate(dronePrefab);
        DroneWeapon drone = currentDrone.GetComponent<DroneWeapon>();
        drone.player = transform;
        drone.bulletPrefab = bulletPrefab;
    }
}