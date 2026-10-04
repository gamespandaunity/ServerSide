using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooglePlayGames.BasicApi.Nearby
{
	public class DummyNearbyConnectionClient : INearbyConnectionClient
	{
		public int MaxUnreliableMessagePayloadLength()
		{
			return 1168;
		}

		public int MaxReliableMessagePayloadLength()
		{
			return 4096;
		}

		public void SendReliable(List<string> recipientEndpointIds, byte[] payload)
		{
			ConstantsData_M.Log("SendReliable called from dummy implementation");
		}

		public void SendUnreliable(List<string> recipientEndpointIds, byte[] payload)
		{
			ConstantsData_M.Log("SendUnreliable called from dummy implementation");
		}

		public void StartAdvertising(string name, List<string> appIdentifiers, TimeSpan? advertisingDuration, Action<AdvertisingResult> resultCallback, Action<ConnectionRequest> connectionRequestCallback)
		{
			AdvertisingResult obj = new AdvertisingResult(ResponseStatus.LicenseCheckFailed, string.Empty);
			resultCallback(obj);
		}

		public void StopAdvertising()
		{
			ConstantsData_M.Log("StopAvertising in dummy implementation called");
		}

		public void SendConnectionRequest(string name, string remoteEndpointId, byte[] payload, Action<ConnectionResponse> responseCallback, IMessageListener listener)
		{
			ConstantsData_M.Log("SendConnectionRequest called from dummy implementation");
			if (responseCallback != null)
			{
				ConnectionResponse obj = ConnectionResponse.Rejected(0L, string.Empty);
				responseCallback(obj);
			}
		}

		public void AcceptConnectionRequest(string remoteEndpointId, byte[] payload, IMessageListener listener)
		{
			ConstantsData_M.Log("AcceptConnectionRequest in dummy implementation called");
		}

		public void StartDiscovery(string serviceId, TimeSpan? advertisingTimeout, IDiscoveryListener listener)
		{
			ConstantsData_M.Log("StartDiscovery in dummy implementation called");
		}

		public void StopDiscovery(string serviceId)
		{
			ConstantsData_M.Log("StopDiscovery in dummy implementation called");
		}

		public void RejectConnectionRequest(string requestingEndpointId)
		{
			ConstantsData_M.Log("RejectConnectionRequest in dummy implementation called");
		}

		public void DisconnectFromEndpoint(string remoteEndpointId)
		{
			ConstantsData_M.Log("DisconnectFromEndpoint in dummy implementation called");
		}

		public void StopAllConnections()
		{
			ConstantsData_M.Log("StopAllConnections in dummy implementation called");
		}

		public string LocalEndpointId()
		{
			return string.Empty;
		}

		public string LocalDeviceId()
		{
			return "DummyDevice";
		}

		public string GetAppBundleId()
		{
			return "dummy.bundle.id";
		}

		public string GetServiceId()
		{
			return "dummy.service.id";
		}
	}
}
