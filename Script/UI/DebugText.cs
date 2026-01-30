using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
    public class DebugText : MonoBehaviour
    {
        private static DebugText _instance;

        private Text _text;
        private SortedDictionary<int, string> _lines;

        public static void Log(int id, string str)
        {
            _instance._lines[id] = str;
        }

        private void Awake()
        {
            _instance = this;
            _text = GetComponent<Text>();
            _lines = new SortedDictionary<int, string>();
        }

        private void LateUpdate()
        {
            _text.text = "";
            foreach (var line in _lines)
            {
                _text.text += $"{line.Value}\n";
            }
        }
    }
}
