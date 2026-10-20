using UnityEngine;

public class LinkBarrierController : MonoBehaviour
{
    private BarrierPiece[] barrierPieces;
    private bool isBroken = false;

    private void Awake()
    {
        barrierPieces = GetComponentsInChildren<BarrierPiece>(true);

        foreach (BarrierPiece piece in barrierPieces)
        {
            piece.Initialize(this);
        }
    }

    // ゲーム開始時などに呼び出してバリアを無効化
    public void DeactivateBarrier()
    {
        gameObject.SetActive(false);
    }

    // カード選択時にバリアを有効化
    public void ActivateBarrier()
    {
        if (isBroken) return;

        gameObject.SetActive(true);
        Debug.Log("連携バリアが有効になりました");
    }

    public void BreakAll()
    {
        if (isBroken) return;

        isBroken = true;
        Debug.Log("連携バリアがすべて破壊されました");

        Destroy(gameObject);
    }
}