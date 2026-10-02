using System.Collections;
using TMPro;
using UnityEngine;

public class Termite : MonoBehaviour
{
    [Header("Report Fade")]
    [SerializeField] private float holdBeforeFade = 1f;
    [SerializeField] private float fadeDuration   = 0.5f;

    private TermiteSpawnInfo info;
    private AntMonologue monologue;
    private bool dying;

    void Awake() => monologue = GetComponent<AntMonologue>();

    public void Init(TermiteSpawnInfo i) => info = i;

    public void Judge()
    {
        if (dying) return;
        if (StageFlow.instance != null && StageFlow.instance.IsEnded) return;

        if (info != null && info.isReal)
        {
            dying = true;

            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            InteractionPopup.instance?.ShowTermiteFound(monologue);
            AntCounter.instance?.AddFound();
            TutorialController.instance?.NotifyReportAnt();

            StartCoroutine(FadeOutAndDie());
        }
        else
        {
            InteractionPopup.instance?.ShowWrongTarget(monologue);
        }
    }

    private IEnumerator FadeOutAndDie()
    {
        yield return new WaitForSeconds(holdBeforeFade);

        if (monologue != null) monologue.FreezeForFade();

        var sprites = GetComponentsInChildren<SpriteRenderer>(true);
        var texts   = GetComponentsInChildren<TMP_Text>(true);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeDuration);

            foreach (var sr in sprites)
            {
                if (sr == null) continue;
                var c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, a);
            }
            foreach (var tx in texts)
            {
                if (tx == null) continue;
                tx.alpha = a;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}