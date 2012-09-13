// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.motion
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class RobotStatusResponseRecord : ISpecificRecord, RobotStatusResponse
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""RobotStatusResponseRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""responseHeader"",""type"":{""type"":""record"",""name"":""RobotResponseHeaderRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""robotId"",""type"":""string""},{""name"":""requestSourceId"",""type"":""string""},{""name"":""requestDestinationId"",""type"":""string""},{""name"":""requestType"",""type"":""string""},{""name"":""requestTimestampMillisecUTC"",""type"":""long""},{""name"":""responseTimestampMillisecUTC"",""type"":""long""}]}},{""name"":""statusResponse"",""type"":""boolean""}]}");
		private org.robokind.avrogen.motion.RobotResponseHeaderRecord _responseHeader;
		private bool _statusResponse;
		public virtual Schema Schema
		{
			get
			{
				return RobotStatusResponseRecord._SCHEMA;
			}
		}
		public org.robokind.avrogen.motion.RobotResponseHeaderRecord responseHeader
		{
			get
			{
				return this._responseHeader;
			}
			set
			{
				this._responseHeader = value;
			}
		}
		public bool statusResponse
		{
			get
			{
				return this._statusResponse;
			}
			set
			{
				this._statusResponse = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.responseHeader;
			case 1: return this.statusResponse;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.responseHeader = (org.robokind.avrogen.motion.RobotResponseHeaderRecord)fieldValue; break;
			case 1: this.statusResponse = (System.Boolean)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
