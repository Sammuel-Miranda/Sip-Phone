//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE.
//  This material may not be duplicated in whole or in part, except for 
//  personal use, without the express written consent of the author. 
//  Email: ianier@hotmail.com
//  Copyright (C) 1999-2003 Ianier Munoz. All Rights Reserved.
namespace WaveLib
{
    public delegate void BufferEventHandler(global::System.IntPtr data, int size);

    public class FifoStream : global::System.IO.Stream
    {
        private const int BlockSize = 65536;
        private const int MaxBlocksInCache = ((3 * 1024 * 1024) / global::WaveLib.FifoStream.BlockSize); //48
        private int m_Size;
        private int m_RPos;
        private int m_WPos;
        private global::System.Collections.Stack m_UsedBlocks = new global::System.Collections.Stack();
        private global::System.Collections.ArrayList m_Blocks = new global::System.Collections.ArrayList(); 
        private byte[] AllocBlock() { return this.m_UsedBlocks.Count > 0 ? (byte[])this.m_UsedBlocks.Pop() : new byte[global::WaveLib.FifoStream.BlockSize]; }
        private void FreeBlock(byte[] block) { if (this.m_UsedBlocks.Count < global::WaveLib.FifoStream.MaxBlocksInCache) { this.m_UsedBlocks.Push(block); } }

        private byte[] GetWBlock()
        {
            byte[] Result = null;
            if (this.m_WPos < global::WaveLib.FifoStream.BlockSize && this.m_Blocks.Count > 0) { Result = (byte[])this.m_Blocks[this.m_Blocks.Count - 1]; }
            else
            {
                Result = this.AllocBlock();
                this.m_Blocks.Add(Result);
                this.m_WPos = 0;
            }
            return Result;
        }

        public override void Flush()
        {
            lock(this)
            {
                foreach (byte[] block in this.m_Blocks) { this.FreeBlock(block); }
                this.m_Blocks.Clear();
                this.m_RPos = 0;
                this.m_WPos = 0;
                this.m_Size = 0;
            }
        }

        public override void Write(byte[] buf, int ofs, int count)
        {
            lock(this)
            {
                int Left = count;
                while (Left > 0)
                {
                    int ToWrite = global::System.Math.Min(global::WaveLib.FifoStream.BlockSize - this.m_WPos, Left);
                    global::System.Array.Copy(buf, ofs + count - Left, this.GetWBlock(), this.m_WPos, ToWrite);
                    this.m_WPos += ToWrite;
                    Left -= ToWrite;
                }
                this.m_Size += count;
            }
        }

        public int Advance(int count)
        {
            lock(this)
            {
                int SizeLeft = count;
                while (SizeLeft > 0 && this.m_Size > 0)
                {
                    if (this.m_RPos == global::WaveLib.FifoStream.BlockSize)
                    {
                        this.m_RPos = 0;
                        this.FreeBlock((byte[])this.m_Blocks[0]);
                        this.m_Blocks.RemoveAt(0);
                    }
                    int ToFeed = this.m_Blocks.Count == 1 ? global::System.Math.Min(this.m_WPos - this.m_RPos, SizeLeft) : global::System.Math.Min(global::WaveLib.FifoStream.BlockSize - this.m_RPos, SizeLeft);
                    this.m_RPos += ToFeed;
                    SizeLeft -= ToFeed;
                    this.m_Size -= ToFeed;
                }
                return count - SizeLeft;
            }
        }

