using AudioFingerprint.Audio;
using ElasticFingerprints;
using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AudioFingerprint.SubFingerprintQuery;

namespace AudioFingerprint.Query
{
    public class ElasticQuery
    {
        private Func<QueryPlanConfig, int[], List<FingerprintSignature>> getFingerprints;
        private readonly ElasticClient _client;
        private QueryPlanConfig config;
        private int numberOfHits;

        public ElasticQuery(QueryPlanConfig _config, ElasticClient client)
        {

            this.getFingerprints = GetFingerprintsMySQL;
            _client = client;
            config = _config;
        }

        public void SubFingerIndexPlanWithVariants(QueryPlanConfig config, int blockSize, int indexStep, int fingerBlockSize, byte maxVariantBit, out List<HashIndex> subFingerListWithVariants)
        {
            DateTime startTime = DateTime.Now;
            subFingerListWithVariants = new List<HashIndex>();

            int maxFingerIndex = -1;
            int savedindex = 0;
            for (int index = (indexStep * blockSize); (index < (config.fsQuery.SubFingerprintCount - blockSize) && index <= ((indexStep * blockSize) + (fingerBlockSize * blockSize))); index++)
            {
                savedindex = index;
                uint h = config.fsQuery.SubFingerprint(index);
                if ((int)index / 256 > maxFingerIndex)
                {
                    maxFingerIndex = (int)index / 256;
                }
                // this is used for hamming difference
                uint[] hashes = SubFingerVariants(maxVariantBit, h, config.fsQuery.Reliability(index));
                int count = 0;
                foreach (uint h2 in hashes)
                {
                    subFingerListWithVariants.Add(new HashIndex(maxFingerIndex, index, h2, count != 0));
                    count++;
                }
            } // for j
        }

        public uint[] SubFingerVariants(byte maxBitFlips, uint hash, byte[] r)
        {
            // doe maximaal 2^10 variant meenemen
            if (maxBitFlips > 10)
            {
                maxBitFlips = 10;
            }
            int countFlip = 0;
            List<SimpleBitVector32> bitVectors = new List<SimpleBitVector32>();
            bitVectors.Add(new SimpleBitVector32(hash));
            for (byte k = 0; k < maxBitFlips; k++)
            {
                for (byte i = 0; i < r.Length; i++)
                {
                    if (r[i] == k)
                    {
                        // flip bit
                        int len = bitVectors.Count;
                        for (int j = 0; j < len; j++)
                        {
                            SimpleBitVector32 bv = new SimpleBitVector32(bitVectors[j].UInt32Value);
                            bv.Toggle(i);
                            bitVectors.Add(bv);
                        }
                        countFlip++;
                        // Exit i loop want we zijn klaar met deze bit
                        break;
                    }
                } //for i
            } //for k

            // voeg orgineel en varianten toe, filter op minimaal aantal bits die zijn veranderd
            System.Collections.Hashtable table = new System.Collections.Hashtable(bitVectors.Count);
            foreach (SimpleBitVector32 bv in bitVectors)
            {
                uint h = bv.UInt32Value;
                int bits = AudioFingerprint.Math.SimilarityUtility.HammingDistance(h, 0);
                if (bits <= 13 || bits >= 19) // 5 27  (10 22)
                {
                    // try further in the fingerprint
                    continue;
                }
                if (!table.ContainsKey(h))
                {
                    table.Add(h, h);
                }
            } //foreach

            return table.Keys.Cast<uint>().ToArray();
        }

        public List<ElasticFingerprints.ElasticFingerprint> QuerySubFingers(QueryPlanConfig config, List<HashIndex> qSubFingerIndex)
        {
            DateTime startTime = DateTime.Now;

            string hash = "";
            for (int j = 0; j < qSubFingerIndex.Count - 1; j++)
            {
                hash = hash + (qSubFingerIndex[j].Hash.ToString()) + " ";
            }

            // Zoek naar de fingerprint in de database
            List<ElasticFingerprints.ElasticFingerprint> list = FindPossibleMatches1Async(hash);
            

#if SHOWTRACEINFO
            Console.WriteLine(config.SearchStrategy.ToString() + " TotalHits=" + topHits.TotalHits.ToString());
#endif
            return list;
        }

        public int[] ElasticTopDocs2FingerIDs(QueryPlanConfig config, List<ElasticFingerprints.ElasticFingerprint> list)
        {
            DateTime startTime = DateTime.Now;
            List<int> fingerIDs = new List<int>();
           foreach(var fingerprint in list)
            {
                int fid = Convert.ToInt32(fingerprint.Id);
                fingerIDs.Add(fid);
            }

                config.SubFingerQueryTime = config.SubFingerQueryTime.Add(DateTime.Now - startTime);

            return fingerIDs.ToArray();
        }

