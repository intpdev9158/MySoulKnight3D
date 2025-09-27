using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class UIFlowHUD : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject startPanel;
    [SerializeField] GameObject clearPanel;
    [SerializeField] GameObject exitPanel;

    [Header("Texts (optional)")]
    [SerializeField] TMP_Text startText;
    [SerializeField] TMP_Text clearText;
    [SerializeField] TMP_Text exitText;

    [Header("Messages")]
    [SerializeField] string startMsg = "Press E to Start";
    [SerializeField] string clearMsg = "CLEAR!";
    [SerializeField] string exitMsg = "Congratulation!\nPress E to Exit";

    [Header("Duration")]
    [SerializeField] float clearShowSeconds = 1.5f;


    void Awake()
    {
        SetActive(startPanel, false);
        SetActive(clearPanel, false);
        SetActive(exitPanel, false);

        if (startText) startText.text = startMsg;
        if (clearText) clearText.text = clearMsg;
        if (exitText) exitText.text = exitMsg;

    }

    public void ShowStart(bool on) => SetActive(startPanel, on);
    public void ShowClear(bool on) => SetActive(clearPanel, on);
    public void ShowExit(bool on) => SetActive(exitPanel,   on);

    public void ShowStartMsg(string msg) { if (startText) startText.text = msg; }
    public void ShowClearMsg(string msg) { if (clearText) clearText.text = msg; }
    public void ShowExitMsg (string msg) { if (exitText) exitText.text = msg; }

    public void HideClearAfter()
    {
        StartCoroutine(HideClearAfter(clearShowSeconds));
    }

    System.Collections.IEnumerator HideClearAfter(float t)
    {
        yield return new WaitForSeconds(t);
        ShowClear(false);
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go && go.activeSelf != on) go.SetActive(on);
    }
}
