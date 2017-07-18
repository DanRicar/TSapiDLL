using System;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using log4net;
using Tsapi;

namespace TSapiDLL {

	public class Manager {

		public ILog mLog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public event TsapiEvent tsapiEvent;

		private AcsStream asyncSession = null;
		private AcsStream syncSession = null;
		private Thread bgWorker;
		private EventManager evtMgr;

		public Manager() {
			bgWorker = new Thread(doEventReaderWork);
			evtMgr = new EventManager();
			evtMgr.Log = mLog;
			evtMgr.tsapiEvent += EvtMgr_tsapiEvent;
		}

		private void EvtMgr_tsapiEvent(Object sender, Event e) {
			tsapiEvent?.Invoke(this, e);
		}

		#region Stream Commands

		public short startAsyncSession(String serverId, String loginId, String passwd) {
			asyncSession = new AcsStream();
			short retCode = asyncSession.openStream(serverId, loginId, passwd);
			if (retCode >= Acs.ACSPOSITIVE_ACK)
				bgWorker.Start();
			else
				asyncSession = null;
			return retCode;
		}


		public short stopAsyncSession() {
			short retCode = Acs.ACSERR_UNKNOWN;
			if (asyncSession != null)
				retCode = asyncSession.closeStream();
			if (bgWorker != null)
				bgWorker.Abort();
			return retCode;
		}

		public short startSyncSession(String serverId, String loginId, String passwd) {
			syncSession = new AcsStream();
			short retCode = syncSession.openStream(serverId, loginId, passwd);
			if (retCode < Acs.ACSPOSITIVE_ACK)
				asyncSession = null;
			Csta.EventBuffer_t evtBuf;
			Acs.PrivateData_t privData;
			retCode = syncSession.readEvent(out evtBuf, out privData)._value;
			return retCode;
		}

		public short stopSyncSession() {
			short retCode = Acs.ACSERR_UNKNOWN;
			if (syncSession != null)
				retCode = syncSession.closeStream();
			return retCode;
		}

		public short startSessions(String serverId, String loginId, String passwd) {
			short retCode = startSyncSession(serverId, loginId, passwd);
			if (retCode < Acs.ACSPOSITIVE_ACK)
				return retCode;
			retCode = startAsyncSession(serverId, loginId, passwd);
			return retCode;
		}

		public short stopSessions() {
			short retCode = stopSyncSession();
			if (retCode != Acs.ACSPOSITIVE_ACK)
				return retCode;
			retCode = stopAsyncSession();
			return retCode;
		}

		private void doEventReaderWork() {
			Console.WriteLine("TSapiDLL: bgWorker_DoWork: Started");
			while (true)
				try {
					Csta.EventBuffer_t evtBuf = null;
					Acs.PrivateData_t privData = null;
					Acs.RetCode_t retCode = asyncSession.readEvent(out evtBuf, out privData);
					if (retCode._value < Acs.ACSPOSITIVE_ACK) {
						Console.WriteLine("TSapiDLL: bgWorker_DoWork: Error AcsGetEvent: " + retCode._value);
					} else {
						Console.WriteLine("TSapiDLL: bgWorker_DoWork: Event : " + retCode._value + " : " + evtBuf.evt.eventHeader.eventClass.eventClass);
						evtMgr.throwEvent(this, evtBuf.evt, privData);
					}
				} catch (Exception ex) {
					Console.WriteLine("TSapiDLL: bgWorker_DoWork: Exception while getEvent", ex);
				}
		}

		#endregion

		#region Async Session Commands

		#region Monitoring Commands

		public short monitorDevice(String ext) {
			if (asyncSession == null) {
				mLog.Error("TSapiDLL: monitorDevice: No session stream connected");
				return Acs.ACSERR_UNKNOWN;
			}
			Csta.DeviceID_t device = ext;
			var filter = new Csta.CSTAMonitorFilter_t();
			var privData = new Acs.PrivateData_t();

			Acs.RetCode_t retCode = Csta.cstaMonitorDevice(asyncSession.AcsHandle, new Acs.InvokeID_t(), ref device, ref filter, privData);
			return retCode._value;
		}

		public short monitorStop(int monitorCrossRefId) {
			if (asyncSession == null) {
				mLog.Error("TSapiDLL: monitorStop: No session stream connected");
				return Acs.ACSERR_UNKNOWN;
			}
			var privData = new Acs.PrivateData_t();
			Csta.CSTAMonitorCrossRefID_t monID = monitorCrossRefId;

			Acs.RetCode_t retCode = Csta.cstaMonitorStop(asyncSession.AcsHandle, new Acs.InvokeID_t(), monID, privData);
			return retCode._value;
		}

