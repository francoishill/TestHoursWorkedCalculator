/*--------------------------------------------------------
 * MainForm.cs - (c) Mohammad Elsheimy, 2010
 * http://JustLikeAMagic.WordPress.com
  --------------------------------------------------------*/

using System;
using System.Runtime.InteropServices;
using System.Text;

//namespace Geming.SimpleRec
namespace SharedClasses
{
	internal static class SafeNativeMethods
	{
		// Constants

		public const string WaveAudio = "waveaudio";

		public const uint MM_MCINOTIFY = 0x3B9;

		public const uint MCI_NOTIFY_SUCCESSFUL = 0x0001;
		public const uint MCI_NOTIFY_SUPERSEDED = 0x0002;
		public const uint MCI_NOTIFY_ABORTED = 0x0004;
		public const uint MCI_NOTIFY_FAILURE = 0x0008;

		public const uint MCI_OPEN = 0x0803;
		public const uint MCI_CLOSE = 0x0804;
		public const uint MCI_PLAY = 0x0806;
		public const uint MCI_SEEK = 0x0807;
		public const uint MCI_STOP = 0x0808;
		public const uint MCI_PAUSE = 0x0809;

		public const uint MCI_SET = 0x80D;
		public const uint MCI_WAVE_SET_BITSPERSAMPLE = 0x200000;
		public const uint MCI_WAVE_SET_CHANNELS = 0x20000;
		public const uint MCI_WAVE_SET_SAMPLESPERSEC = 0x40000;

		public const uint MCI_RECORD = 0x080F;
		public const uint MCI_RESUME = 0x0855;
		public const uint MCI_SAVE = 0x0813;
		public const uint MCI_LOAD = 0x0850;
		public const uint MCI_STATUS = 0x0814;


		public const uint MCI_SAVE_FILE = 0x00000100;
		public const uint MCI_OPEN_ELEMENT = 0x00000200;
		public const uint MCI_OPEN_TYPE = 0x00002000;
		public const uint MCI_LOAD_FILE = 0x00000100;
		public const uint MCI_STATUS_POSITION = 0x00000002;
		public const uint MCI_STATUS_LENGTH = 0x00000001;
		public const uint MCI_STATUS_ITEM = 0x00000100;

		public const uint MCI_NOTIFY = 0x00000001;
		public const uint MCI_WAIT = 0x00000002;
		public const uint MCI_FROM = 0x00000004;
		public const uint MCI_TO = 0x00000008;


		// Structures

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct MCI_OPEN_PARMS
		{
			public IntPtr dwCallback;
			public uint wDeviceID;
			public IntPtr lpstrDeviceType;
			public IntPtr lpstrElementName;
			public IntPtr lpstrAlias;
		}


		//[StructLayout(LayoutKind.Sequential)] // in docs for compatability with C++ type of struct 
		//public struct MCI_RECORD_PARMS
		//{
		//    public uint dwCallback;
		//    public uint dwTimeFormat;
		//    public uint dwAudio;
		//    public uint wInput;
		//    public uint wOutput;
		//    public ushort wFormatTag; //As Integer 
		//    public ushort wReserved2; //As Integer 
		//    public ushort nChannels;
		//    public ushort wReserved3; //As Integer 
		//    public uint nSamplesPerSec;
		//    public uint nAvgBytesPerSec;
		//    public ushort nBlockAlign;
		//    public ushort wReserved4;
		//    public ushort wBitsPerSample;
		//    public ushort wReserved5;
		//} 
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct MCI_RECORD_PARMS
		{
			public IntPtr dwCallback;
			public uint dwFrom;
			public uint dwTo;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct MCI_PLAY_PARMS
		{
			public IntPtr dwCallback;
			public uint dwFrom;
			public uint dwTo;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct MCI_GENERIC_PARMS
		{
			public IntPtr dwCallback;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct MCI_SEEK_PARMS
		{
			public IntPtr dwCallback;
			public uint dwTo;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct MCI_SAVE_PARMS
		{
			public IntPtr dwCallback;
			public IntPtr lpfilename;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct MCI_STATUS_PARMS
		{
			public IntPtr dwCallback;
			public uint dwReturn;
			public uint dwItem;
			public uint dwTrack;
		} ;

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct WaveInCaps
		{
			public short wMid;
			public short wPid;
			public int vDriverVersion;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public char[] szPname;
			public uint dwFormats;
			public short wChannels;
			public short wReserved1;
		}

		[StructLayout(LayoutKind.Sequential)] // in docs for compatability with C++ type of struct 
		public struct MCI_WAVE_SET_PARMS
		{
			public uint dwCallback;
			public uint dwTimeFormat;
			public uint dwAudio;
			public uint wInput;
			public uint wOutput;
			public ushort wFormatTag; //As Integer 
			public ushort wReserved2; //As Integer 
			public ushort nChannels;
			public ushort wReserved3; //As Integer 
			public uint nSamplesPerSec;
			public uint nAvgBytesPerSec;
			public ushort nBlockAlign;
			public ushort wReserved4;
			public ushort wBitsPerSample;
			public ushort wReserved5;
		}

		// Functions

		[DllImport("winmm.dll", CharSet = CharSet.Ansi,
			BestFitMapping = true, ThrowOnUnmappableChar = true)]
		[return: MarshalAs(UnmanagedType.U4)]
		public static extern uint mciSendCommand(
			uint mciId,
			uint uMsg,
			uint dwParam1,
			IntPtr dwParam2);

		[DllImport("winmm.dll", CharSet = CharSet.Ansi, BestFitMapping = true, ThrowOnUnmappableChar = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool mciGetErrorString(uint mcierr, [MarshalAs(UnmanagedType.LPStr)]System.Text.StringBuilder pszText, uint cchText);

		//return total Sound Recording Devices
		[DllImport("winmm.dll")]
		public static extern int waveInGetNumDevs();
		//return spesific Sound Recording Devices spec
		[DllImport("winmm.dll", EntryPoint = "waveInGetDevCaps")]
		public static extern int waveInGetDevCapsA(int uDeviceID, ref WaveInCaps lpCaps, int uSize);
	}
}