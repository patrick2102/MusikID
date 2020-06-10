using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DatabaseCommunication
{
    public class OnDemandFile
    {
        public string path;

        public int file_id;

        public float percentage;

        public string user;


        public OnDemandFile (string Path, int File_id, float Percentage, string User = null)
        {
            path = Path;
            file_id = File_id;
            percentage = Percentage;
            user = User;
        }
    }
}
