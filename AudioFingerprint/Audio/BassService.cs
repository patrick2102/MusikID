﻿namespace AudioFingerprint.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Un4seen.Bass;
    using Un4seen.Bass.AddOn.Fx;
    using Un4seen.Bass.AddOn.Mix;
    using Un4seen.Bass.AddOn.Tags;

    public class BassService : IDisposable
    {
        private BassLifetimeManager lifetimeManager;
        private static object lockObject = new object();

        private bool alreadyDisposed;

        public BassService(bool initializeRecordDevice = false)
        {
            alreadyDisposed = false;
            lock (lockObject)
            {
                lifetimeManager = new BassLifetimeManager(initializeRecordDevice);
            }
        }

        ~BassService()
        {
            Dispose(false);
        }

        public bool RecordDeviceAvailable
        {
            get
            {
                return lifetimeManager.RecordDeviceAvailable;
            }
        }

        public void RegisterBass(string email, string registrationKey)
        {
            BassNet.Registration(email, registrationKey);
        }

        public bool BassLoadMe(string targetPath)
        {
            return Bass.LoadMe(targetPath);
        }

        public bool BassMixLoadMe(string targetPath)
        {
            return BassMix.LoadMe(targetPath);
        }

        public bool BassFxLoadMe(string targetPath)
        {
            return BassFx.LoadMe(targetPath);
        }

        public int GetVersion()
        {
            return Bass.BASS_GetVersion();
        }

        public int GetMixerVersion()
        {
            return BassMix.BASS_Mixer_GetVersion();
        }

        public int GetFxVersion()
        {
            return BassFx.BASS_FX_GetVersion();
        }

        public IDictionary<int, string> PluginLoadDirectory(string path)
        {
            return Bass.BASS_PluginLoadDirectory(path);
        }

        public bool Init(int deviceNumber, int sampleRate, BASSInit flags)
        {
            return Bass.BASS_Init(deviceNumber, sampleRate, flags, IntPtr.Zero);
        }

        public bool SetConfig(BASSConfig config, int value)
        {
            return Bass.BASS_SetConfig(config, value);
        }

        public bool SetConfig(BASSConfig config, bool value)
        {
            return Bass.BASS_SetConfig(config, value);
        }

        public bool RecordInit(int deviceNumber)
        {
            return Bass.BASS_RecordInit(deviceNumber);
        }

        public string GetLastError()
        {
            return Bass.BASS_ErrorGetCode().ToString();
        }

        public int GetRecordingDevice()
        {
            return Bass.BASS_RecordGetDevice();
        }

        public int CreateStream(string pathToAudioFile, BASSFlag flags)
        {
            lock (lockObject)
            {
                return Bass.BASS_StreamCreateFile(pathToAudioFile, 0, 0, flags);
            }
        }

        public int CreateStreamFromUrl(string urlToResource, BASSFlag flags)
        {
            return Bass.BASS_StreamCreateURL(urlToResource, 0, flags, null, IntPtr.Zero);
        }

        public int StartRecording(int sampleRate, int numberOfChannels, BASSFlag flags)
        {
            return Bass.BASS_RecordStart(sampleRate, numberOfChannels, flags, null, IntPtr.Zero);
        }

        public bool StartPlaying(int stream)
        {
            return Bass.BASS_ChannelPlay(stream, false);
        }

        public int CreateMixerStream(int sampleRate, int channels, BASSFlag flags)
        {
            return BassMix.BASS_Mixer_StreamCreate(sampleRate, channels, flags);
        }
        
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCritical]
        public bool CombineMixerStreams(int mixerStream, int stream, BASSFlag flags)
        {
            return BassMix.BASS_Mixer_StreamAddChannel(mixerStream, stream, flags);
        }

        public bool ChannelSetPosition(int stream, int seekToSecond)
        {
            return Bass.BASS_ChannelSetPosition(stream, (double)seekToSecond);
        }

        public int ChannelGetData(int stream, float[] buffer, int lengthInBytes)
        {
            return Bass.BASS_ChannelGetData(stream, buffer, lengthInBytes);
        }

        public bool FreeStream(int stream)
        {
            if (!Bass.BASS_StreamFree(stream))
            {
                Trace.WriteLine("Could not release stream " + stream + ". Possible memory leak! Bass Error: " + GetLastError(), "Error");
                return false;
            }

            return true;
        }

        public bool PluginFree(int number)
        {
            return Bass.BASS_PluginFree(number);
        }

        public bool BassFree()
        {
            return Bass.BASS_Free();
        }

        public TAG_INFO GetTagsFromFile(string pathToFile)
        {
            return BassTags.BASS_TAG_GetFromFile(pathToFile);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            alreadyDisposed = true;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!alreadyDisposed && lifetimeManager != null)
            {
                lifetimeManager.Dispose();
                lifetimeManager = null;
            }
        }
    }
}