        //TODO rewrite this to Framework, might be here error happens. 
        public List<FingerprintSignature> GetFingerprintsMySQL(QueryPlanConfig config, int[] fingerIDList)
        {
            try
            {
                if (fingerIDList.Length == 0)
                {
                    return new List<FingerprintSignature>();
                }

                DateTime startTime = DateTime.Now;
                System.Collections.Concurrent.ConcurrentBag<FingerprintSignature> fingerBag = new System.Collections.Concurrent.ConcurrentBag<FingerprintSignature>();

                using (MySql.Data.MySqlClient.MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
                {
                    StringBuilder sb = new StringBuilder(1024);
                    sb.Append("SELECT *\r\n");
                    sb.Append("FROM   SONGS AS T1,\r\n");
                    sb.Append("       SUBFINGERID AS T2\r\n");
                    sb.Append("WHERE  T1.ID = T2.ID\r\n");
                    sb.Append("AND    T1.ID IN (\r\n");
                    int count = 0;
                    System.Collections.Hashtable hTable = new System.Collections.Hashtable(fingerIDList.Length);
                    foreach (int id in fingerIDList)
                    {
                        if (count > 0)
                        {
                            sb.Append(',');
                        }
                        sb.Append(id.ToString());
                        hTable.Add(id, count);
                        count++;
                    }
                    sb.Append(')');

                    MySql.Data.MySqlClient.MySqlCommand command = new MySql.Data.MySqlClient.MySqlCommand(sb.ToString(), conn);
                    command.CommandTimeout = 120;

                    MySql.Data.MySqlClient.MySqlDataAdapter adapter = new MySql.Data.MySqlClient.MySqlDataAdapter(command);
                    System.Data.DataSet ds = new System.Data.DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count > 0)
                    {
                        foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                        {
                            FingerprintSignature fs;
                            fs = new FingerprintSignature(row["REFERENCE"].ToString(), Convert.ToInt32(row["DR_DISKOTEKSNR"]), Convert.ToInt32(row["SIDENUMMER"]), Convert.ToInt32(row["SEKVENSNUMMER"]),
                                Convert.ToInt64(row["ID"]), (byte[])row["SIGNATURE"], Convert.ToInt64(row["DURATION"]), true);


                            int fingerID = Convert.ToInt32(row["ID"]);
                            fs.Tag = hTable[fingerID];

                            fingerBag.Add(fs);
                        }
                    }
                }

                List<FingerprintSignature> result = fingerBag.OrderBy(e => (int)e.Tag)
                                                             .ToList();
                config.FingerLoadTime = DateTime.Now - startTime;

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("=======ERRROR IN ELASTICQUERY=======");
                Console.WriteLine(e); 
                return new List<FingerprintSignature>();
            }
        }

        //private int[] LuceneTopDocs2FingerIDs(List<ElasticFingerprints.ElasticFingerprint> list)
        //{
        //    DateTime startTime = DateTime.Now;
        //    List<int> fingerIDs = new List<int>(); 

        //    foreach(var fingerprint in list)
        //    {
        //        int fid = Convert.ToInt32(fingerprint.Id);
                
        //            if (!fingerIDs.Contains(fid))
        //            {
        //                fingerIDs.Add(fid);
        //            }
                

        //    } 
        //    return fingerIDs.ToArray();
        //}

        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        public List<ElasticFingerprint> FindPossibleMatches1Async(string signatures)
        {
            //Check om queue er over 200 på nogen af noderne
            bool isOverloaded = true;
            while (isOverloaded)
            {
                var r = _client.Cat.ThreadPool();
                var information = r.Records;
                var searchList = information.Where(s => s.Name == "search");
                foreach (var i in searchList)
                {
                    var queue = i.Queue;
                    if(queue < 200)
                    {
                        isOverloaded = false;
                    }
                    else {
                        Thread.Sleep(5000);
                    }
                    
                }
            }
            
           


            var GUID = RandomNumber(1, 1000000);
            var searchResponse = _client.Search<ElasticFingerprint>(s => s
                //.Scroll("10m")
                //.RequestConfiguration(r => r
                //.OpaqueId(GUID.ToString())
                // )
                .From(0)
                .Size(config.maxSearchResult)
                .Query(qu => qu
                .Match(m => m
                .Field(f => f.Fp)
                .Query(signatures)
                )
                )
            );


            var fingerprints = searchResponse.Documents;
            if(fingerprints.Count == 0)
                Console.WriteLine("No fingerprints found in elastic");

            //if (fingerprints.Count == 0)
            //{
            //    Console.WriteLine("No match found");
            //}
            //else if (fingerprints.Count > 1)
            //{
            //    Console.WriteLine("Multiple hits");
            //}
            //foreach (var fingerprint in fingerprints)
            //{
            //    Console.WriteLine("ID: " + fingerprint.Id);
            //}
            return fingerprints.ToList(); 
        }

