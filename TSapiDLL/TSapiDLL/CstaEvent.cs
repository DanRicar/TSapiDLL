using System;
using Tsapi;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace TSapiDLL {
	class CstaEvent : Event {
		public CstaEvent(Csta.CSTAEvent_t evt) : base(evt.eventHeader.eventClass.eventClass, evt.eventHeader.eventType.eventType) {
			this.EvData = getEventData(evt);

		}

		public Hashtable getEventData(Csta.CSTAEvent_t evt) {
			Hashtable rslt = new Hashtable();
			Object evtClass = getPropertyObjByName(evt, EventDefs.EventClassNames[(EventDefs.EventClass) EvClass]);
			Type tpEv = EventDefs.CstaEventTypes[(EventDefs.EventCstaType) EvType];
			FieldInfo[] fields = tpEv.GetFields();
			foreach (var field in fields) {
				string name = field.Name;
				object val = field.GetValue(getPropertyObjByName(evtClass, EventDefs.CstaEventNames[(EventDefs.EventCstaType) EvType]));
				rslt.Add(name, val);
			}
			return rslt;
		}
	}
}