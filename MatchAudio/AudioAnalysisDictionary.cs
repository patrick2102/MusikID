using Framework;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MatchAudio
{
    public class AudioAnalysisDictionary //TODO This class is redundant, get rid of it.
    {
        string _channelID;

        public AudioAnalysisDictionary(string channelID = "")
        {
            _channelID = channelID;
        }

        public AudioAnalysisDictionary()
        {
            _channelID = "";
        }

    }
}
