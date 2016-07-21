using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPHandler : MonoBehaviour {
	
	// receiving Thread
	Thread receiveThread;
	
	// udpclient object
	UdpClient client;
	
	// public
	// public string IP = "127.0.0.1"; default local
	public int port; // define > init
	
	// infos
	public string lastReceivedUDPPacket="";
	public string allReceivedUDPPackets=""; // clean up this from time to time!
	
	
	// start from shell
	private static void Main()
	{
		UDPHandler receiveObj=new UDPHandler();
		receiveObj.init();
		
		string text="";
		do
		{
			text = Console.ReadLine();
		}
		while(!text.Equals("exit"));
	}
	// start from unity3d
	public void Start()
	{
		
		init();
	}
	
	// OnGUI
	void OnGUI()
	{
		Rect rectObj=new Rect(40,10,200,400);
		GUIStyle style = new GUIStyle();
		style.alignment = TextAnchor.UpperLeft;
		GUI.Box(rectObj,"# UDPHandler\n127.0.0.1 "+port+" #\n" 
		        + "LocalIP: " + GetLocalIP() + "\n"
		        + "shell> nc -u 127.0.0.1 : "+port+" \n"
		        + "\nLast Packet: \n"+ lastReceivedUDPPacket
		        + "\n\nAll Messages: \n"+allReceivedUDPPackets
		        ,style);
	}
	
	// init
	private void init()
	{
		// Endpunkt definieren, von dem die Nachrichten gesendet werden.
		Debug.Log("UDPSend.init()");
		
		// define port
		port = 6970;
		
		// status
		Debug.Log("Sending to 127.0.0.1 : "+port);
		Debug.Log("Test-Sending to this Port: nc -u 127.0.0.1  "+port+"");
		
		
		// ----------------------------
		// Abhören
		// ----------------------------
		// Lokalen Endpunkt definieren (wo Nachrichten empfangen werden).
		// Einen neuen Thread für den Empfang eingehender Nachrichten erstellen.
		receiveThread = new Thread(
			new ThreadStart(ReceiveData));
		receiveThread.IsBackground = true;
		receiveThread.Start();
		
	}
	
	// receive thread
	private  void ReceiveData()
	{
		
		client = new UdpClient(port);
		while (true)
		{
			
			try
			{
				// Bytes empfangen.
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = client.Receive(ref anyIP);
				
				// Bytes mit der UTF8-Kodierung in das Textformat kodieren.
				string text = Encoding.UTF8.GetString(data);
				
				// Den abgerufenen Text anzeigen.
				Debug.Log(">> " + text);
				
				// latest UDPpacket
				lastReceivedUDPPacket=text;
				
				// ....
				allReceivedUDPPackets=allReceivedUDPPackets+text;
				
			}
			catch (Exception err)
			{
				Debug.Log(err.ToString());
			}
		}
	}
	
	// getLatestUDPPacket
	// cleans up the rest
	public string getLatestUDPPacket()
	{
		allReceivedUDPPackets="";
		return lastReceivedUDPPacket;
	}



	
	public string GetLocalIP()
	{
		string strHostName = "";
		strHostName = System.Net.Dns.GetHostName();
		
		IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);
		
		IPAddress[] addr = ipEntry.AddressList;
		
		return addr[addr.Length-1].ToString();
		
	}
}

