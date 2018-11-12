using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Setup : MonoBehaviour
{
    public enum Mode
    {
        HeadGaze,
        Gaze
    }

    public GameObject ui;
    public Dropdown modeControl;
    public InputField ipControl;

    public Mode mode { get { return (Mode)modeControl.value; } }
    public string ip { get { return ipControl.text; } }

    public Dictionary<string, string> settings { get { return _settings; } }

    public event EventHandler startServer = delegate { };
    public event EventHandler startClient = delegate { };

    // internal members

    NetworkManager _networkManager;

    Dictionary<string, string> _settings = new Dictionary<string, string>();

    const string FILENAME = "settings.txt";

    // overrides

    void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();

        try
        {
            System.IO.StreamReader stream = new System.IO.StreamReader(FILENAME);
            while (!stream.EndOfStream)
            {
                string line = stream.ReadLine();
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    _settings.Add(parts[0].Trim(), parts[1].Trim());
                }
            }
        }
        catch (Exception) { }

        modeControl.value = GetSettingValue("mode", (int)Mode.Gaze);
        ipControl.text = GetSettingValue("ip", "169.254.31.15");
    }

    // public mehtods

    public void hide()
    {
        ui.SetActive(false);
    }

    public void OnStartServerRequest()
    {
        SaveSettings();
        _networkManager.StartServer();
        startServer(this, new EventArgs());
    }

    public void OnStartClientRequest()
    {
        SaveSettings();
        _networkManager.networkAddress = _settings["ip"];
        _networkManager.StartClient();
        startClient(this, new EventArgs());
    }

    // internal methods

    void SaveSettings()
    {
        _settings["mode"] = modeControl.value.ToString();
        _settings["ip"] = ipControl.text;

        using (var stream = new System.IO.StreamWriter(FILENAME))
        {
            foreach (var pair in _settings)
            {
                stream.WriteLine($"{pair.Key} = {pair.Value}");
            }
        }
    }

    string GetSettingValue(string key, string defaultValue)
    {
        string result;

        if (!_settings.TryGetValue(key, out result) || string.IsNullOrEmpty(result))
        {
            result = defaultValue;
        }

        return result;
    }

    int GetSettingValue(string key, int defaultValue)
    {
        int result;
        string str;
        if (!_settings.TryGetValue(key, out str) || string.IsNullOrEmpty(str) || !int.TryParse(str, out result))
        {
            result = defaultValue;
        }

        return result;
    }
}