        public int Peek(byte[] buf, int ofs, int count)
        {
            lock(this)
            {
                int SizeLeft = count;
                int TempBlockPos = this.m_RPos;
                int TempSize = this.m_Size;
                int CurrentBlock = 0;
                while (SizeLeft > 0 && TempSize > 0)
                {
                    if (TempBlockPos == BlockSize)
                    {
                        TempBlockPos = 0;
                        CurrentBlock++;
                    }
                    int Upper = CurrentBlock < this.m_Blocks.Count - 1 ? global::WaveLib.FifoStream.BlockSize : this.m_WPos;
                    int ToFeed = global::System.Math.Min(Upper - TempBlockPos, SizeLeft);
                    global::System.Array.Copy((byte[])this.m_Blocks[CurrentBlock], TempBlockPos, buf, ofs + count - SizeLeft, ToFeed);
                    SizeLeft -= ToFeed;
                    TempBlockPos += ToFeed;
                    TempSize -= ToFeed;
                }
                return count - SizeLeft;
            }
        }

        public override int Read(byte[] buf, int ofs, int count)
        {
            lock (this)
            {
                int Result = this.Peek(buf, ofs, count);
                this.Advance(Result);
                return Result;
            }
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { lock (this) { return this.m_Size; } } }
        public override long Position { get { throw new global::System.InvalidOperationException(); } set { throw new global::System.InvalidOperationException(); } }
        public override void Close() { this.Flush(); }
        public override void SetLength(long len) { throw new global::System.InvalidOperationException(); }
        public override long Seek(long pos, global::System.IO.SeekOrigin o) { throw new global::System.InvalidOperationException(); }
    }

    public enum WaveFormats : int
    {
        Pcm = 1,
        Float = 3
    }

