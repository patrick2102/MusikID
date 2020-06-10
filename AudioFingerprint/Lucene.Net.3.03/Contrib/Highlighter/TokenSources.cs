/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/*
* Created on 28-Oct-2004
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace Lucene.Net.Search.Highlight
{

    /// <summary> Hides implementation issues associated with obtaining a TokenStream for use with
    /// the higlighter - can obtain from TermFreqVectors with offsets and (optionally) positions or
    /// from Analyzer class reparsing the stored content. 
    /// </summary>
    public class TokenSources
    {
        public class StoredTokenStream : TokenStream
        {
            protected internal Token[] tokens;
            protected internal int currentToken = 0;
            protected internal ITermAttribute termAtt;
            protected internal IOffsetAttribute offsetAtt;

            protected internal StoredTokenStream(Token[] tokens)
            {
                this.tokens = tokens;
                termAtt = AddAttribute<ITermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            public override bool IncrementToken()
            {
                if (currentToken >= tokens.Length)
                {
                    return false;
                }
                ClearAttributes();
                Token token = tokens[currentToken++];
                termAtt.SetTermBuffer(token.Term);
                offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                // do nothing
            }
        }

        /// <summary>
        /// A convenience method that tries to first get a TermPositionVector for the specified docId, then, falls back to
        /// using the passed in {@link org.apache.lucene.document.Document} to retrieve the TokenStream.  This is useful when
        /// you already have the document, but would prefer to use the vector first.
        /// </summary>
        /// <param name="reader">The <see cref="IndexReader"/> to use to try and get the vector from</param>
        /// <param name="docId">The docId to retrieve.</param>
        /// <param name="field">The field to retrieve on the document</param>
        /// <param name="doc">The document to fall back on</param>
        /// <param name="analyzer">The analyzer to use for creating the TokenStream if the vector doesn't exist</param>
        /// <returns>The <see cref="TokenStream"/> for the <see cref="IFieldable"/> on the <see cref="Document"/></returns>
        /// <exception cref="IOException">if there was an error loading</exception>
        public static TokenStream GetAnyTokenStream(IndexReader reader, int docId, String field, Document doc,
                                                    Analyzer analyzer)
        {
            TokenStream ts = null;

            var tfv = reader.GetTermFreqVector(docId, field);
            if (tfv != null)
            {
                var termPositionVector = tfv as TermPositionVector;
                if (termPositionVector != null)
                {
                    ts = GetTokenStream(termPositionVector);
                }
            }
            //No token info stored so fall back to analyzing raw content
            return ts ?? GetTokenStream(doc, field, analyzer);
        }

        /// <summary>
        /// A convenience method that tries a number of approaches to getting a token stream.
        /// The cost of finding there are no termVectors in the index is minimal (1000 invocations still 
        /// registers 0 ms). So this "lazy" (flexible?) approach to coding is probably acceptable
        /// </summary>
        /// <returns>null if field not stored correctly</returns>
        public static TokenStream GetAnyTokenStream(IndexReader reader, int docId, String field, Analyzer analyzer)
        {
            TokenStream ts = null;

            var tfv = reader.GetTermFreqVector(docId, field);
            if (tfv != null)
            {
                var termPositionVector = tfv as TermPositionVector;
                if (termPositionVector != null)
                {
                    ts = GetTokenStream(termPositionVector);
                }
            }
            //No token info stored so fall back to analyzing raw content
            return ts ?? GetTokenStream(reader, docId, field, analyzer);
        }

        public static TokenStream GetTokenStream(TermPositionVector tpv)
        {
            //assumes the worst and makes no assumptions about token position sequences.
            return GetTokenStream(tpv, false);
        }

        /// <summary>
        /// Low level api.
        /// Returns a token stream or null if no offset info available in index.
        /// This can be used to feed the highlighter with a pre-parsed token stream 
        /// 
        /// In my tests the speeds to recreate 1000 token streams using this method are:
        /// - with TermVector offset only data stored - 420  milliseconds 
        /// - with TermVector offset AND position data stored - 271 milliseconds
        ///  (nb timings for TermVector with position data are based on a tokenizer with contiguous
        ///  positions - no overlaps or gaps)
        /// The cost of not using TermPositionVector to store
        /// pre-parsed content and using an analyzer to re-parse the original content: 
        /// - reanalyzing the original content - 980 milliseconds
        /// 
        /// The re-analyze timings will typically vary depending on -
        /// 	1) The complexity of the analyzer code (timings above were using a 
        /// 	   stemmer/lowercaser/stopword combo)
        ///  2) The  number of other fields (Lucene reads ALL fields off the disk 
        ///     when accessing just one document field - can cost dear!)
        ///  3) Use of compression on field storage - could be faster due to compression (less disk IO)
        ///     or slower (more CPU burn) depending on the content.
        /// </summary>
        /// <param name="tpv"/>
        /// <param name="tokenPositionsGuaranteedContiguous">true if the token position numbers have no overlaps or gaps. If looking
        /// to eek out the last drops of performance, set to true. If in doubt, set to false.</param>
        public static TokenStream GetTokenStream(TermPositionVector tpv, bool tokenPositionsGuaranteedContiguous)
        {
            //code to reconstruct the original sequence of Tokens
            String[] terms = tpv.GetTerms();
            int[] freq = tpv.GetTermFrequencies();

            int totalTokens = freq.Sum();

            var tokensInOriginalOrder = new Token[totalTokens];
            List<Token> unsortedTokens = null;
            for (int t = 0; t < freq.Length; t++)
            {
                TermVectorOffsetInfo[] offsets = tpv.GetOffsets(t);
                if (offsets == null)
                {
                    return null;
                }

                int[] pos = null;
                if (tokenPositionsGuaranteedContiguous)
                {
                    //try get the token position info to speed up assembly of tokens into sorted sequence
                    pos = tpv.GetTermPositions(t);
                }
                if (pos == null)
                {
                    //tokens NOT stored with positions or not guaranteed contiguous - must add to list and sort later
                    if (unsortedTokens == null)
                    {
                        unsortedTokens = new List<Token>();
                    }

                    foreach (TermVectorOffsetInfo t1 in offsets)
                    {
                        var token = new Token(t1.StartOffset, t1.EndOffset);
                        token.SetTermBuffer(terms[t]);
                        unsortedTokens.Add(token);
                    }
                }
                else
                {
                    //We have positions stored and a guarantee that the token position information is contiguous

                    // This may be fast BUT wont work if Tokenizers used which create >1 token in same position or
                    // creates jumps in position numbers - this code would fail under those circumstances

                    //tokens stored with positions - can use this to index straight into sorted array
                    for (int tp = 0; tp < pos.Length; tp++)
                    {
                        var token = new Token(terms[t], offsets[tp].StartOffset, offsets[tp].EndOffset);
                        tokensInOriginalOrder[pos[tp]] = token;
                    }
                }
            }
            //If the field has been stored without position data we must perform a sort        
            if (unsortedTokens != null)
            {
                tokensInOriginalOrder = unsortedTokens.ToArray();
                Array.Sort(tokensInOriginalOrder, (t1, t2) =>
                                                      {
                                                          if (t1.StartOffset > t2.EndOffset)
                                                              return 1;
                                                          if (t1.StartOffset < t2.StartOffset)
                                                              return -1;
                                                          return 0;
                                                      });
            }
            return new StoredTokenStream(tokensInOriginalOrder);
        }

        public static TokenStream GetTokenStream(IndexReader reader, int docId, System.String field)
        {
            var tfv = reader.GetTermFreqVector(docId, field);
            if (tfv == null)
            {
                throw new ArgumentException(field + " in doc #" + docId
                                            + "does not have any term position data stored");
            }
            if (tfv is TermPositionVector)
            {
                var tpv = (TermPositionVector) reader.GetTermFreqVector(docId, field);
                return GetTokenStream(tpv);
            }
            throw new ArgumentException(field + " in doc #" + docId
                                        + "does not have any term position data stored");
        }

        //convenience method
        public static TokenStream GetTokenStream(IndexReader reader, int docId, String field, Analyzer analyzer)
        {
            Document doc = reader.Document(docId);
            return GetTokenStream(doc, field, analyzer);
        }

        public static TokenStream GetTokenStream(Document doc, String field, Analyzer analyzer)
        {
            String contents = doc.Get(field);
            if (contents == null)
            {
                throw new ArgumentException("Field " + field + " in document is not stored and cannot be analyzed");
            }
            return GetTokenStream(field, contents, analyzer);
        }

        //convenience method
        public static TokenStream GetTokenStream(String field, String contents, Analyzer analyzer)
        {
            return analyzer.TokenStream(field, new StringReader(contents));
        }
    }
}