#region License
// Copyright (c) 2015-2017 Stichting Centrale Discotheek Rotterdam.
// 
// website: https://www.muziekweb.nl
// e-mail:  info@muziekweb.nl
//
// This code is under MIT licence, you can find the complete file here: 
// LICENSE.MIT
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CreateInversedFingerprintIndex
{
    class Program
    {
        static void Main(string[] args)
        {
            InversedWorker worker = new InversedWorker();
            worker.InitializeInversedID(false);
        }
    }
}