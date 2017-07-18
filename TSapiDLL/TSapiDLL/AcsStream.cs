using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;
using Tsapi;

namespace TSapiDLL {
	class AcsStream {
		public const String API_VERSION = "TS1-2";
		public const String REQ_VERSION = "3-10";
		public const String APP_NAME = "TSAPI.DLL";
		private ILog mLog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Acs.ACSHandle_t acsHandle;

		/// <summary>
		/// Calls acsOpenStream on TSAPI.
		/// Starts worker for event reading
		/// </summary>
		/// <param name="serverId">TLink</param>
		/// <param name="loginId">username</param>
		/// <param name="passwd">password</param>
		public short openStream(String serverId, String loginId, String passwd) {
			mLog.Debug("TSapiDLL: openStream: " + serverId + " / " + loginId);
			this.acsHandle = new Acs.ACSHandle_t();
			var invokeIdType = Acs.InvokeIDType_t.APP_GEN_ID;
			var invokeId = new Acs.InvokeID_t();
			var streamType = Acs.StreamType_t.ST_CSTA;
			Acs.ServerID_t _serverId = serverId;
			Acs.LoginID_t _loginId = loginId;
			Acs.Passwd_t _passwd = passwd;
			Acs.AppName_t appName = APP_NAME;
			Acs.Level_t acsLevelReq = Acs.Level_t.ACS_LEVEL1;
			Acs.Version_t apiVer = API_VERSION;
			ushort sendQSize = 0;
			ushort sendExtraBufs = 0;
			ushort recvQSize = 0;
			ushort recvExtraBufs = 0;
			// Get supportedVersion string

			System.Text.StringBuilder supportedVersion = new System.Text.StringBuilder();
			Acs.RetCode_t attrc = Att.attMakeVersionString(REQ_VERSION, supportedVersion);
			// Set PrivateData request
			Acs.PrivateData_t privData;
			privData = new Acs.PrivateData_t();
			privData.vendor = "VERSION";
			privData.data = new byte[Att.ATT_MAX_PRIVATE_DATA];
			privData.data[0] = Acs.PRIVATE_DATA_ENCODING;
			for (int i = 0; i < supportedVersion.Length; i++) {
				privData.data[i + 1] = (byte) supportedVersion[i];
			}
			privData.length = Att.ATT_MAX_PRIVATE_DATA;
			Acs.RetCode_t retCode = Acs.acsOpenStream(out this.acsHandle,
														  invokeIdType,
														  invokeId,
														  streamType,
														  ref _serverId,
														  ref _loginId,
														  ref _passwd,
														  ref appName,
														  acsLevelReq,
														  ref apiVer,
														  sendQSize,
														  sendExtraBufs,
														  recvQSize,
														  recvExtraBufs,
														  privData);
			return retCode._value;
		}

		/// <summary>
		/// Calls acsCloseStream on TSAPI.
		/// Stops worker for event reading.
		/// </summary>
		/// <returns></returns>
		public short closeStream() {
			Csta.EventBuffer_t evtBuf = new Csta.EventBuffer_t();
			Acs.RetCode_t retCode = Acs.acsCloseStream(this.acsHandle, new Acs.InvokeID_t(), null);
			return retCode._value;
		}

		public Acs.RetCode_t readEvent(out Csta.EventBuffer_t evtBuf, out Acs.PrivateData_t privData) {
			evtBuf = new Csta.EventBuffer_t();
			ushort eventBufSize = Csta.CSTA_MAX_HEAP;
			ushort numEvents = 0;
			privData = new Acs.PrivateData_t();
			privData.data = new byte[Att.ATT_MAX_PRIVATE_DATA];
			privData.length = Att.ATT_MAX_PRIVATE_DATA;
			Acs.RetCode_t retCode = Acs.acsGetEventBlock(AcsHandle,
								 evtBuf,
								 ref eventBufSize,
								 privData,
								 out numEvents);
			return retCode;
		}


		public Acs.ACSHandle_t AcsHandle {
			get {
				return acsHandle;
			}
		}

	}
}
