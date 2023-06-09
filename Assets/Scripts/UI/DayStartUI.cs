using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DayStartUI : MonoBehaviour {
    public RectTransform bgTransform;

    public RectTransform characterT;
    public Image characterImage;
    public Sprite dayCharacterSprite;
    public Sprite nightCharacterSprite;


    private void Awake() {
        SetCharacterNightMode(true);
    }

    private Action onComplete;
    public void Show(Action onCoverScreen, Action _onComplete) {
        onComplete = _onComplete;
        Vector2 startPos = bgTransform.anchoredPosition;
        Player.instance.CreateAnimationRoutine(1f, (float progress) => {
            bgTransform.anchoredPosition = Vector2.Lerp(startPos, NightStartUI.ON_SCREEN_POS, Easing.easeInSine(0, 1, progress));
        }, () => {
            onCoverScreen?.Invoke();
            Continue();
        });
    }

    private void Continue() {
        Player.instance.StartCoroutine(ContinueRoutine());
        IEnumerator ContinueRoutine() {
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Player.instance.CreateAnimationRoutine(0.5f, (float progress) => {
                characterT.localEulerAngles = new Vector3(0, Mathf.Lerp(0, 90, progress), 0);
            });

            SetCharacterNightMode(false);

            yield return Player.instance.CreateAnimationRoutine(0.5f, (float progress) => {
                characterT.localEulerAngles = new Vector3(0, Mathf.Lerp(90, 0, progress), 0);
            });
            yield return new WaitForSecondsRealtime(0.3f);
            Vector2 startPos = bgTransform.anchoredPosition;
            Player.instance.CreateAnimationRoutine(1f, (float progress) => {
                bgTransform.anchoredPosition = Vector2.Lerp(startPos, NightStartUI.OFF_SCREEN_POS, Easing.easeInSine(0, 1, progress));
            }, () => {
                onComplete?.Invoke();
                SetCharacterNightMode(true);
            });
        }
    }

    private void SetCharacterNightMode(bool isNight) {
        characterT.sizeDelta = isNight ? NightStartUI.NIGHT_SPRITE_SIZE : NightStartUI.DAY_SPRITE_SIZE;
        characterImage.sprite = isNight ? nightCharacterSprite : dayCharacterSprite;
    }
}
