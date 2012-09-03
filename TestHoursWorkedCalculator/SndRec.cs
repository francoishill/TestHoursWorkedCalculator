/*--------------------------------------------------------
 * MainForm.cs - (c) Mohammad Elsheimy, 2010
 * http://JustLikeAMagic.WordPress.com
  --------------------------------------------------------*/

using System;
using System.Runtime.InteropServices;
using System.Text;
using API = SharedClasses.SafeNativeMethods;
using System.Collections.Generic;
//using Res = Geming.SimpleRec.Properties.Resources;

//namespace Geming.SimpleRec
namespace SharedClasses
{
	public enum SndStatus
	{
		Uninitialized,
		Normal,
		Working,
		Paused,
	}

	public interface ISnd : IDisposable
	{

		SndStatus Status { get; }
		bool IsWorking { get; }
		bool IsPaused { get; }
		bool IsInitialized { get; }
		bool IsNormal { get; }
		int Length { get; }
		int Position { get; }

		void Initialize(int recordingDeviceIndex);
		void Start();
		void Pause();
		void Resume();
		void Stop();
		void Close();

		void SetNotifyHandle(IntPtr handle);
	}

	public sealed class SndRec : ISnd
	{
		private const int BufferLength = 256;

		private static SndRec _instance;
		private static SndStatus _status = SndStatus.Uninitialized;
		private API.MCI_GENERIC_PARMS _parmsGeneric = new API.MCI_GENERIC_PARMS();
		private API.MCI_OPEN_PARMS _parmsOpen = new API.MCI_OPEN_PARMS();
		private API.MCI_WAVE_SET_PARMS _parmsWaveSet = new API.MCI_WAVE_SET_PARMS();
		private API.MCI_RECORD_PARMS _parmsRecord = new API.MCI_RECORD_PARMS();
		private API.MCI_SAVE_PARMS _parmsSave = new API.MCI_SAVE_PARMS();
		private API.MCI_STATUS_PARMS _parmsStat = new API.MCI_STATUS_PARMS();
		private uint _deviceID;
		private uint _err;
		private IntPtr _handle = IntPtr.Zero;

		public static SndRec Instance
		{
			get
			{
				if (null == _instance)
					_instance = new SndRec();
				return _instance;
			}
		}
		public SndStatus Status { get { return _status; } }
		public bool IsWorking { get { return _status == SndStatus.Working; } }
		public bool IsPaused { get { return _status == SndStatus.Paused; } }
		public bool IsInitialized { get { return _status != SndStatus.Uninitialized; } }
		public bool IsNormal { get { return _status == SndStatus.Normal; } }
		public int Length { get { return InternalGetLength(); } }
		public int Position { get { return InternalGetPosition(); } }


		private SndRec()
		{
		}

		public int GetRecordingDeviceCount() { return SafeNativeMethods.waveInGetNumDevs(); }
		public List<string> GetRecordingDevices() { return InternalGetRecordingDevices(); }
		public void Initialize(int recordingDeviceIndex) { InternalInitialize((uint)recordingDeviceIndex); }
		public void Start() { Start(0, true); }
		public void Start(bool async) { Start(0, async); }
		public void Start(int length) { Start(length, true); }
		public void Start(int length, bool async) { InternalStart(length, async); }
		public void Pause() { InternalPause(); }
		public void Resume() { InternalResume(); }
		public void Stop() { InternalStop(); }
		public void Save(string fileName) { InternalSave(fileName); }
		public void Close() { InternalClose(); }
		public void Dispose() { Close(); }
		public void SetNotifyHandle(IntPtr handle) { _handle = handle; }

