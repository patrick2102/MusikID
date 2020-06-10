using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Framework.Rules
{
    public class DRInfoRule : IBusinessRule
    {
        public int GetPriority()
        {
            return 2;
        }
        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            
            foreach (var res in input)
            {
                if (res._diskotekNr == -1) continue;

                var reference = $"{res._diskotekNr}-{res._sideNr}-{res._sequenceNr}";
                res._reference = reference;
                DRInfo info = GetInfo(reference).Result;

                res.title = info.title;
                res.artists = info.GetArtists();
                res.primary_src = info.primary_src;
            }

            return input;
        }

        public class DRInfo
        {
            public string title { get; set; }
            public string primary_src { get; set; }
            public IEnumerable<string> artists { get; set; }

            public DRInfo(string Title, IEnumerable<string> Artists, string Primary_src)
            {
                title = Title;
                artists = Artists;
                primary_src = Primary_src;
            }

            public string GetArtists()
            {
                string tmp = "";

                int count = 0;
                foreach (var artist in artists)
                {
                    if (count > 0) tmp += ", ";
                    tmp += $"{artist}";
                    count++;
                }
                return tmp;
            }
        }

        string url = "https://music-api.public.prod.gcp.dr.dk/api/tracks/";

        async Task<DRInfo> GetInfo(string reference)
        {
            string json = null;
            HttpClient client = new HttpClient();

            var path = $"{url}{reference}";

            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                json = await response.Content.ReadAsStringAsync();

            } else
            {
                return new DRInfo("Unknown",new List<string>() { "Unknown" }, reference) ;
            }
            RootObject track = JsonConvert.DeserializeObject<RootObject>(json);

            var title = track.title;

            var primaries = track.roles.Where(r => r.primary);
            List<string> artists;
            if (primaries.Count() == 0) artists = new List<string>() { "Unknown" };

            else  artists = primaries.Select(r => r.artist).Select(a => a.names.Exists(n => n.type.ToLower() == "kunstnernavn") ? a.names.Where(n => n.type.ToLower() == "kunstnernavn").First().name : a.names.First().name).ToList();

            string primary_src = "";
            if (!track.primary)
                primary_src = track.primarySource.Split('/').Last();
            else
                primary_src = reference;

            return new DRInfo(title, artists, primary_src);
        }

        public List<IBusinessRule> GetRequiredRules()
        {
            throw new NotImplementedException();
        }
    }
    
    public class Role
    {
        public int artistId { get; set; }
        public int key { get; set; }
        public bool primary { get; set; }
        public string name { get; set; }
        public int sortNumber { get; set; }
        public int subSortNumber { get; set; }
    }

    public class Composition
    {
        public int id { get; set; }
        public bool @public { get; set; }
        public string title { get; set; }
        public List<Role> roles { get; set; }
        public string href { get; set; }
    }

    public class Period
    {
        public DateTime endDate { get; set; }
        public int endYear { get; set; }
    }

    public class Recording
    {
        public Period period { get; set; }
        public string country { get; set; }
        public string location { get; set; }
    }

    public class Label
    {
        public string name { get; set; }
        public bool primary { get; set; }
        public string catalogNumber { get; set; }
        public int releaseYear { get; set; }
        public DateTime releaseDate { get; set; }
    }

    public class Role2
    {
        public int artistId { get; set; }
        public int key { get; set; }
        public bool primary { get; set; }
        public string name { get; set; }
        public int sortNumber { get; set; }
        public int subSortNumber { get; set; }
    }

    public class Size
    {
        public string size { get; set; }
        public string source { get; set; }
    }

    public class Image
    {
        public List<Size> sizes { get; set; }
        public int sortNumber { get; set; }
    }

    public class Release
    {
        public int id { get; set; }
        public bool @public { get; set; }
        public bool primary { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public bool commercial { get; set; }
        public string format { get; set; }
        public List<Label> labels { get; set; }
        public List<Role2> roles { get; set; }
        public List<Image> images { get; set; }
        public string href { get; set; }
    }

    public class Name
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public bool primary { get; set; }
    }

    public class Artist
    {
        public int id { get; set; }
        public bool @public { get; set; }
        public string description { get; set; }
        public string country { get; set; }
        public List<Name> names { get; set; }
        public List<object> images { get; set; }
        public string href { get; set; }
    }

    public class Role3
    {
        public Artist artist { get; set; }
        public int key { get; set; }
        public bool primary { get; set; }
        public string name { get; set; }
        public int sortNumber { get; set; }
        public int subSortNumber { get; set; }
        public string subName { get; set; }
    }

    public class Role4
    {
        public int artistId { get; set; }
        public int key { get; set; }
        public bool primary { get; set; }
        public string name { get; set; }
        public int sortNumber { get; set; }
        public int subSortNumber { get; set; }
        public string subName { get; set; }
    }

    public class Track
    {
        public string id { get; set; }
        public bool @public { get; set; }
        public bool primary { get; set; }
        public string title { get; set; }
        public int duration { get; set; }
        public List<object> subtracks { get; set; }
        public int compositionId { get; set; }
        public List<string> genres { get; set; }
        public int releaseId { get; set; }
        public List<Role4> roles { get; set; }
        public string href { get; set; }
    }

    public class Relationship
    {
        public Track track { get; set; }
        public int key { get; set; }
        public string type { get; set; }
        public int sortNumber { get; set; }
    }

    public class RootObject
    {
        public string id { get; set; }
        public bool @public { get; set; }
        public bool primary { get; set; }
        public string primarySource { get; set; }
        public string title { get; set; }
        public int sideNumber { get; set; }
        public int trackNumber { get; set; }
        public int duration { get; set; }
        public string language { get; set; }
        public List<object> subtracks { get; set; }
        public Composition composition { get; set; }
        public Recording recording { get; set; }
        public List<string> genres { get; set; }
        public List<object> classicalPeriods { get; set; }
        public List<string> styles { get; set; }
        public Release release { get; set; }
        public List<Role3> roles { get; set; }
        public List<Relationship> relationships { get; set; }
        public List<object> links { get; set; }
    }
    
}
