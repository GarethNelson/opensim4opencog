// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.speech
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class SpeechEventRecord : ISpecificRecord, SpeechEvent
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""SpeechEventRecord"",""namespace"":""org.robokind.avrogen.speech"",""fields"":[{""name"":""eventType"",""type"":""string""},{""name"":""streamNumber"",""type"":""long""},{""name"":""textPosition"",""type"":""int""},{""name"":""textLength"",""type"":""int""},{""name"":""currentData"",""type"":""int""},{""name"":""nextData"",""type"":""int""},{""name"":""stringData"",""type"":""string""},{""name"":""duration"",""type"":""int""}]}");
		private string _eventType;
		private long _streamNumber;
		private int _textPosition;
		private int _textLength;
		private int _currentData;
		private int _nextData;
		private string _stringData;
		private int _duration;
		public virtual Schema Schema
		{
			get
			{
				return SpeechEventRecord._SCHEMA;
			}
		}
		public string eventType
		{
			get
			{
				return this._eventType;
			}
			set
			{
				this._eventType = value;
			}
		}
		public long streamNumber
		{
			get
			{
				return this._streamNumber;
			}
			set
			{
				this._streamNumber = value;
			}
		}
		public int textPosition
		{
			get
			{
				return this._textPosition;
			}
			set
			{
				this._textPosition = value;
			}
		}
		public int textLength
		{
			get
			{
				return this._textLength;
			}
			set
			{
				this._textLength = value;
			}
		}
		public int currentData
		{
			get
			{
				return this._currentData;
			}
			set
			{
				this._currentData = value;
			}
		}
		public int nextData
		{
			get
			{
				return this._nextData;
			}
			set
			{
				this._nextData = value;
			}
		}
		public string stringData
		{
			get
			{
				return this._stringData;
			}
			set
			{
				this._stringData = value;
			}
		}
		public int duration
		{
			get
			{
				return this._duration;
			}
			set
			{
				this._duration = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.eventType;
			case 1: return this.streamNumber;
			case 2: return this.textPosition;
			case 3: return this.textLength;
			case 4: return this.currentData;
			case 5: return this.nextData;
			case 6: return this.stringData;
			case 7: return this.duration;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.eventType = (System.String)fieldValue; break;
			case 1: this.streamNumber = (System.Int64)fieldValue; break;
			case 2: this.textPosition = (System.Int32)fieldValue; break;
			case 3: this.textLength = (System.Int32)fieldValue; break;
			case 4: this.currentData = (System.Int32)fieldValue; break;
			case 5: this.nextData = (System.Int32)fieldValue; break;
			case 6: this.stringData = (System.String)fieldValue; break;
			case 7: this.duration = (System.Int32)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
