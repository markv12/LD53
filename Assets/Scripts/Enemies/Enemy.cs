using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.FirstPerson;

public class Enemy : MonoBehaviour {
    [Min(0)]
    public int maxHealth;
    public Transform spriteT;
    public SpriteRenderer mainRenderer;

    public Sprite normalSprite;
    public Sprite attackSprite;
    public Sprite deathSprite;

    public AudioClip deathSound;

    public NavMeshAgent navMeshAgent;

    private int health;
    private void Awake() {
        health = maxHealth;
    }

    bool isDestroyed = false;
    private void Update() {
        if (!isDestroyed) {
            if (Player.instance != null && navMeshAgent.enabled) {
                Vector3 playerPos = Player.instance.transform.position;
                navMeshAgent.destination = playerPos;
                if ((spriteT.position - playerPos).sqrMagnitude < 2f) {
                    Player.instance.Hurt(33);
                    Destroy(gameObject);
                    isDestroyed = true;
                }
            }
            if (!DayNightManager.instance.IsNight) {
                Destroy(gameObject);
                isDestroyed = true;
            }
        }

    }

    private bool died = false;
    public void Hurt(int amount) {
        if (!died) {
            health -= amount;
            FlashColor(Color.red);
            if (health <= 0) {
                Die();
            }
        }
    }

    private void Die() {
        died = true;
        navMeshAgent.enabled = false;
        mainRenderer.sprite = deathSprite;
        AudioManager.Instance.PlaySFX(deathSound, 1f);
        Vector3 startScale = spriteT.localScale;
        Vector3 endScale = Vector3.zero;
        this.CreateAnimationRoutine(0.8f, (float progress) => {
            spriteT.localScale = Vector3.Lerp(startScale, endScale, Easing.easeInSine(0, 1, progress));
        }, () => {
            Destroy(gameObject);
        });
    }

    private const float FLASH_DURATION = 0.05f;
    private static readonly WaitForSeconds flashWait = new WaitForSeconds(FLASH_DURATION);
    private Coroutine flashRoutine;
    private void FlashColor(Color color) {
        this.EnsureCoroutineStopped(ref flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
        IEnumerator FlashRoutine() {
            Vector3 startScale = spriteT.localScale;
            Vector3 flashScale = startScale *= 0.92f;
            mainRenderer.color = color;
            spriteT.localScale = flashScale;
            yield return flashWait;
            mainRenderer.color = Color.white;
            spriteT.localScale = startScale;
        }
    }
}
