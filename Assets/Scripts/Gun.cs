using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Gun : MonoBehaviour {
    public RectTransform rectT;
    public Transform raycastT;
    public Image[] smokeImages;
    public LayerMask hitLayerMask;
    public Sprite[] splatSprites;
    public AudioClip shootSound;
    public AudioClip equipSound;
    public AudioClip unequipSound;
    public GameObject switchWeaponText;

    private static int defaultLayer;
    private static int enemyLayer;
    private void Awake() {
        defaultLayer = LayerMask.NameToLayer("Default");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    private const float RAYCAST_DISTANCE = 200f;
    public void Shoot() {
        AudioManager.Instance.PlaySFXPitched(shootSound, 1, Random.Range(0.90f, 1.1f));

        Kickback();
        if (Physics.Raycast(raycastT.position, raycastT.forward, out RaycastHit hit, RAYCAST_DISTANCE, hitLayerMask)) {
            if (hit.transform.gameObject.layer == enemyLayer) {
                if(hit.transform.TryGetComponent(out Enemy enemy)) {
                    enemy.Hurt(StatsManager.instance.PistolDamage);
                } else if (hit.transform.TryGetComponent(out ExplodingEnemy explodingEnemy)) {
                    explodingEnemy.Hurt(StatsManager.instance.PistolDamage);
                }
            } else {
                CreateSplat(hit);
            }
        }
    }

    private const float AUTO_WAIT = 0.25f;
    private float lastShootTime;
    private float timeHeld = 0;
    public void Hold() {
        if (StatsManager.instance.pistolFullAuto) {
            timeHeld += Time.deltaTime;
            if (timeHeld > AUTO_WAIT) {
                if (Time.time - lastShootTime > 0.08f) {
                    lastShootTime = Time.time;
                    Shoot();
                }
            }
        }
    }

    public void EndHold() {
        timeHeld = 0;
    }

    private Coroutine kickbackRoutine;
    private Coroutine kickbackSubroutine;
    private const float KICK_TIME = 0.2f;
    private const float RETURN_TIME = 0.3f;
    private void Kickback() {
        StopKick();
        for (int i = 0; i < smokeImages.Length; i++) {
            smokeImages[i].gameObject.SetActive(false);
        }
        kickbackRoutine = StartCoroutine(KickbackRoutine());

        IEnumerator KickbackRoutine() {
            Vector2 startPos = rectT.anchoredPosition;
            Vector2 dirToEquip = equippedPos - startPos;
            Vector2 endPos = startPos + shootVector + (dirToEquip/5f);
            float startTime = Time.time;
            kickbackSubroutine = this.CreateAnimationRoutine(KICK_TIME, (float progress) => {
                rectT.anchoredPosition = Vector2.Lerp(startPos, endPos, Easing.easeOutQuad(0, 1, progress));
            });
            Image smokeImage = smokeImages[Random.Range(0, smokeImages.Length)];
            smokeImage.gameObject.SetActive(true);
            yield return WaitUtil.GetWait(0.05f);
            smokeImage.gameObject.SetActive(false);
            while (Time.time - startTime < KICK_TIME) {
                yield return null;
            }

            startPos = rectT.anchoredPosition;
            kickbackSubroutine = this.CreateAnimationRoutine(RETURN_TIME, (float progress) => {
                rectT.anchoredPosition = Vector2.Lerp(startPos, equippedPos, Easing.easeOutSine(0, 1, progress));
            });
        }
    }

    private void StopKick() {
        this.EnsureCoroutineStopped(ref kickbackRoutine);
        this.EnsureCoroutineStopped(ref kickbackSubroutine);
    }

    private void CreateSplat(RaycastHit hit) {
        GameObject newSplat = new GameObject("Splat");
        Transform splatT = newSplat.transform;
        Vector3 pos = hit.point + (hit.normal * 0.02f);
        splatT.position = pos;
        splatT.rotation = Quaternion.LookRotation(hit.normal);
        splatT.RotateAround(pos, splatT.forward, Random.Range(0f, 360f));
        splatT.localScale = new Vector3(1.35f, 1.35f, 1.35f);
        SpriteRenderer splatRenderer = newSplat.AddComponent<SpriteRenderer>();
        splatRenderer.sprite = splatSprites[Random.Range(0, splatSprites.Length)];
        splatRenderer.sortingOrder = 1;
        newSplat.layer = defaultLayer;

        Vector3 startScale = splatT.localScale;
        Vector3 endScale = Vector3.zero;
        Player.instance.CreateAnimationRoutine(7f, (float progress) => {
            splatT.localScale = Vector3.Lerp(startScale, endScale, progress);
        }, () => {
            Destroy(newSplat);
        });
    }

    private Coroutine equipRoutine;
    private static readonly Vector2 equippedPos = new Vector2(575f, -320f);
    private static readonly Vector2 unequippedPos = new Vector2(575f, -800f);
    private const float SHOOT_MAGNITUDE = 30;
    private static readonly Vector2 shootVector = new Vector2(0.766f, -0.6427876f) * SHOOT_MAGNITUDE;
    public const float MOVE_ANIM_TIME = 0.5f;
    public void SetEquipped(bool equipped) {
        StopKick();
        if(equipped) {
            gameObject.SetActive(true);
        }
        AudioManager.Instance.PlaySFX(equipped ? equipSound : unequipSound, 1);

        this.EnsureCoroutineStopped(ref equipRoutine);
        Vector2 startPos = rectT.anchoredPosition;
        Vector2 endPos = equipped ? equippedPos : unequippedPos; 
        equipRoutine = this.CreateAnimationRoutine(MOVE_ANIM_TIME, (float progress) => {
            rectT.anchoredPosition = Vector2.Lerp(startPos, endPos, Easing.easeInOutSine(0, 1, progress));
        }, () => {
            if(!equipped) {
                gameObject.SetActive(false);
            }
        });
    }

    public void SetSwitchWeaponTextActive(bool active) {
        switchWeaponText.SetActive(active);
    }
}
