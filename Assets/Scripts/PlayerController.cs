using UnityEngine;
using UnityEngine.UI;//追加
using TMPro;

public class PlayerController : MonoBehaviour
{
    public float speed = 5.0f;
    public int hp = 100;
    public int currentExp = 0;
    public int expToNextLevel = 10;
    public int currentLevel = 1;
    public int BattlePhase = 1;

    public GameObject bulletPrefab;
    public GameObject dronePrefab;
    public TextMeshProUGUI hpText;
    public UIManager uiManager; 
    public Slider expSlider;//追加

    //攻撃系
    private bool hasBurst = false;
    public int attackPower = 1;
    private float gatlingTimer = 0f;
    private float burstTimer = 0f;
    public float gatlingInterval = 0.5f;
    private GameObject currentDrone;//現在いるドローンの保存
    private bool hasDelayBomb = false;
    private float delayBombTimer = 0f;
    public float delayBombInterval = 8f;//何秒ごとに撃つか
    public GameObject delayBombPrefab;

    //防御系
    public float damageRate = 1.0f;
    public bool hasRevive = false;//復活可能
    private bool isInvincible = false;//無敵
    private float invincibleTimer = 0f;//無敵経過時間
    public float invincibleTime = 3f;//無敵時間

    private Rigidbody2D rb;
    private Animator animator;

    void Start() 
    { 
        rb = GetComponent<Rigidbody2D>(); 
        animator = GetComponent<Animator>();
        UpdateHPText();
        UpdateExpBar();//追加
    }

    void Update()
    {
        // 画面が止まっている（ポーズ中）なら、プレイヤーの操作も完全に無効化する
        if (Time.timeScale == 0) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 moveDir = new Vector2(moveX, moveY).normalized;
        rb.linearVelocity = new Vector2(moveX, moveY).normalized * speed;

        // ★ここを追加：アニメーションへ値を送る
        animator.SetFloat("Speed", moveDir.magnitude);
    

        if (moveX != 0) {
            transform.localScale = new Vector3(Mathf.Sign(moveX), 1, 1);
        }

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

        if (hasDelayBomb)
        {
            delayBombTimer += Time.deltaTime;
            if (delayBombTimer >= delayBombInterval)
            {
                FireDelayBomb();
                delayBombTimer = 0f;
            }
        }

        
    }

    void FireGatling()
    {
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
                // ここを EnemyHealth に修正しました
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
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)//敵が周りにいるかを確認
            return;

        GameObject nearest = null;//一番近い敵を入れる用の箱
        float minDist = Mathf.Infinity;//距離を入れる

        foreach (GameObject e in enemies)
        {
            //enemiesとの距離を測っている
            float dist = Vector2.Distance(transform.position, e.transform.position);

            if (dist < minDist)//一番近いenemiesを探す
            {
                minDist = dist;
                nearest = e;
            }
        }
        if (nearest != null)
        {
            //PrefabのDelayBombをnearestに生成する
            Instantiate(delayBombPrefab, nearest.transform.position, Quaternion.identity);
        }
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        if (currentExp >= expToNextLevel) {
            currentExp -= expToNextLevel;
            expToNextLevel += 5;
            currentLevel++;
            
            if (uiManager != null) {
                uiManager.ShowLevelUp(BattlePhase);
            } else {
                Time.timeScale = 0;
            }
        }
        UpdateExpBar();
    }

    public void IncreaseHP(int amount)
    {
        hp += amount;
        UpdateHPText();
    }

    public void IncreaseAttackPower(int amount)
    {
        attackPower += amount;
    }

    public void DecreaseAttackPower(int amount)
    {
        attackPower -= amount;

        if (attackPower < 1)
        {
            attackPower = 1;
        }
    }

    public void IncreaseDefense(float percent)
    {
        damageRate *= (100f - percent) / 100f;
    }

    public void IncreaseMoveSpeed(float percent)
    {
        speed *= (100f + percent) / 100f;
    }

    public void DecreaseMoveSpeed(float percent)
    {
        speed *= (100f - percent) / 100f;
    }

    public void EnableRevive()
    {
        hasRevive = true;
    }

    public void CreateDrone()
    {
        if (currentDrone != null)
            return;
        currentDrone = Instantiate(dronePrefab);//DroneのPrefabを作成し、currentDroneに保存
        DroneWeapon drone = currentDrone.GetComponent<DroneWeapon>();//DroneWeaponを取得する
        drone.player = transform;//Droneをプレイヤーの周りに回らせる
        drone.bulletPrefab = bulletPrefab;
    }

    public void EnableDelayBomb()
    {
        hasDelayBomb = true;
        FireDelayBomb();// 最初の1発をすぐ発射
        delayBombTimer = 0f;// 次の発射まで8秒
    }

    public void IncreaseAttackSpeed(float percent)
    {
        gatlingInterval *= (100f - percent) / 100f;
    }

    public void DecreaseAttackSpeed(float percent)
    {
        gatlingInterval *= (100f + percent) / 100f;
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
        if (isInvincible)return;

        dmg = Mathf.RoundToInt(dmg * damageRate);
        hp -= dmg;
        if (hp <= 0) {
            if (hasRevive){
                hasRevive = false;// 一度だけ
                hp = 50;// 復活時のHP
                isInvincible = true;// 無敵開始
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
    }
}