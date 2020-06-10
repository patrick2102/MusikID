//#define SHOWTRACEINFO
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioFingerprint.Audio;
using AudioFingerprint.Query;
using ElasticFingerprints;
using Nest;

namespace AudioFingerprint
{
    public class SubFingerprintQuery
    {
        public ConcurrentDictionary<string, FingerprintHit> EmptyDictionary = new ConcurrentDictionary<string, FingerprintHit>();
        private ElasticClient _client;
        public bool useLookupHash;

        public SubFingerprintQuery(Nest.ElasticClient client)
        {
            _client = client;
            // - GetFingerprintsLucene
            // - GetFingerprintsMSSQL
            // - GetFingerprintsMySQL
            //      this.getFingerprints = GetFingerprintsMySQL;

            this.useLookupHash = false;

            // Check if index hash precalculated lookup hashes (if not we init finger with different data)
            /*
            if (this.fingerIndex != null)
            {
                BooleanQuery query = new BooleanQuery();
                query.Add(new TermQuery(new Term("FINGERID", "1")), Occur.SHOULD);
                TopDocs topHits = this.fingerIndex.Search(query, 1);
                if (topHits.TotalHits > 0)
                {
                    ScoreDoc match = topHits.ScoreDocs[0];
                    Document doc = this.fingerIndex.Doc(match.Doc);
                    useLookupHash = (doc.GetField("LOOKUPHASHES") != null);
                }
            }
            */
            // Forceer dat setup table aangemaakt wordt
            AudioFingerprint.Math.SimilarityUtility.InitSimilarityUtility();
        }




        /// <summary>
        /// Beste zoek strategy is.
        /// Plan 1. Zoek eerst met zoveel mogelijk subfinger naar hits.
        ///          Kijk dan bij de eerste 25 hits of er een BER kleiner dan 2000 bij zit
        /// Plan 2. Niks gevonden.
        ///         Ga opnieuw 1ste 256 finger blok en doe nu stappen van 512 subfingers
        ///         Neem nu ook "varianten" mee
        /// </summary>
        public Resultset MatchAudioFingerprint(FingerprintSignature fsQuery, int berMatch = -1)
        {
            DateTime startTime = DateTime.Now;

            ConcurrentDictionary<string, FingerprintHit> hits = new ConcurrentDictionary<string, FingerprintHit>();

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            ParallelOptions pOptions = new ParallelOptions();
            pOptions.CancellationToken = tokenSource.Token;

            Resultset result = new Resultset();
            {
                QueryPlanConfig config = new QueryPlanConfig();
                config.SearchStrategy = SearchStrategy.Plan0;
                config.fsQuery = fsQuery;
                config.maxSearchResult = 10;
                config.MinimalBERMatch = 2000; // default is te hoog en geeft false positieves
                config.Token = tokenSource;

                DoPlan0(config);
                if (config.LowestBER < 2000)
                {
                    // Cancel the rest of the search options
                    tokenSource.Cancel();
                }
                // copy data 
                foreach (KeyValuePair<string, FingerprintHit> item in config.Hits)
                {
                    hits.TryAdd(item.Key, item.Value);
                } //foreach

                result.FingerQueryTime = result.FingerQueryTime.Add(config.SubFingerQueryTime);
                result.FingerLoadTime = result.FingerLoadTime.Add(config.FingerLoadTime);
                result.MatchTime = result.MatchTime.Add(config.MatchTime);
            }

            if (!tokenSource.IsCancellationRequested)
            {
                try
                {
                    Parallel.Invoke(pOptions,
                    () =>
                    {
                        QueryPlanConfig config = new QueryPlanConfig();
                        config.SearchStrategy = SearchStrategy.Plan1;
                        config.maxSearchResult = 10;
                        config.fsQuery = fsQuery;
                        config.MinimalBERMatch = 2300; // default is te hoog en geeft false positieves
                        config.Token = tokenSource;

                        DoPlan1(config);
                        if (!tokenSource.IsCancellationRequested && config.Hits.Count > 0)
                        {
                            if (config.LowestBER < 2000)
                            {
                                // Cancel the rest of the search options
                                tokenSource.Cancel();
                            }
                            // copy data 
                            foreach (KeyValuePair<string, FingerprintHit> item in config.Hits)
                            {
                                hits.TryAdd(item.Key, item.Value);
                            } //foreach
                        }

                        lock (result)
                        {
                            result.FingerQueryTime = result.FingerQueryTime.Add(config.SubFingerQueryTime);
                            result.FingerLoadTime = result.FingerLoadTime.Add(config.FingerLoadTime);
                            result.MatchTime = result.MatchTime.Add(config.MatchTime);
                        }
                    },
                    () =>
                     {
                     QueryPlanConfig config = new QueryPlanConfig();
                     config.SearchStrategy = SearchStrategy.Plan2;
                     config.fsQuery = fsQuery;
                     config.maxSearchResult = 25;
                     config.MinimalBERMatch = 2300; // vanwege extra subfinger varianten kan makkelijker een verkeerde match gevonden worden, dus strenger zijn!
                    config.Token = tokenSource;

                     DoPlan2(config);
                     if (!tokenSource.IsCancellationRequested && config.Hits.Count > 0)
                     {
                         if (config.LowestBER < 2300)
                         {
                            // Cancel the rest of the search options
                            tokenSource.Cancel();
                         }
                        // copy data 
                        foreach (KeyValuePair<string, FingerprintHit> item in config.Hits)
                         {
                             hits.TryAdd(item.Key, item.Value);
                         } //foreach
                     }

                         lock (result)
                         {
                             result.FingerQueryTime = result.FingerQueryTime.Add(config.SubFingerQueryTime);
                             result.FingerLoadTime = result.FingerLoadTime.Add(config.FingerLoadTime);
                             result.MatchTime = result.MatchTime.Add(config.MatchTime);
                         }
                     }
                    );
                }
                catch
                {
                }
            }

            // Geef resultaat terug
            List<ResultEntry> resultEntries = hits.Select(e => new ResultEntry
            {
                DiskotekNr = e.Value.Fingerprint.DiskotekNr,
                SideNr = e.Value.Fingerprint.SideNr,
                SequenceNr = e.Value.Fingerprint.SequenceNr,
                Reference = e.Value.Fingerprint.Reference,
                FingerTrackID = e.Value.Fingerprint.TrackID,
                Similarity = e.Value.BER,
                TimeIndex = e.Value.TimeIndex,
                IndexNumberInMatchList = e.Value.IndexNumberInMatchList,
                SubFingerCountHitInFingerprint = e.Value.SubFingerCountHitInFingerprint,
                SearchStrategy = e.Value.SearchStrategy,
                SearchIteration = e.Value.SearchIteration
            })
                                                  .ToList();

            result.QueryTime = (DateTime.Now - startTime);
            result.ResultEntries = resultEntries;
            result.Algorithm = FingerprintAlgorithm.SubFingerprint;
            return result;
        }

