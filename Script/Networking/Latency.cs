using System;
using System.Collections.Generic;

namespace HoverTanks.Networking
{
    public class LatencyHistory
	{
		public int Average { get; private set; }

		private const int LATENCY_HISTORY_SIZE = 3;
		private List<int> _history = new List<int>(LATENCY_HISTORY_SIZE);
        private int _nextRecordIndex;
        private int _numTimesRecorded;

		public void Record(int latency)
		{
			if (_history.Count < _nextRecordIndex + 1)
            {
                _history.Add(0);
            }

            // record in history
            _history[_nextRecordIndex++] = latency;
            _nextRecordIndex %= LATENCY_HISTORY_SIZE;

            int allLatencies = 0;

            // sum up all history
            for (int i = 0; i < _history.Count; ++i)
            {
                allLatencies += _history[i];
            }

            ++_numTimesRecorded;

            // average out latency
            Average = allLatencies / Math.Min(_numTimesRecorded, _history.Count);
            Average = Math.Abs(Average);
		}
	}
}
