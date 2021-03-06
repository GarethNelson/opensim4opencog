// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.audio
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class WavPlayerConfigRecord : ISpecificRecord, WavPlayerConfig
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""WavPlayerConfigRecord"",""namespace"":""org.robokind.avrogen.audio"",""fields"":[{""name"":""wavPlayerId"",""type"":""string""},{""name"":""wavLocation"",""type"":""string""},{""name"":""startTimeMicrosec"",""type"":""long""},{""name"":""stopTimeMicrosec"",""type"":""long""},{""name"":""startDelayMillisec"",""type"":""long""}]}");
		private string _wavPlayerId;
		private string _wavLocation;
		private long _startTimeMicrosec;
		private long _stopTimeMicrosec;
		private long _startDelayMillisec;
		public virtual Schema Schema
		{
			get
			{
				return WavPlayerConfigRecord._SCHEMA;
			}
		}
		public string wavPlayerId
		{
			get
			{
				return this._wavPlayerId;
			}
			set
			{
				this._wavPlayerId = value;
			}
		}
		public string wavLocation
		{
			get
			{
				return this._wavLocation;
			}
			set
			{
				this._wavLocation = value;
			}
		}
		public long startTimeMicrosec
		{
			get
			{
				return this._startTimeMicrosec;
			}
			set
			{
				this._startTimeMicrosec = value;
			}
		}
		public long stopTimeMicrosec
		{
			get
			{
				return this._stopTimeMicrosec;
			}
			set
			{
				this._stopTimeMicrosec = value;
			}
		}
		public long startDelayMillisec
		{
			get
			{
				return this._startDelayMillisec;
			}
			set
			{
				this._startDelayMillisec = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.wavPlayerId;
			case 1: return this.wavLocation;
			case 2: return this.startTimeMicrosec;
			case 3: return this.stopTimeMicrosec;
			case 4: return this.startDelayMillisec;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.wavPlayerId = (System.String)fieldValue; break;
			case 1: this.wavLocation = (System.String)fieldValue; break;
			case 2: this.startTimeMicrosec = (System.Int64)fieldValue; break;
			case 3: this.stopTimeMicrosec = (System.Int64)fieldValue; break;
			case 4: this.startDelayMillisec = (System.Int64)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
