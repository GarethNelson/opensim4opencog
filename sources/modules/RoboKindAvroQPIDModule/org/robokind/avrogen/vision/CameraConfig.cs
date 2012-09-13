// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.vision
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class CameraConfig : ISpecificRecord, Camera
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""CameraConfig"",""namespace"":""org.robokind.avrogen.vision"",""fields"":[{""name"":""brokerAddress"",""type"":""string""},{""name"":""connectionOptions"",""type"":""string""},{""name"":""destination"",""type"":""string""},{""name"":""cameraNumber"",""type"":""int""},{""name"":""frameLength"",""type"":""int""},{""name"":""imageWidth"",""type"":""int""},{""name"":""imageHeight"",""type"":""int""},{""name"":""imageChannels"",""type"":""int""}]}");
		private string _brokerAddress;
		private string _connectionOptions;
		private string _destination;
		private int _cameraNumber;
		private int _frameLength;
		private int _imageWidth;
		private int _imageHeight;
		private int _imageChannels;
		public virtual Schema Schema
		{
			get
			{
				return CameraConfig._SCHEMA;
			}
		}
		public string brokerAddress
		{
			get
			{
				return this._brokerAddress;
			}
			set
			{
				this._brokerAddress = value;
			}
		}
		public string connectionOptions
		{
			get
			{
				return this._connectionOptions;
			}
			set
			{
				this._connectionOptions = value;
			}
		}
		public string destination
		{
			get
			{
				return this._destination;
			}
			set
			{
				this._destination = value;
			}
		}
		public int cameraNumber
		{
			get
			{
				return this._cameraNumber;
			}
			set
			{
				this._cameraNumber = value;
			}
		}
		public int frameLength
		{
			get
			{
				return this._frameLength;
			}
			set
			{
				this._frameLength = value;
			}
		}
		public int imageWidth
		{
			get
			{
				return this._imageWidth;
			}
			set
			{
				this._imageWidth = value;
			}
		}
		public int imageHeight
		{
			get
			{
				return this._imageHeight;
			}
			set
			{
				this._imageHeight = value;
			}
		}
		public int imageChannels
		{
			get
			{
				return this._imageChannels;
			}
			set
			{
				this._imageChannels = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.brokerAddress;
			case 1: return this.connectionOptions;
			case 2: return this.destination;
			case 3: return this.cameraNumber;
			case 4: return this.frameLength;
			case 5: return this.imageWidth;
			case 6: return this.imageHeight;
			case 7: return this.imageChannels;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.brokerAddress = (System.String)fieldValue; break;
			case 1: this.connectionOptions = (System.String)fieldValue; break;
			case 2: this.destination = (System.String)fieldValue; break;
			case 3: this.cameraNumber = (System.Int32)fieldValue; break;
			case 4: this.frameLength = (System.Int32)fieldValue; break;
			case 5: this.imageWidth = (System.Int32)fieldValue; break;
			case 6: this.imageHeight = (System.Int32)fieldValue; break;
			case 7: this.imageChannels = (System.Int32)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