        public void DoPlan0(QueryPlanConfig config)
        {
            // =============================================================================================================================
            // Plan 0
            // =============================================================================================================================
#if SHOWTRACEINFO
            Console.WriteLine("PLAN 0");
#endif
            ElasticQuery eq = new ElasticQuery(config, _client);
            List<HashIndex> subFingerIndexWithVariants = new List<HashIndex>();
            eq.SubFingerIndexPlanWithVariants(config, 64, 0, 1, 3, out subFingerIndexWithVariants);
            List<HashIndex> subFingerIndexWithVariantsForMatch = new List<HashIndex>();
            eq.SubFingerIndexPlanWithVariants(config, 256, 0, 1, 3, out subFingerIndexWithVariantsForMatch);
            List<ElasticFingerprints.ElasticFingerprint> list = eq.QuerySubFingers(config, subFingerIndexWithVariants);
            int[] fingerIDs = eq.ElasticTopDocs2FingerIDs(config, list);
            // config.SubFingerQueryTime = config.SubFingerQueryTime.Add(DateTime.Now - startTime);
            List<FingerprintSignature> fingerprintSignatures = eq.GetFingerprintsMySQL(config, fingerIDs);

            //   int[] fingerIDs = LuceneTopDocs2FingerIDs(config, topHits);
            //     List<FingerprintSignature> fingerprints = getFingerprints(config, fingerIDs);
            if (config.Token != null && config.Token.IsCancellationRequested)
            {
                return;
            }
            eq.FindPossibleMatch(config, fingerprintSignatures, subFingerIndexWithVariantsForMatch);

            //       FindPossibleMatch(config, fingerprints, subFingerIndexWithVariantsForMatch);
        }

