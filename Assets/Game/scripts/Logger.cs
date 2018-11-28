using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class Logger : MonoBehaviour
{
    string filename;

    public class LogDomain
    {
        public bool enabled;

        internal event EventHandler<string> Added = delegate { };

        private string _header;

        internal LogDomain(string aHeader, bool aEnabled)
        {
            _header = aHeader;
            enabled = aEnabled;
        }

        public void add(string aText, params string[] aArgs)
        {
            if (enabled)
            {
                List<string> fields = new List<string>();
                fields.Add(Time.time.ToString());
                fields.Add(_header);
                fields.Add(aText);
                fields.AddRange(aArgs);
                Added(this, string.Join("\t", fields));
            }
        }
    }

    private StreamWriter _stream = null;
    private LogDomain _general;

    private List<string> _buffer = new List<string>();
    private Dictionary<string, LogDomain> _domains = new Dictionary<string, LogDomain>();

    public LogDomain register(string aName, string aID = null, bool aEnabled = true)
    {
        string name = string.IsNullOrEmpty(aID) ? aName : $"{aName}\t{aID}";

        LogDomain result;
        if (!_domains.ContainsKey(name))
        {
            result = new LogDomain(name, aEnabled);
            result.Added += onRecordAdded;
            result.add("init");

            _domains.Add(name, result);
        }
        else
        {
            result = _domains[name];
        }

        return result;
    }

    private void onRecordAdded(object sender, string e)
    {
        if (_stream != null)
        {
            _stream.WriteLine(e);
        }
        else
        {
            lock (_buffer)
            {
                _buffer.Add(e);
            }
        }
    }

    void Start()
    {
        var rnd = new System.Random();
        filename = $"log_{rnd.Next()}.txt";

        try
        {
            _stream = new StreamWriter(filename);
        }
        catch (System.Exception ex)
        {
            print(ex.Message);
        }

        if (_stream != null)
        {
            lock (_buffer)
            {
                foreach (string s in _buffer)
                {
                    _stream.WriteLine(s);
                }
            }

            _buffer = null;
            _general = register("general");
        }
    }

    void OnDisable()
    {
        if (_general != null)
        {
            _general.add("end");
        }
        if (_stream != null)
        {
            _stream.Dispose();
        }
    }
}