        public void FindPossibleMatch(QueryPlanConfig config, List<FingerprintSignature> fingerprints, List<HashIndex> qSubFingerIndex)
        {
            DateTime starttime = DateTime.Now;
            ConcurrentDictionary<string, FingerprintHit> possibleHits = new ConcurrentDictionary<string, FingerprintHit>();
            config.LowestBER = int.MaxValue;
            int sLowestBER = config.LowestBER;
            // Nu gaan we op basis van fingerprints een match proberen te vinden
            int BERMatch = config.MinimalBERMatch;
            if (BERMatch < 0) // get default?
            {
                BERMatch = FingerprintSignature.BER(256);
            }

            int indexNumberInMatchList = 0;
            foreach (FingerprintSignature fsMatch in fingerprints)
            {
                indexNumberInMatchList++;
                string key = (string)fsMatch.Reference;

                // ---------------------------------------------------------------
                // Tel aantal hits dat we vonden in deze 256 subfingers.
                // handiog om te beoordelen hoe we komen tot een hit
                int subFingerCountHitInFingerprint = 0;
                foreach (HashIndex hi in qSubFingerIndex)
                {
                    if (fsMatch.IndexOf(hi.Hash).Length > 0)
                    {
                        subFingerCountHitInFingerprint++;
                    }
                } //foreach
                  // ---------------------------------------------------------------

                bool foundMatch = false; // to exit loop from an innerloop
                int checkCount = 0;
                foreach (HashIndex hi in qSubFingerIndex)
                {
                    // Na 2 BER controles stoppen, is het toch niet
                    if (checkCount >= 2)
                    {
                        break;
                    }
                    int[] indexOfList = fsMatch.IndexOf(hi.Hash);
                    if (indexOfList.Length != 0)
                    {
                        uint[] fsQuery256 = config.fsQuery.Fingerprint(hi.Index);
                        checkCount++;

                        foreach (int indexOf in indexOfList)
                        {
                            uint[] fsMatch256 = fsMatch.Fingerprint(indexOf);
                            if (fsMatch256 == null)
                            {
                                // we zijn klaar
                                break;
                            }

                            int BER = FingerprintSignature.HammingDistance(fsMatch256, fsQuery256);
#if SHOWTRACEINFO
                                    //System.Diagnostics.Trace.WriteLine(key + " BER=" + BER.ToString() + " Timeindex=" + ((indexOf * 11.6) / 1000).ToString("#0.000") + "sec");
                                    if (TestTrackID(key))
                                    {
                                        Console.WriteLine(key + " BER=" + BER.ToString() + " Timeindex=" + ((indexOf * 11.6) / 1000).ToString("#0.000") + "sec");
                                    }
#endif

                            if (BER < BERMatch)
                            {
                                // een mogelijk hit!
                                //System.Diagnostics.Trace.WriteLine(key + " BER(" + BERMatch.ToString() + ")=" + BER.ToString() + " Timeindex=" + ((indexOf * 11.6) / 1000).ToString("#0.000") + "sec
                                if (config.Hits.ContainsKey(key))
                                {
                                    if (BER < config.Hits[key].BER)
                                    {
                                        config.Hits[key] = new FingerprintHit(fsMatch, indexOf, BER, indexNumberInMatchList, subFingerCountHitInFingerprint, config.SearchStrategy, config.SearchIteration);
                                    }
                                }
                                else
                                {
                                    config.Hits.TryAdd(key, new FingerprintHit(fsMatch, indexOf, BER, indexNumberInMatchList, subFingerCountHitInFingerprint, config.SearchStrategy, config.SearchIteration));
                                }
                                if (sLowestBER > BER)
                                {
                                    sLowestBER = BER;
                                }

                                foundMatch = true;
                                //break;
                            }
                        } //foreach

                        // Als we hit gevonden hebben stop met verder zoeken
                        if (foundMatch)
                        {
                            //break;
                        }
                    }
                } // foreach

                // Good enough BER
                if (sLowestBER < 1800)
                {
                    // break;
                }
            }
            config.LowestBER = sLowestBER;
            config.MatchTime = config.MatchTime.Add(DateTime.Now - starttime);
        }
    }
}
