using UnityEngine;

public class BarrierPiece : MonoBehaviour
{
    [Header("バリア設定")]
    public int durability = 5;

    private LinkBarrierController parentBarrier;
    private bool isBroken = false;

    public void Initialize(LinkBarrierController parent)
    {
        parentBarrier = parent;
    }

    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[1] 接触検知: {collision.gameObject.name}, Tag={collision.gameObject.tag}");

        if (isBroken)
        {
            Debug.Log("[2] isBroken が true のため処理を終了");
            return;
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("[3] Enemy判定を通過");

            durability--;
            Debug.Log($"[4] {gameObject.name} の耐久力: {durability}");

            if (durability <= 0)
            {
                isBroken = true;

                if (parentBarrier != null)
                {
                    parentBarrier.BreakAll();
                }
                else
                {
                    Debug.LogError("parentBarrier が設定されていません");
                }
            }
        }
        else
        {
            Debug.Log("[5] Enemy判定に失敗");
        }
    }
}