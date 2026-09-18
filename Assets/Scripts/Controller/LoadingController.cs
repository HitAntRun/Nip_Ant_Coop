using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

public class LoadingController : MonoBehaviour
{
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private LoadingTipData tipData;
    [SerializeField] private float minDisplayTime = 2.5f;
    [SerializeField] private string fallbackScene = SceneRouter.StoryScene;

    IEnumerator Start()
    {
        string target = SceneRouter.NextScene;
        if (string.IsNullOrEmpty(target))
            target = fallbackScene;

        yield return LocalizationSettings.InitializationOperation;

        if (tipText != null && tipData != null)
        {
            var loc = tipData.PickRandom(target);
            if (loc != null)
            {
                var handle = loc.GetLocalizedStringAsync();
                yield return handle;
                if (handle.IsDone && handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    tipText.text = handle.Result;
            }
            else
            {
                tipText.text = string.Empty;
            }
        }

        float minTime = Mathf.Max(minDisplayTime, SceneRouter.MinLoadingTime);
        var op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;

        float t = 0f;
        while (op.progress < 0.9f || t < minTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        op.allowSceneActivation = true;
    }
}
