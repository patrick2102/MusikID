﻿namespace AudioFingerprint.FFT.FFTW
{
    using System;
    using System.Runtime.InteropServices;

    using AudioFingerprint.FFT.FFTW.X86;
    
    public class FFTWService86 : FFTWService
    {
        private static object lockObject = new object();

        public override float[] FFTForward(float[] signal, int startIndex, int length)
        {
            IntPtr input = GetInput(length);
            IntPtr output = GetOutput(length);
            IntPtr fftPlan = GetFFTPlan(length, input, output);
            float[] applyTo = new float[length];
            Array.Copy(signal, startIndex, applyTo, 0, length);
            Marshal.Copy(applyTo, 0, input, length);
            // is this also neccesary?? (other lock are it seems!)
            lock (lockObject)
            {
                FFTWFNativeMethods.execute(fftPlan);
            }
            float[] result = new float[length * 2];
            Marshal.Copy(output, result, 0, length * 2);
            FreeUnmanagedMemory(input);
            FreeUnmanagedMemory(output);
            FreePlan(fftPlan);
            return result;
        }

        public override IntPtr GetOutput(int length)
        {
            lock (lockObject)
            {
                return FFTWFNativeMethods.malloc(8 * length);
            }
        }

        public override IntPtr GetInput(int length)
        {
            lock (lockObject)
            {
                return FFTWFNativeMethods.malloc(4 * length);
            }
        }

        public override IntPtr GetFFTPlan(int length, IntPtr input, IntPtr output)
        {
            lock (lockObject)
            {
                return FFTWFNativeMethods.dft_r2c_1d(length, input, output, InteropFFTWFlags.Estimate);
            }
        }

        public override void FreeUnmanagedMemory(IntPtr memoryBlock)
        {
            lock (lockObject)
            {
                FFTWFNativeMethods.free(memoryBlock);
            }
        }

        public override void FreePlan(IntPtr fftPlan)
        {
            lock (lockObject)
            {
                FFTWFNativeMethods.destroy_plan(fftPlan);
            }
        }

        public override void Execute(IntPtr fftPlan)
        {
            FFTWFNativeMethods.execute(fftPlan);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // release managed resources
            }

            // release unmanaged resources
        }
    }
}
