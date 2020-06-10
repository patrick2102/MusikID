using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Framework
{
    public class Result
    {
        public float _accuracy;
        public string _reference;
        public DateTime _startTime, _endTime;
        public int _diskotekNr, _sideNr, _sequenceNr;
        public int resultID; // this is receive when the result is first submitted to the database.
        public string _filePath, _fileType;
        public float _song_offset_seconds;
        public bool doneUpdatingDB;
        public float _song_duration;

        public string title, artists, primary_src;
        
        public float _timeIndex;

        public List<Result> _joinedResults = new List<Result>();
        public Result()
        {
            _joinedResults.Add(this);
        }
        public Result(string reference, DateTime startTime, DateTime endTime, float accuracy = -1, int diskotekNr = -1, int sideNr = -1, int sequenceNr = -1, float song_offset_seconds = -1, float song_duration = -1)
        {
            _reference = reference;
            _startTime = startTime;
            _endTime = endTime;
            _diskotekNr = diskotekNr;
            _sideNr = sideNr;
            _sequenceNr = sequenceNr;
            resultID = -1;
            _accuracy = accuracy;
            _song_offset_seconds = song_offset_seconds;
            _song_duration = song_duration;
            _timeIndex = _song_offset_seconds;
            _joinedResults.Add(this);
            doneUpdatingDB = false;
        }

        public int ResultCount()
        {
            return _joinedResults.Count;
        }

        public override string ToString()
        {
            return createResultString(DateTime.Today);
        }

        public string ToString(DateTime offset)
        {
            return createResultString(offset);
        }

        public string createResultString(DateTime offset)
        {
            var reference = $"{_diskotekNr}-{_sideNr}-{_sequenceNr}";

            var min = _joinedResults.Min(r => r._timeIndex) / 1000f;
            var max = _joinedResults.Max(r => r._timeIndex) / 1000f;


            var _accuracy = _joinedResults.Sum(r => r._accuracy) / _joinedResults.Count;


            var time_indexes = _joinedResults.Select(r => (int) (r._timeIndex / 1000));

            var str_ti = "";

            foreach (var ti in time_indexes)
            {
                str_ti += ti + " ";
            }

            return $"{GetStartTime().Add(new TimeSpan(offset.Ticks)).ToString("HH:mm:ss")} ; {GetEndTime().Add(new TimeSpan(offset.Ticks)).ToString("HH:mm:ss")} ; {title} ; {artists} ; {GetAccuracy()}"; // ; {str_ti}";
            //return "Deprecated string representation";
        }

        public bool isUnknown()
        {
            return (GetAccuracy() == -1);
        }

        public DateTime GetStartTime()
        {
            var st = _joinedResults.Min(r => r._startTime);
            this._startTime = st;
            return st;
        }

        public DateTime GetEndTime()
        {
            return _joinedResults.Max(r => r._endTime);
        }

        public bool Overlaps (Result res)
        {
            
            //if (res.GetEndTime() - this.GetEndTime() > TimeSpan.FromSeconds(60)) return false;
            //HACK to check that overlapping is at least 16 seconds. 
            return (this.GetStartTime().AddSeconds(16) <= res.GetEndTime() && this.GetEndTime().AddSeconds(-16) >= res.GetStartTime());
        }



        public override bool Equals(Object obj)
        {
            var other = (obj as Result);
            if (other == null) return false;
            return (_diskotekNr == other._diskotekNr) && (_sideNr == other._sideNr) && (_sequenceNr == other._sequenceNr);
        }

        public void UpdateValues(Result result)
        {
            _joinedResults.AddRange(result._joinedResults);
            //_timeIndexes.Add(result._song_offset_seconds);
            _endTime = new DateTime(Math.Max(this._endTime.Ticks, result._endTime.Ticks));
            _startTime = new DateTime(Math.Min(this._startTime.Ticks, result._startTime.Ticks));
        }

        public int GetDuration()
        {
            return ((int)((GetEndTime().TimeOfDay) - (GetStartTime().TimeOfDay)).TotalSeconds);
        }

        public float GetAccuracy()
        {
            return _joinedResults.Sum(r => r._accuracy) / _joinedResults.Count;
        }
        public float GetLowestTimeIndex()
        {
            return _joinedResults.Min(r => r._timeIndex);
        }
        public float GetHighestTimeIndex()
        {
            return _joinedResults.Max(r => r._timeIndex);
        }
    }
}
