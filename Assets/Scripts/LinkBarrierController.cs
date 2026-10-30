using UnityEngine;
using System.Collections;

public class LinkBarrierController : MonoBehaviour
{
    private BarrierPiece[] barrierPieces;
    private bool isBroken = false;
    public Texture2D barrierTexture;
    public Vector2 centerOffset = new Vector2(1.5f, 1.5f);
    public float halfExtent = 9f;
    public bool IsBroken => isBroken;
    public bool IsProtecting => isActiveAndEnabled && !isBroken;

    private void Awake()
    {
        barrierPieces = GetComponentsInChildren<BarrierPiece>(true);

        foreach (BarrierPiece piece in barrierPieces)
        {
            piece.Initialize(this);
            switch (piece.name) {
                case "Barrier_up":
                    piece.transform.localPosition = centerOffset + Vector2.up * halfExtent;
                    piece.ConfigureVisual(barrierTexture, new Rect(220, 900, 890, 200), true); break;
                case "Barrier_down":
                    piece.transform.localPosition = centerOffset + Vector2.down * halfExtent;
                    piece.ConfigureVisual(barrierTexture, new Rect(220, 80, 890, 200), true); break;
                case "Barrier_left":
                    piece.transform.localPosition = centerOffset + Vector2.left * halfExtent;
                    piece.ConfigureVisual(barrierTexture, new Rect(75, 245, 200, 675), false); break;
                case "Barrier_right":
                    piece.transform.localPosition = centerOffset + Vector2.right * halfExtent;
                    piece.ConfigureVisual(barrierTexture, new Rect(1060, 245, 200, 675), false); break;
            }
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

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(0.08f);
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;
        float elapsed = 0f;
        while (elapsed < 0.2f) {
            float alpha = 1f - elapsed / 0.2f;
            for (int i = 0; i < renderers.Length; i++) {
                Color color = colors[i];
                color.a *= alpha;
                renderers[i].color = color;
            }
            yield return null;
            elapsed += Time.deltaTime;
        }
        Destroy(gameObject);
    }
}
