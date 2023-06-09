using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeavyGun : MonoBehaviour {
    public RectTransform rectT;
    public Transform raycastT;
    public Image gunBodyImage;
    public Sprite[] reloadFrames;
    public GameObject flagDown;
    public GameObject flagUp;
    public Image[] smokeImages;
    public LayerMask hitLayerMask;
    public Sprite[] splatSprites;
    public AudioClip shootSound;
    public AudioClip equipSound;
    public AudioClip unequipSound;
    public AudioClip reloadFinishSound;

    private static int defaultLayer;
    private static int enemyLayer;
    private void Awake() {
        defaultLayer = LayerMask.NameToLayer("Default");
        enemyLayer = LayerMask.NameToLayer("Enemy");
        CanShoot = true;
    }

    private const float RAYCAST_DISTANCE = 50f;
    private int reloadFrameIndex = 0;
    private readonly RaycastHit[] hits = new RaycastHit[15];
    public void Shoot() {
        AudioManager.Instance.PlaySFXPitched(shootSound, 1, Random.Range(0.90f, 1.1f));

        Kickback();
        
        int hitCount = Physics.SphereCastNonAlloc(new Ray(raycastT.position, raycastT.forward), 2, hits, RAYCAST_DISTANCE, hitLayerMask);
        for (int i = 0; i < hitCount; i++) {
            RaycastHit hit = hits[i];
            if (hit.transform.gameObject.layer == enemyLayer) {
                if (hit.transform.TryGetComponent(out Enemy enemy)) {
                    enemy.Hurt(3000);
                } else if (hit.transform.TryGetComponent(out ExplodingEnemy explodingEnemy)) {
                    explodingEnemy.Hurt(3000);
                }
            } else {
                CreateSplat(hit);
            }
        }

        Player.instance.StartCoroutine(ReloadRoutine());

        IEnumerator ReloadRoutine() {
            CanShoot = false;
            yield return WaitUtil.GetWait(StatsManager.instance.ShotgunReloadTime);
            if (DayNightManager.instance.IsNight) {
                AudioManager.Instance.PlaySFX(reloadFinishSound, 1);
            }
            reloadFrameIndex = (reloadFrameIndex + 1) % reloadFrames.Length;
            gunBodyImage.sprite = reloadFrames[reloadFrameIndex];
            CanShoot = true;
        }
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
            yield return WaitUtil.GetWait(0.075f);
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
    private static readonly Vector2 equippedPos = new Vector2(577f, -308f);
    private static readonly Vector2 unequippedPos = new Vector2(577f, -800f);
    private const float SHOOT_MAGNITUDE = 50;
    private static readonly Vector2 shootVector = new Vector2(0.766f, -0.6427876f) * SHOOT_MAGNITUDE;

    private bool canShoot;
    public bool CanShoot {
        get {
            return canShoot;
        }
        private set {
            canShoot = value;
            flagDown.SetActive(!canShoot);
            flagUp.SetActive(canShoot);
        }
    }

    public void SetEquipped(bool equipped) {
        StopKick();
        if(equipped) {
            gameObject.SetActive(true);
        }
        AudioManager.Instance.PlaySFX(equipped ? equipSound : unequipSound, 1);

        this.EnsureCoroutineStopped(ref equipRoutine);
        Vector2 startPos = rectT.anchoredPosition;
        Vector2 endPos = equipped ? equippedPos : unequippedPos; 
        equipRoutine = this.CreateAnimationRoutine(Gun.MOVE_ANIM_TIME, (float progress) => {
            rectT.anchoredPosition = Vector2.Lerp(startPos, endPos, Easing.easeInOutSine(0, 1, progress));
        }, () => {
            if(!equipped) {
                gameObject.SetActive(false);
            }
        });
    }
}
