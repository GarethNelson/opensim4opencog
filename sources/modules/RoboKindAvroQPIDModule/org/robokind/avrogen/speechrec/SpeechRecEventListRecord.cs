// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.speechrec
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class SpeechRecEventListRecord : ISpecificRecord, SpeechRecEventList
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""SpeechRecEventListRecord"",""namespace"":""org.robokind.avrogen.speechrec"",""fields"":[{""name"":""speechRecServiceId"",""type"":""string""},{""name"":""eventDestinationId"",""type"":""string""},{""name"":""timestampMillisecUTC"",""type"":""long""},{""name"":""speechRecEvents"",""type"":{""type"":""array"",""items"":{""type"":""record"",""name"":""SpeechRecEventRecord"",""namespace"":""org.robokind.avrogen.speechrec"",""fields"":[{""name"":""recognizerId"",""type"":""string""},{""name"":""timestampMillisecUTC"",""type"":""long""},{""name"":""recognizedText"",""type"":""string""},{""name"":""confidence"",""type"":""double""}]}}}]}");
		private string _speechRecServiceId;
		private string _eventDestinationId;
		private long _timestampMillisecUTC;
		private IList<org.robokind.avrogen.speechrec.SpeechRecEventRecord> _speechRecEvents;
		public virtual Schema Schema
		{
			get
			{
				return SpeechRecEventListRecord._SCHEMA;
			}
		}
		public string speechRecServiceId
		{
			get
			{
				return this._speechRecServiceId;
			}
			set
			{
				this._speechRecServiceId = value;
			}
		}
		public string eventDestinationId
		{
			get
			{
				return this._eventDestinationId;
			}
			set
			{
				this._eventDestinationId = value;
			}
		}
		public long timestampMillisecUTC
		{
			get
			{
				return this._timestampMillisecUTC;
			}
			set
			{
				this._timestampMillisecUTC = value;
			}
		}
		public IList<org.robokind.avrogen.speechrec.SpeechRecEventRecord> speechRecEvents
		{
			get
			{
				return this._speechRecEvents;
			}
			set
			{
				this._speechRecEvents = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.speechRecServiceId;
			case 1: return this.eventDestinationId;
			case 2: return this.timestampMillisecUTC;
			case 3: return this.speechRecEvents;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.speechRecServiceId = (System.String)fieldValue; break;
			case 1: this.eventDestinationId = (System.String)fieldValue; break;
			case 2: this.timestampMillisecUTC = (System.Int64)fieldValue; break;
			case 3: this.speechRecEvents = (IList<org.robokind.avrogen.speechrec.SpeechRecEventRecord>)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
