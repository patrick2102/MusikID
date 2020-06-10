using System;
using System.Collections.Generic;

namespace DatabaseCommunication
{
    public interface IDBCommunication
    {
        bool Exec_MySQL_SUBFINGERID_D(string reference);
        bool Exec_MySQL_SUBFINGERID_IU(int diskotekNr, int sideNr, int sequenceNr, string reference, long duration, byte[] signature, out int trackID);
        bool GetRadioURLFromID(string id, out string url);
        bool GetRadioURLs(out Dictionary<string, string> dict);
        bool InsertJob(string jobType, DateTime startTime, float percentage, int fileID, string arguments, out int jobID, string user = null);
        bool InsertStation(string ID, string URL, out Station station);
        bool SongAlreadyFingerprinted(int diskotekNr, int sideNr, int sequenceNr);
        bool UpdateJob(int jobID, float percentage);
    }
}