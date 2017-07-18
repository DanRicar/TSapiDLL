using System;
using System.Collections;
using System.Reflection;

namespace TSapiDLL {
	public class Event {
		private EventDefs.EventClass evClass;
		private ushort evType;
		private Hashtable  evData;
		private Hashtable privData;

		public Event() { }

		public Event(ushort eventClass) {
			evClass = (EventDefs.EventClass) eventClass; 
		}

		public Event(ushort eventClass, ushort eventType) {
			evClass = (EventDefs.EventClass) eventClass;
			EvType = eventType;
		}

		public static Object getPropertyObjByName(Object target, String prop) {
			try {
				Type t = target.GetType();
				PropertyInfo i = t.GetProperty(prop);
				Object p = i.GetValue(target, null);
//				Object p = target.GetType().GetProperty(prop).GetValue(target, null);
				return p;
			} catch (Exception ex) {
				return ex.Message;
			}
		}

		public EventDefs.EventClass EvClass {
			get {
				return evClass;
			}

			set {
				this.evClass = value;
			}
		}

		public Hashtable EvData {
			get {
				return evData;
			}

			set {
				this.evData = value;
			}
		}

		public Hashtable PrivData {
			get {
				return privData;
			}

			set {
				this.privData = value;
			}
		}

		public System.UInt16 EvType {
			get {
				return evType;
			}

			set{ 
				this.evType = value;
			}
		}
	}
}
