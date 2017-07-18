using System;
using System.Collections;
using Tsapi;
using System.Reflection;

namespace TSapiDLL {
	class AcsEvent : Event {
		public AcsEvent(Csta.CSTAEvent_t evt) : base(evt.eventHeader.eventClass.eventClass, evt.eventHeader.eventType.eventType) {
			this.EvData = getEventData(evt);

		}

		public Hashtable getEventData(Csta.CSTAEvent_t evt) {
			Hashtable rslt = new Hashtable();
			Object evtClass = Event.getPropertyObjByName(evt, EventDefs.EventClassNames[(EventDefs.EventClass) EvClass]);
			Type tpEv = EventDefs.AcsEventTypes[(EventDefs.EventAcsType) EvType];
			FieldInfo[] fields = tpEv.GetFields();
			foreach (var field in fields) {
				string name = field.Name;
				object val = field.GetValue(Event.getPropertyObjByName(evtClass, EventDefs.AcsEventNames[(EventDefs.EventAcsType) EvType]));
				rslt.Add(name, val);
			}
			return rslt;
		}
	}
}