        private void DoPlan1(QueryPlanConfig config)
        {
            // =============================================================================================================================
            // Plan 1
            // =============================================================================================================================
#if SHOWTRACEINFO
            Console.WriteLine("PLAN 1");
#endif
            List<HashIndex> matchSubFingerIndex = new List<HashIndex>();
            ElasticQuery eq = new ElasticQuery(config, _client);

            // Haal lijst op over max 12 seconden data (geeft bij goede kwaliteit geluid snel een goede hit)
            SubFingerIndexPlanNormal(config, 4, out matchSubFingerIndex);
            if (config.Token.IsCancellationRequested)
            {
                return;
            }
            var topHits = eq.QuerySubFingers(config, matchSubFingerIndex);
            if (config.Token.IsCancellationRequested)
            {
                return;
            }
            int[] fingerIDs = eq.ElasticTopDocs2FingerIDs(config, topHits);
            List<FingerprintSignature> fingerprints = eq.GetFingerprintsMySQL(config, fingerIDs);
            if (config.Token.IsCancellationRequested)
            {
                return;
            }


            // Hint added 
            /*
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("TITELNUMMERTRACK", "JK140158-0004")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("FINGERID", "9169")), Occur.SHOULD);
            TopDocs tmpTopHits = fingerIndex.Search(query, 1);
            if (tmpTopHits.TotalHits > 0)
            {
                ScoreDoc match = tmpTopHits.ScoreDocs[0];
                Document doc = fingerIndex.Doc(match.Doc);
                FingerprintSignature fs = new FingerprintSignature(doc.Get("TITELNUMMERTRACK"), doc.GetBinaryValue("FINGERPRINT"),
                    doc.GetBinaryValue("LOOKUPHASHES"), Convert.ToInt64(doc.Get("DURATIONINMS")));
                fingerprints.Add(fs);
            }
            */

            eq.FindPossibleMatch(config, fingerprints, matchSubFingerIndex);
        }

        private void DoPlan2(QueryPlanConfig config)
        {
            // =============================================================================================================================
            // Plan 2
            // =============================================================================================================================
#if SHOWTRACEINFO
            Console.WriteLine("PLAN 2");
#endif
            List<HashIndex> subFingerIndexWithVariants = new List<HashIndex>();
            ElasticQuery eq = new ElasticQuery(config, _client);

            int maxIndexSteps = Convert.ToInt32((config.fsQuery.SubFingerprintCount - (config.fsQuery.SubFingerprintCount % 256)) / 256);
            if (maxIndexSteps > 8)
            {
                maxIndexSteps = 8;
            }
            Console.WriteLine("Doing plan 2");

            for (int indexStep = 0; indexStep < maxIndexSteps; indexStep++)
            {
                subFingerIndexWithVariants.Clear();
                config.SearchIteration = indexStep;

                SubFingerIndexPlanWithVariants(config, 256, indexStep, 1, 3, out subFingerIndexWithVariants);
                var topHits = eq.QuerySubFingers(config, subFingerIndexWithVariants);
                int[] fingerIDs = eq.ElasticTopDocs2FingerIDs(config, topHits);
                List<FingerprintSignature> fingerprints = eq.GetFingerprintsMySQL(config, fingerIDs);
                if (config.Token != null && config.Token.IsCancellationRequested)
                {
                    return;
                }


                // hint
                /*
                BooleanQuery query = new BooleanQuery();
                //query.Add(new TermQuery(new Term("TITELNUMMERTRACK", "JK140158-0004")), Occur.SHOULD);
                query.Add(new TermQuery(new Term("FINGERID", "1159967")), Occur.SHOULD); //JK190376-0006 / Photograph - Ed Sheeran
                TopDocs tmpTopHits = fingerIndex.Search(query, 1);
                if (tmpTopHits.TotalHits > 0)
                {
                    ScoreDoc match = tmpTopHits.ScoreDocs[0];
                    Document doc = fingerIndex.Doc(match.Doc);
                    FingerprintSignature fs;
                    if (useLookupHash)
                    {
                        fs = new FingerprintSignature(doc.Get("TITELNUMMERTRACK"), doc.GetBinaryValue("FINGERPRINT"),
                            doc.GetBinaryValue("LOOKUPHASHES"), Convert.ToInt64(doc.Get("DURATIONINMS")));
                    }
                    else
                    {
                        fs = new FingerprintSignature(doc.Get("TITELNUMMERTRACK"), doc.GetBinaryValue("FINGERPRINT"),
                            Convert.ToInt64(doc.Get("DURATIONINMS")), true);
                    }
                    fingerprints.Add(fs);
                }
                */

                eq.FindPossibleMatch(config, fingerprints, subFingerIndexWithVariants);
                if (config.Token != null && config.Token.IsCancellationRequested)
                {
                    config.LowestBER = int.MaxValue;
                    return;
                }

                // When we find a "GOOD" hit we stop searching
                if (config.Hits.Count > 0 && config.LowestBER < 2000)
                {
                    // token.cancel will be called from calling fnction!
                    return;
                }
            } //for indexstep (with max 4)
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

            // Make sure we don't return duplicates
            subFingerListWithVariants = subFingerListWithVariants.Distinct().ToList();
            config.SubFingerCreateQueryTime = config.SubFingerCreateQueryTime.Add(DateTime.Now - startTime);
        }

