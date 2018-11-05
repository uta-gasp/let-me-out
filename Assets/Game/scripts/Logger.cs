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

        private StreamWriter _stream = null;
        private List<string> buffer = new List<string>();

        internal LogDomain(string aHeader, bool aEnabled)
        {
            _header = aHeader;
            enabled = aEnabled;
        }

        public void add(string aText)
        {
            if (enabled)
            {
                Added(this, $"{Time.time}\t{_header}\t{aText}");
            }
        }
    }

    private StreamWriter _stream = null;
    private uint _startTime;
    private LogDomain _general;

    private List<string> _buffer = new List<string>();
    private Dictionary<string, LogDomain> _domains = new Dictionary<string, LogDomain>();

    public LogDomain register(string aName, bool aEnabled = true)
    {
        LogDomain result;
        if (!_domains.ContainsKey(aName))
        {
            result = new LogDomain(aName, aEnabled);
            result.Added += onRecordAdded;
            result.add("init");

            _domains.Add(aName, result);
        }
        else
        {
            result = _domains[aName];
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
        _startTime = 0;

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