    [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)] public class WaveFormat
    {
        public short wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;

        public WaveFormat(int rate, int bits, int channels)
        {
            this.wFormatTag = (short)global::WaveLib.WaveFormats.Pcm;
            this.nChannels = (short)channels;
            this.nSamplesPerSec = rate;
            this.wBitsPerSample = (short)bits;
            this.cbSize = 0;
            this.nBlockAlign = (short)(channels * (bits / 8));
            this.nAvgBytesPerSec = this.nSamplesPerSec * this.nBlockAlign;
        }
    }

    public class WaveStream : global::System.IO.Stream, global::System.IDisposable
    {
        private global::System.IO.Stream m_Stream;
        private long m_DataPos;
        private long m_Length;
        private global::WaveLib.WaveFormat m_Format;
        public global::WaveLib.WaveFormat Format { get { return this.m_Format; } }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this.m_Stream != null) { this.m_Stream.Close(); }
            global::System.GC.SuppressFinalize(this);
        }

        public override long Seek(long pos, global::System.IO.SeekOrigin o)
        {
            switch (o)
            {
                case global::System.IO.SeekOrigin.Begin: this.m_Stream.Position = pos + this.m_DataPos; break;
                case global::System.IO.SeekOrigin.Current: this.m_Stream.Seek(pos, global::System.IO.SeekOrigin.Current); break;
                case global::System.IO.SeekOrigin.End: this.m_Stream.Position = this.m_DataPos + this.m_Length - pos; break;
            }
            return this.Position;
        }

        public override long Position { get { return this.m_Stream.Position - this.m_DataPos; } set { this.Seek(value, global::System.IO.SeekOrigin.Begin); } }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return this.m_Length; } }
        public override void Close() { this.Dispose(); }
        public override void Flush() { /* NOTHING */ }
        public override void SetLength(long len) { throw new global::System.InvalidOperationException(); }
        public override int Read(byte[] buf, int ofs, int count) { return m_Stream.Read(buf, ofs, (int)global::System.Math.Min(count, this.m_Length - Position)); }
        public override void Write(byte[] buf, int ofs, int count) { throw new global::System.InvalidOperationException(); }

        private string ReadChunk(global::System.IO.BinaryReader reader)
        {
            byte[] ch = new byte[4];
            reader.Read(ch, 0, ch.Length);
            return global::System.Text.Encoding.ASCII.GetString(ch);
        }

        private void ReadHeader()
        {
            global::System.IO.BinaryReader Reader = new global::System.IO.BinaryReader(this.m_Stream);
            if (this.ReadChunk(Reader) != "RIFF") { throw new global::System.Exception("Invalid file format"); }
            Reader.ReadInt32();
            if (this.ReadChunk(Reader) != "WAVE") { throw new global::System.Exception("Invalid file format"); }
            if (this.ReadChunk(Reader) != "fmt ") { throw new global::System.Exception("Invalid file format"); }
            int len = Reader.ReadInt32();
            if (len < 16) { throw new global::System.Exception("Invalid file format"); }
            this.m_Format = new global::WaveLib.WaveFormat(22050, 16, 2);
            this.m_Format.wFormatTag = Reader.ReadInt16();
            this.m_Format.nChannels = Reader.ReadInt16();
            this.m_Format.nSamplesPerSec = Reader.ReadInt32();
            this.m_Format.nAvgBytesPerSec = Reader.ReadInt32();
            this.m_Format.nBlockAlign = Reader.ReadInt16();
            this.m_Format.wBitsPerSample = Reader.ReadInt16();
            len -= 16;
            while (len > 0)
            {
                Reader.ReadByte();
                len--;
            }
            while (this.m_Stream.Position < this.m_Stream.Length && this.ReadChunk(Reader) != "data") ;
            if (this.m_Stream.Position >= this.m_Stream.Length) { throw new global::System.Exception("Invalid file format"); }
            this.m_Length = Reader.ReadInt32();
            this.m_DataPos = this.m_Stream.Position;
            this.Position = 0;
        }

        public WaveStream(global::System.IO.Stream S)
        {
            this.m_Stream = S;
            this.ReadHeader();
        }

        public WaveStream(string fileName) : this(new global::System.IO.FileStream(fileName, global::System.IO.FileMode.Open)) { /* NOTHING */ }
        ~WaveStream() { this.Dispose(); }
    }

    internal class WaveNative
    {
        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)] public struct WaveHdr
        {
            public global::System.IntPtr lpData; // pointer to locked data buffer
            public int dwBufferLength; // length of data buffer
            public int dwBytesRecorded; // used for input only
            public global::System.IntPtr dwUser; // for client's use
            public int dwFlags; // assorted flags (see defines)
            public int dwLoops; // loop control counter
            public global::System.IntPtr lpNext; // PWaveHdr, reserved for driver
            public int reserved; // reserved for driver
        }

        private const string mmdll = "winmm.dll";
        public const int MMSYSERR_NOERROR = 0;
        public const int MM_WOM_OPEN = 0x3BB;
        public const int MM_WOM_CLOSE = 0x3BC;
        public const int MM_WOM_DONE = 0x3BD;
        public const int MM_WIM_OPEN = 0x3BE;
        public const int MM_WIM_CLOSE = 0x3BF;
        public const int MM_WIM_DATA = 0x3C0;
        public const int CALLBACK_FUNCTION = 0x00030000; // dwCallback is a FARPROC 
        public const int TIME_MS = 0x0001;  // time in milliseconds 
        public const int TIME_SAMPLES = 0x0002;  // number of wave samples 
        public const int TIME_BYTES = 0x0004;  // current byte offset 
        public delegate void WaveDelegate(global::System.IntPtr hdrvr, int uMsg, int dwUser, ref global::WaveLib.WaveNative.WaveHdr wavhdr, int dwParam2);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutGetNumDevs();
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutPrepareHeader(global::System.IntPtr hWaveOut, ref global::WaveLib.WaveNative.WaveHdr lpWaveOutHdr, int uSize);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutUnprepareHeader(global::System.IntPtr hWaveOut, ref global::WaveLib.WaveNative.WaveHdr lpWaveOutHdr, int uSize);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutWrite(global::System.IntPtr hWaveOut, ref global::WaveLib.WaveNative.WaveHdr lpWaveOutHdr, int uSize);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutOpen(out global::System.IntPtr hWaveOut, int uDeviceID, global::WaveLib.WaveFormat lpFormat, global::WaveLib.WaveNative.WaveDelegate dwCallback, int dwInstance, int dwFlags);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutReset(global::System.IntPtr hWaveOut);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutClose(global::System.IntPtr hWaveOut);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutPause(global::System.IntPtr hWaveOut);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutRestart(global::System.IntPtr hWaveOut);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutGetPosition(global::System.IntPtr hWaveOut, out int lpInfo, int uSize);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutSetVolume(global::System.IntPtr hWaveOut, int dwVolume);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveOutGetVolume(global::System.IntPtr hWaveOut, out int dwVolume);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInGetNumDevs();
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInAddBuffer(global::System.IntPtr hwi, ref global::WaveLib.WaveNative.WaveHdr pwh, int cbwh);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInClose(global::System.IntPtr hwi);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInOpen(out global::System.IntPtr phwi, int uDeviceID, global::WaveLib.WaveFormat lpFormat, global::WaveLib.WaveNative.WaveDelegate dwCallback, int dwInstance, int dwFlags);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInPrepareHeader(global::System.IntPtr hWaveIn, ref global::WaveLib.WaveNative.WaveHdr lpWaveInHdr, int uSize);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInUnprepareHeader(global::System.IntPtr hWaveIn, ref global::WaveLib.WaveNative.WaveHdr lpWaveInHdr, int uSize);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInReset(global::System.IntPtr hwi);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInStart(global::System.IntPtr hwi);
        [global::System.Runtime.InteropServices.DllImport(global::WaveLib.WaveNative.mmdll)] public static extern int waveInStop(global::System.IntPtr hwi);
        public static void Try(int err) { if (err != global::WaveLib.WaveNative.MMSYSERR_NOERROR) { throw new global::System.Exception(err.ToString()); } }

        public class WaveInBuffer : global::System.IDisposable
        {
            public global::WaveLib.WaveNative.WaveInBuffer NextBuffer;
            private global::System.Threading.AutoResetEvent m_RecordEvent;
            private global::System.IntPtr m_WaveIn;
            private global::WaveLib.WaveNative.WaveHdr m_Header;
            private byte[] m_HeaderData;
            private global::System.Runtime.InteropServices.GCHandle m_HeaderHandle;
            private global::System.Runtime.InteropServices.GCHandle m_HeaderDataHandle;
            private bool m_Recording;
            public int Size { get { return this.m_Header.dwBufferLength; } }
            public global::System.IntPtr Data { get { return this.m_Header.lpData; } }
            public void WaitFor() { if (this.m_Recording) { this.m_Recording = this.m_RecordEvent.WaitOne(); } else { global::System.Threading.Thread.Sleep(0); } }

            private void OnCompleted()
            {
                this.m_RecordEvent.Set();
                this.m_Recording = false;
            }

            internal static void WaveInProc(global::System.IntPtr hdrvr, int uMsg, int dwUser, ref global::WaveLib.WaveNative.WaveHdr wavhdr, int dwParam2)
            {
                if (uMsg == global::WaveLib.WaveNative.MM_WIM_DATA)
                {
                    try
                    {
                        global::System.Runtime.InteropServices.GCHandle h = (global::System.Runtime.InteropServices.GCHandle)wavhdr.dwUser;
                        (h.Target as global::WaveLib.WaveNative.WaveInBuffer).OnCompleted();
                    } catch { /* NOTHING */ }
                }
            }

            public void Dispose()
            {
                if (this.m_Header.lpData != global::System.IntPtr.Zero)
                {
                    global::WaveLib.WaveNative.waveInUnprepareHeader(m_WaveIn, ref this.m_Header, global::System.Runtime.InteropServices.Marshal.SizeOf(this.m_Header));
                    this.m_HeaderHandle.Free();
                    this.m_Header.lpData = global::System.IntPtr.Zero;
                }
                this.m_RecordEvent.Close();
                if (this.m_HeaderDataHandle.IsAllocated) { this.m_HeaderDataHandle.Free(); }
                global::System.GC.SuppressFinalize(this);
            }

            public bool Record()
            {
                lock (this)
                {
                    this.m_RecordEvent.Reset();
                    this.m_Recording = global::WaveLib.WaveNative.waveInAddBuffer(this.m_WaveIn, ref this.m_Header, global::System.Runtime.InteropServices.Marshal.SizeOf(this.m_Header)) == global::WaveLib.WaveNative.MMSYSERR_NOERROR;
                    return this.m_Recording;
                }
            }

            public WaveInBuffer(global::System.IntPtr waveInHandle, int size)
            {
                this.m_RecordEvent = new global::System.Threading.AutoResetEvent(false);
                this.m_WaveIn = waveInHandle;
                this.m_HeaderHandle = global::System.Runtime.InteropServices.GCHandle.Alloc(m_Header, global::System.Runtime.InteropServices.GCHandleType.Pinned);
                this.m_Header.dwUser = (global::System.IntPtr)global::System.Runtime.InteropServices.GCHandle.Alloc(this);
                this.m_HeaderData = new byte[size];
                this.m_HeaderDataHandle = global::System.Runtime.InteropServices.GCHandle.Alloc(m_HeaderData, global::System.Runtime.InteropServices.GCHandleType.Pinned);
                this.m_Header.lpData = m_HeaderDataHandle.AddrOfPinnedObject();
                this.m_Header.dwBufferLength = size;
                global::WaveLib.WaveNative.Try(global::WaveLib.WaveNative.waveInPrepareHeader(this.m_WaveIn, ref this.m_Header, global::System.Runtime.InteropServices.Marshal.SizeOf(this.m_Header)));
            }

            ~WaveInBuffer() { this.Dispose(); }
        }

        internal class WaveOutBuffer : global::System.IDisposable
        {
            public global::WaveLib.WaveNative.WaveOutBuffer NextBuffer;
            private global::System.Threading.AutoResetEvent m_PlayEvent;
            private global::System.IntPtr m_WaveOut;
            private WaveNative.WaveHdr m_Header;
            private byte[] m_HeaderData;
            private global::System.Runtime.InteropServices.GCHandle m_HeaderHandle;
            private global::System.Runtime.InteropServices.GCHandle m_HeaderDataHandle;
            private bool m_Playing;
            public int Size { get { return this.m_Header.dwBufferLength; } }
            public global::System.IntPtr Data { get { return this.m_Header.lpData; } }
            public void WaitFor() { if (this.m_Playing) { this.m_Playing = this.m_PlayEvent.WaitOne(); } else { global::System.Threading.Thread.Sleep(0); } }

            private void OnCompleted()
            {
                this.m_PlayEvent.Set();
                this.m_Playing = false;
            }

            public bool Play()
            {
                lock (this)
                {
                    this.m_PlayEvent.Reset();
                    this.m_Playing = global::WaveLib.WaveNative.waveOutWrite(this.m_WaveOut, ref this.m_Header, global::System.Runtime.InteropServices.Marshal.SizeOf(this.m_Header)) == WaveNative.MMSYSERR_NOERROR;
                    return this.m_Playing;
                }
            }

            internal static void WaveOutProc(global::System.IntPtr hdrvr, int uMsg, int dwUser, ref global::WaveLib.WaveNative.WaveHdr wavhdr, int dwParam2)
            {
                if (uMsg == global::WaveLib.WaveNative.MM_WOM_DONE)
                {
                    try
                    {
                        global::System.Runtime.InteropServices.GCHandle h = (global::System.Runtime.InteropServices.GCHandle)wavhdr.dwUser;
                        (h.Target as global::WaveLib.WaveNative.WaveOutBuffer).OnCompleted();
                    } catch { /* NOTHING */ }
                }
            }

            public void Dispose()
            {
                if (this.m_Header.lpData != global::System.IntPtr.Zero)
                {
                    global::WaveLib.WaveNative.waveOutUnprepareHeader(this.m_WaveOut, ref this.m_Header, global::System.Runtime.InteropServices.Marshal.SizeOf(this.m_Header));
                    this.m_HeaderHandle.Free();
                    this.m_Header.lpData = global::System.IntPtr.Zero;
                }
                this.m_PlayEvent.Close();
                if (this.m_HeaderDataHandle.IsAllocated) { this.m_HeaderDataHandle.Free(); }
                global::System.GC.SuppressFinalize(this);
            }

            public WaveOutBuffer(global::System.IntPtr waveOutHandle, int size)
            {
                this.m_PlayEvent = new global::System.Threading.AutoResetEvent(false);
                this.m_WaveOut = waveOutHandle;
                this.m_HeaderHandle = global::System.Runtime.InteropServices.GCHandle.Alloc(this.m_Header, global::System.Runtime.InteropServices.GCHandleType.Pinned);
                this.m_Header.dwUser = (global::System.IntPtr)global::System.Runtime.InteropServices.GCHandle.Alloc(this);
                this.m_HeaderData = new byte[size];
                this.m_HeaderDataHandle = global::System.Runtime.InteropServices.GCHandle.Alloc(this.m_HeaderData, global::System.Runtime.InteropServices.GCHandleType.Pinned);
                this.m_Header.lpData = this.m_HeaderDataHandle.AddrOfPinnedObject();
                this.m_Header.dwBufferLength = size;
                global::WaveLib.WaveNative.Try(global::WaveLib.WaveNative.waveOutPrepareHeader(this.m_WaveOut, ref m_Header, global::System.Runtime.InteropServices.Marshal.SizeOf(this.m_Header)));
            }

            ~WaveOutBuffer() { this.Dispose(); }
        }
    }

    public class WaveInRecorder : global::System.IDisposable
    {
        private global::System.IntPtr m_WaveIn;
        private global::WaveLib.WaveNative.WaveInBuffer m_Buffers;
        private global::WaveLib.WaveNative.WaveInBuffer m_CurrentBuffer;
        private global::System.Threading.Thread m_Thread;
        private global::WaveLib.BufferEventHandler m_DoneProc;
        private bool m_Finished;
        private global::WaveLib.WaveNative.WaveDelegate m_BufferProc;
        public static int DeviceCount { get { return global::WaveLib.WaveNative.waveInGetNumDevs(); } }
        private void SelectNextBuffer() { this.m_CurrentBuffer = this.m_CurrentBuffer == null ? this.m_Buffers : this.m_CurrentBuffer.NextBuffer; }

        private void Advance()
        {
            this.SelectNextBuffer();
            this.m_CurrentBuffer.WaitFor();
        }

        private void FreeBuffers()
        {
            this.m_CurrentBuffer = null;
            if (this.m_Buffers != null)
            {
                global::WaveLib.WaveNative.WaveInBuffer First = this.m_Buffers;
                this.m_Buffers = null;
                global::WaveLib.WaveNative.WaveInBuffer Current = First;
                do
                {
                    global::WaveLib.WaveNative.WaveInBuffer Next = Current.NextBuffer;
                    Current.Dispose();
                    Current = Next;
                } while (Current != First);
            }
        }

        private void AllocateBuffers(int bufferSize, int bufferCount)
        {
            this.FreeBuffers();
            if (bufferCount > 0)
            {
                this.m_Buffers = new global::WaveLib.WaveNative.WaveInBuffer(this.m_WaveIn, bufferSize);
                global::WaveLib.WaveNative.WaveInBuffer Prev = this.m_Buffers;
                try
                {
                    for (int i = 1; i < bufferCount; i++)
                    {
                        global::WaveLib.WaveNative.WaveInBuffer Buf = new global::WaveLib.WaveNative.WaveInBuffer(this.m_WaveIn, bufferSize);
                        Prev.NextBuffer = Buf;
                        Prev = Buf;
                    }
                } finally { Prev.NextBuffer = this.m_Buffers; }
            }
        }

        private void WaitForAllBuffers()
        {
            global::WaveLib.WaveNative.WaveInBuffer Buf = this.m_Buffers;
            while (Buf.NextBuffer != this.m_Buffers)
            {
                Buf.WaitFor();
                Buf = Buf.NextBuffer;
            }
        }

        private void ThreadProc()
        {
            while (!this.m_Finished)
            {
                this.Advance();
                if (this.m_DoneProc != null && !this.m_Finished) { this.m_DoneProc(this.m_CurrentBuffer.Data, this.m_CurrentBuffer.Size); }
                this.m_CurrentBuffer.Record();
            }
        }

        public void Dispose()
        {
            if (this.m_Thread != null)
            {
                try
                {
                    this.m_Finished = true;
                    if (this.m_WaveIn != global::System.IntPtr.Zero) { global::WaveLib.WaveNative.waveInReset(this.m_WaveIn); }
                    this.WaitForAllBuffers();
                    this.m_Thread.Join();
                    this.m_DoneProc = null;
                    this.FreeBuffers();
                    if (this.m_WaveIn != global::System.IntPtr.Zero) { global::WaveLib.WaveNative.waveInClose(this.m_WaveIn); }
                }
                finally
                {
                    this.m_Thread = null;
                    this.m_WaveIn = global::System.IntPtr.Zero;
                }
            }
            global::System.GC.SuppressFinalize(this);
        }

        public WaveInRecorder(int device, global::WaveLib.WaveFormat format, int bufferSize, int bufferCount, global::WaveLib.BufferEventHandler doneProc)
        {
            this.m_BufferProc = new global::WaveLib.WaveNative.WaveDelegate(global::WaveLib.WaveNative.WaveInBuffer.WaveInProc);
            this.m_DoneProc = doneProc;
            global::WaveLib.WaveNative.Try(global::WaveLib.WaveNative.waveInOpen(out this.m_WaveIn, device, format, this.m_BufferProc, 0, global::WaveLib.WaveNative.CALLBACK_FUNCTION));
            this.AllocateBuffers(bufferSize, bufferCount);
            for (int i = 0; i < bufferCount; i++)
            {
                this.SelectNextBuffer();
                this.m_CurrentBuffer.Record();
            }
            global::WaveLib.WaveNative.Try(global::WaveLib.WaveNative.waveInStart(this.m_WaveIn));
            this.m_Thread = new global::System.Threading.Thread(new global::System.Threading.ThreadStart(this.ThreadProc));
            this.m_Thread.Start();
        }

        ~WaveInRecorder() { this.Dispose(); }
    }

    public class WaveOutPlayer : global::System.IDisposable
    {
        private global::System.IntPtr m_WaveOut;
        private global::WaveLib.WaveNative.WaveOutBuffer m_Buffers;
        private global::WaveLib.WaveNative.WaveOutBuffer m_CurrentBuffer;
        private global::System.Threading.Thread m_Thread;
        private global::WaveLib.BufferEventHandler m_FillProc;
        private bool m_Finished;
        private byte m_zero;
        private global::WaveLib.WaveNative.WaveDelegate m_BufferProc;
        public static int DeviceCount { get { return global::WaveLib.WaveNative.waveOutGetNumDevs(); } }

        private void Advance()
        {
            this.m_CurrentBuffer = this.m_CurrentBuffer == null ? this.m_Buffers : this.m_CurrentBuffer.NextBuffer;
            this.m_CurrentBuffer.WaitFor();
        }

        private void FreeBuffers()
        {
            this.m_CurrentBuffer = null;
            if (this.m_Buffers != null)
            {
                global::WaveLib.WaveNative.WaveOutBuffer First = this.m_Buffers;
                this.m_Buffers = null;
                global::WaveLib.WaveNative.WaveOutBuffer Current = First;
                do
                {
                    global::WaveLib.WaveNative.WaveOutBuffer Next = Current.NextBuffer;
                    Current.Dispose();
                    Current = Next;
                } while (Current != First);
            }
        }

        private void AllocateBuffers(int bufferSize, int bufferCount)
        {
            this.FreeBuffers();
            if (bufferCount > 0)
            {
                this.m_Buffers = new global::WaveLib.WaveNative.WaveOutBuffer(this.m_WaveOut, bufferSize);
                global::WaveLib.WaveNative.WaveOutBuffer Prev = this.m_Buffers;
                try
                {
                    for (int i = 1; i < bufferCount; i++)
                    {
                        global::WaveLib.WaveNative.WaveOutBuffer Buf = new global::WaveLib.WaveNative.WaveOutBuffer(this.m_WaveOut, bufferSize);
                        Prev.NextBuffer = Buf;
                        Prev = Buf;
                    }
                } finally { Prev.NextBuffer = this.m_Buffers; }
            }
        }

        private void WaitForAllBuffers()
        {
            global::WaveLib.WaveNative.WaveOutBuffer Buf = this.m_Buffers;
            while (Buf.NextBuffer != this.m_Buffers)
            {
                Buf.WaitFor();
                Buf = Buf.NextBuffer;
            }
        }

        private void ThreadProc()
        {
            while (!this.m_Finished)
            {
                this.Advance();
                if (this.m_FillProc != null && !this.m_Finished) { this.m_FillProc(this.m_CurrentBuffer.Data, this.m_CurrentBuffer.Size); }
                else
                {
                    byte v = this.m_zero;
                    byte[] b = new byte[this.m_CurrentBuffer.Size];
                    for (int i = 0; i < b.Length; i++) { b[i] = v; }
                    global::System.Runtime.InteropServices.Marshal.Copy(b, 0, this.m_CurrentBuffer.Data, b.Length);
                }
                this.m_CurrentBuffer.Play();
            }
            this.WaitForAllBuffers();
        }

        public void Dispose()
        {
            if (this.m_Thread != null)
            {
                try
                {
                    this.m_Finished = true;
                    if (this.m_WaveOut != global::System.IntPtr.Zero) { global::WaveLib.WaveNative.waveOutReset(this.m_WaveOut); }
                    this.m_Thread.Join();
                    this.m_FillProc = null;
                    this.FreeBuffers();
                    if (this.m_WaveOut != global::System.IntPtr.Zero) { global::WaveLib.WaveNative.waveOutClose(this.m_WaveOut); }
                }
                finally
                {
                    this.m_Thread = null;
                    this.m_WaveOut = global::System.IntPtr.Zero;
                }
            }
            global::System.GC.SuppressFinalize(this);
        }

        public WaveOutPlayer(int device, global::WaveLib.WaveFormat format, int bufferSize, int bufferCount, global::WaveLib.BufferEventHandler fillProc)
        {
            this.m_BufferProc = new global::WaveLib.WaveNative.WaveDelegate(global::WaveLib.WaveNative.WaveOutBuffer.WaveOutProc);
            this.m_zero = format.wBitsPerSample == 8 ? (byte)128 : (byte)0;
            this.m_FillProc = fillProc;
            global::WaveLib.WaveNative.Try(global::WaveLib.WaveNative.waveOutOpen(out this.m_WaveOut, device, format, this.m_BufferProc, 0, WaveNative.CALLBACK_FUNCTION));
            this.AllocateBuffers(bufferSize, bufferCount);
            this.m_Thread = new global::System.Threading.Thread(new global::System.Threading.ThreadStart(this.ThreadProc));
            this.m_Thread.Start();
        }

        ~WaveOutPlayer() { this.Dispose(); }
    }
}