		private List<string> InternalGetRecordingDevices()
		{
			List<string> tmplist = new List<string>();
			int waveInDevicesCount = SafeNativeMethods.waveInGetNumDevs(); //get total
			if (waveInDevicesCount > 0)
			{
				for (int uDeviceID = 0; uDeviceID < waveInDevicesCount; uDeviceID++)
				{
					SafeNativeMethods.WaveInCaps waveInCaps = new SafeNativeMethods.WaveInCaps();
					SafeNativeMethods.waveInGetDevCapsA(uDeviceID, ref waveInCaps,
									  Marshal.SizeOf(typeof(SafeNativeMethods.WaveInCaps)));
					string deviceName = new string(waveInCaps.szPname).Remove(new string(waveInCaps.szPname).IndexOf('\0')).Trim();
					tmplist.Add(deviceName);
					//clean garbage
				}
			}
			return tmplist;
		}
		private void InternalInitialize(uint recordingDeviceIndex)
		{
			_parmsOpen.wDeviceID = recordingDeviceIndex;
			_parmsOpen.lpstrDeviceType = Marshal.StringToHGlobalAnsi(API.WaveAudio);
			_parmsOpen.lpstrElementName = Marshal.StringToHGlobalAnsi(String.Empty);
			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_OPEN_PARMS)));
			Marshal.StructureToPtr(_parmsOpen, ptr, false);
			_err = API.mciSendCommand(recordingDeviceIndex, API.MCI_OPEN, API.MCI_WAIT | API.MCI_OPEN_TYPE | API.MCI_OPEN_ELEMENT, ptr);
			_parmsOpen = (API.MCI_OPEN_PARMS)Marshal.PtrToStructure(ptr, typeof(API.MCI_OPEN_PARMS));
			Marshal.FreeHGlobal(_parmsOpen.lpstrDeviceType);
			Marshal.FreeHGlobal(_parmsOpen.lpstrElementName);
			if (0 != _err)
				ThrowException(_err);
			_deviceID = _parmsOpen.wDeviceID;

			_parmsWaveSet.wBitsPerSample = 16;
			_parmsWaveSet.nChannels = 1;
			_parmsWaveSet.nSamplesPerSec = 44100;
			IntPtr ptr2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_WAVE_SET_PARMS)));
			Marshal.StructureToPtr(_parmsWaveSet, ptr2, false);
			_err = API.mciSendCommand(_deviceID, API.MCI_SET, API.MCI_WAIT | API.MCI_WAVE_SET_BITSPERSAMPLE | API.MCI_WAVE_SET_CHANNELS | API.MCI_WAVE_SET_SAMPLESPERSEC, ptr2);
			_parmsWaveSet = (API.MCI_WAVE_SET_PARMS)Marshal.PtrToStructure(ptr2, typeof(API.MCI_WAVE_SET_PARMS));
			//Marshal.FreeHGlobal(_parmsWaveSet.wBitsPerSample);
			if (0 != _err)
				ThrowException(_err);
			Marshal.FreeHGlobal(ptr2);

			_status = SndStatus.Normal;
		}
		private void InternalStart(int length, bool async)
		{
			if (!IsInitialized)
				ThrowException("Uninitialized.");//"Uninitialized.");

			if (length < 0)
				ThrowException(new ArgumentOutOfRangeException("length", "Length cannot be less than zero."));

			if (length > 0)
				_parmsRecord.dwTo = (uint)length;

			_parmsRecord.dwTo = (uint)length;
			_parmsRecord.dwCallback = _handle;

			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_RECORD_PARMS)));
			Marshal.StructureToPtr(_parmsRecord, ptr, false);
			_err = API.mciSendCommand(_deviceID, API.MCI_RECORD, (length != 0 ? API.MCI_NOTIFY : 0) | (async ? 0 : API.MCI_WAIT) | (length > 0 ? API.MCI_TO : 0), ptr);
			Marshal.FreeHGlobal(ptr);

			if (0 != _err)
				ThrowException(_err);

			_status = SndStatus.Working;
		}
		private void InternalPause()
		{
			if (!IsInitialized)
				ThrowException("Uninitialized.");

			if (!IsWorking)
				ThrowException("Invalid operation. Check Status for more details.");

			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_GENERIC_PARMS)));
			Marshal.StructureToPtr(_parmsGeneric, ptr, false);
			_err = API.mciSendCommand(_deviceID, API.MCI_PAUSE, (API.MCI_WAIT), ptr);
			Marshal.FreeHGlobal(ptr);

			if (0 != _err)
				ThrowException(_err);

			_status = SndStatus.Paused;
		}
		private void InternalResume()
		{
			if (!IsInitialized)
				ThrowException("Uninitialized.");

			if (!IsPaused)
				ThrowException("Invalid operation. Check Status for more details.");


			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_GENERIC_PARMS)));
			Marshal.StructureToPtr(_parmsGeneric, ptr, false);
			_err = API.mciSendCommand(_deviceID, API.MCI_RESUME, (API.MCI_WAIT), ptr);
			Marshal.FreeHGlobal(ptr);


			if (0 != _err)
				ThrowException(_err);

			_status = SndStatus.Working;
		}
		private void InternalStop()
		{
			if (!IsInitialized)
				ThrowException("Uninitialized.");

			if (!IsWorking && !IsPaused)
				return; // do nothing


			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_GENERIC_PARMS)));
			Marshal.StructureToPtr(_parmsGeneric, ptr, false);
			_err = API.mciSendCommand(_deviceID, API.MCI_STOP, (API.MCI_WAIT), ptr);
			Marshal.FreeHGlobal(ptr);

			if (0 != _err)
				ThrowException(_err);

			_status = SndStatus.Normal;
		}
		private void InternalSave(string fileName)
		{
			if (!IsInitialized)
				ThrowException("Uninitialized.");

			if (!IsNormal)
				ThrowException("Invalid operation. Check Status for more details.");

			_parmsSave.lpfilename = Marshal.StringToHGlobalAnsi(fileName);

			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_SAVE_PARMS)));
			Marshal.StructureToPtr(_parmsSave, ptr, false);
			_err = API.mciSendCommand(_deviceID, API.MCI_SAVE, (API.MCI_WAIT | API.MCI_SAVE_FILE), ptr);
			Marshal.FreeHGlobal(ptr);

			Marshal.FreeHGlobal(_parmsSave.lpfilename);


			if (0 != _err)
				ThrowException(_err);
		}
		private int InternalGetLength()
		{
			if (!IsInitialized)
				ThrowException("Uninitialized.");

			_parmsStat.dwItem = API.MCI_STATUS_LENGTH;

			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_STATUS_PARMS)));
			Marshal.StructureToPtr(_parmsStat, ptr, false);

			_err = API.mciSendCommand(_deviceID, API.MCI_STATUS, API.MCI_WAIT | API.MCI_STATUS_ITEM, ptr);

			_parmsStat = (API.MCI_STATUS_PARMS)Marshal.PtrToStructure(ptr, typeof(API.MCI_STATUS_PARMS));
			Marshal.FreeHGlobal(ptr);

			if (0 != _err)
				ThrowException(_err);

			return (int)Math.Round((double)(_parmsStat.dwReturn / 1000));
		}
		private int InternalGetPosition()
		{
			if (!IsInitialized)
				ThrowException("Uninitialized.");

			_parmsStat.dwItem = API.MCI_STATUS_POSITION;

			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_STATUS_PARMS)));
			Marshal.StructureToPtr(_parmsStat, ptr, false);

			_err = API.mciSendCommand(_deviceID, API.MCI_STATUS, API.MCI_WAIT | API.MCI_STATUS_ITEM, ptr);

			_parmsStat = (API.MCI_STATUS_PARMS)Marshal.PtrToStructure(ptr, typeof(API.MCI_STATUS_PARMS));
			Marshal.FreeHGlobal(ptr);

			if (0 != _err)
				ThrowException(_err);

			return (int)Math.Round((double)(_parmsStat.dwReturn / 1000));
		}
		private void InternalClose()
		{
			if (!IsInitialized)
				return;

			if (IsWorking || IsPaused)
				Stop();

			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(API.MCI_GENERIC_PARMS)));
			Marshal.StructureToPtr(_parmsGeneric, ptr, false);
			_err = API.mciSendCommand(_deviceID, API.MCI_CLOSE, (API.MCI_WAIT), ptr);
			Marshal.FreeHGlobal(ptr);


			if (0 != _err)
				ThrowException(_err);

			_status = SndStatus.Uninitialized;
		}

		public string GetLastError() { return GetError(_err); }
		public string GetLastError(out int errCode) { errCode = (int)_err; return GetError(_err); }

		private static void ThrowException(uint errCode) { ThrowException(GetError(errCode), errCode); }
		private static void ThrowException(string msg) { ThrowException(msg, 0); }
		private static void ThrowException(string msg, uint errCode) { throw new SndException(msg, (int)errCode); }
		private static void ThrowException(Exception ex) { throw ex; }
		private static string GetError(uint errCode)
		{
			StringBuilder str = new StringBuilder(BufferLength);
			API.mciGetErrorString(errCode, str, BufferLength);
			return str.ToString();
		}
	}

	[Serializable]
	public class SndException : Exception
	{
		private int _errCode;

		public int ErrorCode { get { return _errCode; } }

		public SndException() : this(string.Empty, 0) { }
		public SndException(string message) : this(message, 0) { }
		public SndException(int errCode) : this(String.Empty, errCode) { }
		public SndException(string message, int errCode) : base(message) { _errCode = errCode; }
		public SndException(string message, Exception inner) : base(message, inner) { }
		protected SndException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			info.AddValue("ErrorCode", _errCode, typeof(int));
			base.GetObjectData(info, context);
		}
	}
}