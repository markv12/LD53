using UnityEngine;

public class Projectile : MonoBehaviour {
    public int damage;
    public Transform mainT;

    public SpriteRenderer mainRenderer;
    public Sprite[] loopSprites;

    private bool isDestroyed = false;
    public void Execute(Vector3 startPos, Vector3 endPos) {
        StartCoroutine(SpriteAnimUtil.SpriteLoopRoutine(mainRenderer, loopSprites, 12));

        this.CreatePausableAnimationRoutine(3f, (float progress) => {
            if (!isDestroyed && Player.instance != null) {
                Vector3 pos = Vector3.Lerp(startPos, endPos, progress);
                mainT.position = pos;

                Vector3 playerPos = Player.instance.transform.position;
                if ((pos - playerPos).sqrMagnitude < 2f) {
                    Player.instance.Hurt(damage);
                    Destroy(gameObject);
                    isDestroyed = true;
                }
            }
        }, () => {
            Destroy(gameObject);
        });
    }
}