        private void SubFingerIndexPlanNormal(QueryPlanConfig config, int fingerBlockSize, out List<HashIndex> subFingerList)
        {
            DateTime startTime = DateTime.Now;
            subFingerList = new List<HashIndex>();
            int maxFingerIndex = -1;
            int savedindex = 0;
            for (int index = 0; (index < (config.fsQuery.SubFingerprintCount - 256) && index <= (fingerBlockSize * 256)); index++)
            {
                savedindex = index;
                uint h = config.fsQuery.SubFingerprint(index);
                int bits = AudioFingerprint.Math.SimilarityUtility.HammingDistance(h, 0);
                if (bits < 13 || bits > 19) // 5 27  (10 22)
                {
                    // try further in the fingerprint
                    continue;
                }
                if ((int)index / 256 > maxFingerIndex)
                {
                    maxFingerIndex = (int)index / 256;
                }
                subFingerList.Add(new HashIndex(maxFingerIndex, index, h));
            } // for j

            config.SubFingerCreateQueryTime = config.SubFingerCreateQueryTime.Add(DateTime.Now - startTime);
        }

        /// <summary>
        /// Voeg eventuele variant hashes (naast de orginele hash) toe op basis van reliabilty info
        /// De return hashes is een "unieke" lijst
        /// 
        /// maxBitFlips=3 (dan max 8 verschillende waardes)
        /// maxBitFlips=10 dan (max 1024 waardes, dit is tevens het maximum)
        /// </summary>
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

        private void FindPossibleMatch(QueryPlanConfig config, List<FingerprintSignature> fingerprints, List<HashIndex> qSubFingerIndex)
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

        private void FindPossibleMatchParallel(QueryPlanConfig config, List<FingerprintSignature> fingerprints, List<HashIndex> qSubFingerIndex)
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

