using System;
using Tsapi;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace TSapiDLL {

	public delegate void TsapiEvent(Object sender, Event e);

	public class EventManager {
		private ILog mLog;
		public ILog Log {
			get {
				return mLog;
			}

			set {
				this.mLog = value;
			}
		}

		public event TsapiEvent tsapiEvent;

		public void throwEvent(Object sender, Csta.CSTAEvent_t evt, Acs.PrivateData_t privData) {
			if (tsapiEvent != null) {
				Event mEv = createEvent(evt);
				if (mEv != null)
					tsapiEvent.Invoke(this, mEv);
			}
		}

		public Event createEvent(Csta.CSTAEvent_t evt) {
			Event mEv = null;
			switch ((EventDefs.EventClass) evt.eventHeader.eventClass.eventClass) {
				case EventDefs.EventClass.ACSCONFIRMATION:
				case EventDefs.EventClass.ACSREQUEST:
				case EventDefs.EventClass.ACSUNSOLICITED:
					mEv = new AcsEvent(evt);
					break;
				case EventDefs.EventClass.CSTACONFIRMATION:
				case EventDefs.EventClass.CSTAEVENTREPORT:
				case EventDefs.EventClass.CSTAREQUEST:
				case EventDefs.EventClass.CSTAUNSOLICITED:
					mEv = new CstaEvent(evt);
					break;
				default:
					break;
			}
			return mEv;
		}
	}
}
