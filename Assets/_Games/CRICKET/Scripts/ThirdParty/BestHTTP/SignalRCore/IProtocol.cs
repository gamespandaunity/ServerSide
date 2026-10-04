using System;
using System.Collections.Generic;
using BestHTTP.SignalRCore.Messages;

namespace BestHTTP.SignalRCore
{
	public interface IProtocol
	{
		TransferModes Type { get; }

		IEncoder Encoder { get; }

		HubConnection Connection { get; set; }

		void ParseMessages(string data, ref List<BestHTTP.SignalRCore.Messages.Message> messages);

		void ParseMessages(byte[] data, ref List<BestHTTP.SignalRCore.Messages.Message> messages);

		byte[] EncodeMessage(BestHTTP.SignalRCore.Messages.Message message);

		object[] GetRealArguments(Type[] argTypes, object[] arguments);

		object ConvertTo(Type toType, object obj);
	}
}