            Parallel.ForEach(fingerprints, (FingerprintSignature fsMatch, ParallelLoopState foreachFingerprintState) =>
            {
                string key = (string)fsMatch.Reference;
#if SHOWTRACEINFO
                if (TestTrackID(key))
                {
                    int hitCount = 0;
                    foreach (HashIndex hi in qSubFingerIndex)
                    {
                        if (fsMatch.IndexOf(hi.Hash).Length > 0)
                        {
                            hitCount++;
                        }
                    } //foreach

                    Console.WriteLine("Probing " + key +  " (" + hitCount.ToString() + ")");
                }
#endif

                bool foundMatch = false; // to exit loop from an innerloop
                int checkCount = 0;
                //foreach (HashIndex hi in qSubFingerIndex)
                Parallel.ForEach(qSubFingerIndex, (HashIndex hi, ParallelLoopState foreachSubFingerState) =>
                {
                    // Na 2 BER controles stoppen, is het toch niet
                    if (checkCount >= 2)
                    {
                        foreachSubFingerState.Stop();
                        return;
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
                                        config.Hits[key] = new FingerprintHit(fsMatch, indexOf, BER, -1, -1, config.SearchStrategy, config.SearchIteration);
                                    }
                                }
                                else
                                {
                                    config.Hits.TryAdd(key, new FingerprintHit(fsMatch, indexOf, BER, -1, -1, config.SearchStrategy, config.SearchIteration));
                                }
                                if (sLowestBER > BER)
                                {
                                    sLowestBER = BER;
                                }

                                foundMatch = true;
                                break;
                            }
                        } //foreach

                        // Als we hit gevonden hebben stop met verder zoeken
                        if (foundMatch)
                        {
                            foreachSubFingerState.Stop();
                            return;
                        }
                    }
                }); //Parallel.Foreach

                // Good enough BER
                if (sLowestBER < 1800)
                {
                    foreachFingerprintState.Stop();
                    return;
                }
            }); //Parallel.Foreach
            config.LowestBER = sLowestBER;
            config.MatchTime = config.MatchTime.Add(DateTime.Now - starttime);
        }

        private void AddProbeEntry(Dictionary<int, int> probeHit, int docID, int score)
        {
            if (probeHit.ContainsKey(docID))
            {
                probeHit[docID] += -1;
            }
            else
            {
                probeHit.Add(docID, score);
            }
        }

        #region Retrieve complete fingerprint (Lucene/MySQL code)
        private delegate List<FingerprintSignature> GetFingerprints(QueryPlanConfig config, int[] fingerIDList);
        private GetFingerprints getFingerprints;


        private List<FingerprintSignature> GetFingerprintsFromDatabase(QueryPlanConfig config, int[] fingerIDList, bool newSongs)
        {
            if (fingerIDList.Length == 0)
            {
                return new List<FingerprintSignature>();
            }

            DateTime startTime = DateTime.Now;
            System.Collections.Concurrent.ConcurrentBag<FingerprintSignature> fingerBag = new System.Collections.Concurrent.ConcurrentBag<FingerprintSignature>();

            //TODO - Changed SONGS to NEWSONGS
            using (MySql.Data.MySqlClient.MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
            {

                StringBuilder sb = new StringBuilder(1024);
                sb.Append("SELECT *\r\n");

                if (newSongs) sb.Append("FROM  NEWSONGS AS T1,\r\n");
                else sb.Append("FROM  SONGS AS T1,\r\n");

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
                command.CommandTimeout = 60;

                MySql.Data.MySqlClient.MySqlDataAdapter adapter = new MySql.Data.MySqlClient.MySqlDataAdapter(command);
                System.Data.DataSet ds = new System.Data.DataSet();
                adapter.Fill(ds);
                if (ds.Tables.Count > 0)
                {
                    foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                    {
                        FingerprintSignature fs;
                        if (useLookupHash)
                        {
                            fs = new FingerprintSignature(row["REFERENCE"].ToString(), Convert.ToInt64(row["ID"]),
                                (byte[])row["SIGNATURE"], (byte[])row["LOOKUPHASHES"], Convert.ToInt64(row["DURATION"]));
                        }
                        else
                        {
                            //TODO OLD CODE:
                            //fs = new FingerprintSignature(row["REFERENCE"].ToString(), Convert.ToInt64(row["ID"]), 
                            //    (byte[])row["SIGNATURE"], Convert.ToInt64(row["DURATION"]), true);
                            fs = new FingerprintSignature(row["REFERENCE"].ToString(), Convert.ToInt32(row["DR_DISKOTEKSNR"]), Convert.ToInt32(row["SIDENUMMER"]), Convert.ToInt32(row["SEKVENSNUMMER"]),
                                Convert.ToInt64(row["ID"]), (byte[])row["SIGNATURE"], Convert.ToInt64(row["DURATION"]), true);
                        }

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

        #endregion

        #region Helper functions
        public static byte[] GetBytes(string str)
        {
            if (str == null)
            {
                return new byte[0];
            }

            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            if (bytes == null)
            {
                return string.Empty;
            }

            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
        #endregion

        public class QueryPlanConfig
        {
            public SearchStrategy SearchStrategy = SearchStrategy.NotSet;

            public int maxSearchResult { get; set; }
            public int LowestBER = int.MaxValue;
            public int MinimalBERMatch = -1; // 
            public CancellationTokenSource Token;
            public FingerprintSignature fsQuery = null;

            public TimeSpan SubFingerCreateQueryTime = TimeSpan.FromTicks(0);
            public TimeSpan SubFingerQueryTime = TimeSpan.FromTicks(0);
            public TimeSpan FingerLoadTime = TimeSpan.FromTicks(0);
            public TimeSpan MatchTime = TimeSpan.FromTicks(0);

            public int SearchIteration = 0;

            public ConcurrentDictionary<string, FingerprintHit> Hits = new ConcurrentDictionary<string, FingerprintHit>();
        }
    }
}