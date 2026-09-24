using UnityEngine;

public class DelayBomb : MonoBehaviour
{
    public GameObject explosionPrefab;
    public float delay = 3f;//着弾まで何秒待つか
    public int damage = 10;//爆発のダメージ

    void Start()
    {
        Invoke(nameof(Explode), delay);//delay秒後にExplodeを呼ぶ
    }

    void Explode()
    {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);//Explosionを生成
        Explosion ex = explosion.GetComponent<Explosion>();//Explosion.csを取得
        ex.damage = damage;//爆発ダメージをexに渡している
        Destroy(gameObject);//必要なくなるので削除
    }
}