		public short monitorCallsViaDevice(String ext) {
			if (asyncSession == null) {
				mLog.Error("TSapiDLL: monitorCallsViaDevice: No session stream connected");
				return Acs.ACSERR_UNKNOWN;
			}
			Csta.DeviceID_t device = ext;
			var filter = new Csta.CSTAMonitorFilter_t();
			var privData = new Acs.PrivateData_t();

			Acs.RetCode_t retCode = Csta.cstaMonitorCallsViaDevice(asyncSession.AcsHandle, new Acs.InvokeID_t(), ref device, ref filter, privData);
			return retCode._value;
		}

		/* unused Monitor Functions
		public short monitorCall(int callId) {
			Csta.ConnectionID_t connectionId = ;
			Acs.RetCode_t retCode = Csta.cstaMonitorCall(asyncSession.AcsHandle, new Acs.InvokeID_t(), monID, privData);
			return retCode._value;
		}
		 
		 changeMonitorFilter
				 
		*/

		#endregion

		#region  CallControll Commands


		/// <summary>
		/// Calls cstaMakeCall on TSAPI
		/// </summary>
		/// <param name="calling">Calling device extension</param>
		/// <param name="called">Called device extension</param>
		/// <param name="UUI">UUI string</param>
		/// <returns></returns>
		public short makeCall(String calling, String called, String UUI) {
			if (asyncSession == null) {
				mLog.Error("TSapiDLL: makeCall: No session stream connected");
				return Acs.ACSERR_UNKNOWN;
			}
			var invokeId = new Acs.InvokeID_t();
			Csta.DeviceID_t callingDevice = calling;
			Csta.DeviceID_t calledDevice = called;

			var u2uString = UUI;
			var u2uInfo = new Att.ATTUserToUserInfo_t();
			// fixed u2u size
			int u2uSize = Att.ATT_MAX_UUI_SIZE;
			u2uInfo.length = (short) u2uString.Length;
			u2uInfo.type = Att.ATTUUIProtocolType_t.UUI_IA5_ASCII;
			u2uInfo.value = Encoding.ASCII.GetBytes(u2uString);
			Array.Resize(ref u2uInfo.value, u2uSize);
			Csta.DeviceID_t destRouteOrSplit = null;
			Acs.PrivateData_t privData = null;
			Att.attV6MakeCall(privData, ref destRouteOrSplit, false, ref u2uInfo);
			Acs.RetCode_t retCode = Csta.cstaMakeCall(asyncSession.AcsHandle, invokeId, ref callingDevice, ref calledDevice, privData);
			return retCode._value;
		}

		/*  Unused CallControll functions
		
		 * csta functions
		alternateCall
		answerCall
		callCompletion
		clearCall
		clearConnection
		conferenceCall
		consultationCall
		deflectCall
		groupPickupCall
		holdCall
		makePredictiveCall
		pickupCall
		reconnectCall
		retrievwCall
		transferCall

		* att Functions
		sendDTMF
		singleStepConference
		singleStepTransfer
		*/

		#endregion

		#region  Snapshot Commands

		/* Unused Snapshot functions
		 snapshotDevice
		 snapshotCall

		  */
		#endregion

		#region Routing Commands

		#endregion

		#endregion

		#region Synchro Session Commands

		#region Query Commands

		public short getApiCaps(out Event responseEvent, out Acs.PrivateData_t privateData) {
			privateData = null;
			responseEvent = null;
			Csta.EventBuffer_t eventBuffer = null;

			Acs.RetCode_t retCode = Csta.cstaGetAPICaps(syncSession.AcsHandle, new Acs.InvokeID_t());
			if (retCode._value < Acs.ACSPOSITIVE_ACK)
				return retCode._value;
			retCode = syncSession.readEvent(out eventBuffer, out privateData);
			responseEvent = evtMgr.createEvent(eventBuffer.evt);
			return retCode._value;
		}

		/*  Unused  functions
		 *  
Query ACD Split
Query Agent Login
Query Agent State 
Query Call Classifier
Query Device Info
Query Device Name
Query Do Not Disturb
Query Forwarding 
Query Message Waiting
Query Station Status
Query Time Of Day
Query Trunk Group
Query Universal Call ID Service (Private)
  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  

		*/

#endregion

#region SetFeature Commands
 /* Unused
  
Set Advice of Charge Service (Private Data Version 5 and Later)
.  .  .  .  .  .  .  .  .  .  .  .      240
Set Agent State Service
.  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .      243
Set Billing Rate Service (Private Data Version 5 and Later)
.  .  .  .  .  .  .  .  .  .  .  .  .  .  .      254
Set Do Not Disturb Feature Service
.  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .      258
Set Forwarding Feature Service
.  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .      261
Set Message Waiting Indicator (MWI) Feature Service

 */
#endregion

#endregion

}
}
