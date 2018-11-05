using System.Linq;
using UnityEngine;

public class DebugDesk : MonoBehaviour {

    public UnityEngine.UI.Text display;
    public bool show;

    public void print(string aMsg)
    {
        Print(aMsg, "");
    }

    public void print(string aID, string aMsg)
    {
        Print(aMsg, aID);
    }

    public void print(string aMsg, bool aIsLocalPlayer, string aID = null)
    {
        Print(Network.isServer ? "SERVER" : ("CLIENT" + (aIsLocalPlayer ? " [LOCAL]" : " [REMOTE]")), aID, aMsg);
    }

    public void print(string aMsg, bool aIsServer, bool aIsLocalPlayer, string aID = null)
    {
        Print(aIsServer ? "SERVER" : ("CLIENT" + (aIsLocalPlayer ? " [LOCAL]" : " [REMOTE]")), aID, aMsg);
    }

    // overriden

    void Start()
    {
        if (display == null)
        {
            display = FindObjectsOfType<UnityEngine.UI.Text>().Single(text => text.tag == "debug");
        }
    }

    // internal

    private void Print(string aMsg)
    {
        if (!show || !enabled)
            return;

        Debug.Log(aMsg);

        if (display != null)
        {
            var lines = display.text.Split('\n').Where((line, i) => i < 30);
            display.text = aMsg + "\n" + string.Join("\n", lines.ToArray());
        }
    }

    private void Print(string aMsg, string aID = "")
    {
        string msg = string.IsNullOrEmpty(aID) ? aMsg : $"[{aID}] {aMsg}";
        Print(msg);
    }

    private void Print(string aPlayer, string aID, string aMsg)
    {
        string msg = $"{aPlayer} - {aID} - {aMsg}";
        Print(msg);
    }
}
