using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Network
{
	/// <summary>
	/// [Undocumented]
	/// </summary>
	public class NetworkManager
	{
		#region Data Codes
		/* When data is received, the first byte will be decoded into a character.
		 * The action taken with the data depends on the code.
		 * 
		 * 
		 * A  - A remote NetworkManager has just added a Sprite to its localControlledSprites.
		 *      The Sprite needs to be found in the SpriteManager and added in the remoteControlledSprites
		 * 
		 * I  - The contained information is an ID.  Only the server can send this out, and 
		 *      receiving this packet specifies that the computer ID should change.
		 * 
		 * M  - A remoteControlledSprite is being updated and the packet contains
		 *      multiple variable updates.  One reason to send updates this way is to
		 *      reduce the amount of bandwidth used in resending individual packets;
		 *		each packet has the overhead of sending all of the generic packet
		 *		information and resending the Sprite ID.  Also, although this doesn't
		 *		make much of a difference, it slightly reduces the amount of processing
		 *		on both the sender and receiver's side in having to find the same Sprite
		 *		multiple times by its ID.  Perhaps the most important thing is that it
		 *		allows for sending updates at the same time.  This is important when
		 *		adding Sprites; if the Sprite's Add packet is received and processed
		 *		before other packets, the Sprite will be added at the origin and sit there
		 *		until the rest of the packets arrive.  This flashing of Sprites 
		 *		is undesirable even if the flash is very fast.  On non-LAN games, this
		 *		flashing can be worse.
		 * 
		 *		Furthermore, when an Add instruction arrives and the Sprite is created
		 *		the receiving computer may want to categorize, change, or apply
		 *		some behavior depending on the kind of Sprite that has arrived.  This
		 *		selection should not occur on the ID because the ID should be transparent
		 *		to the user.  If packets arrive at differen times, the newly added
		 *		Sprite may not receive all of its attributes which are remotely sent until
		 *		later, and ideally, the networkManager should keep track of added Sprites
		 *		for only one frame.
		 * N  - The sending computer is telling the remote computer its name.  The
         *      name should be added to the namesConnectedTo array.
         * P  - Ping from originator.  If this is received, send back a reply (code R).
         * R  - Reply ping to originator.  
		 * U  - A remoteControlledSprite is being updated.  The format will then be:
		 *      U<spriteName>\n<variableName>\t<value>*   Note that the command ends with a *
		 *      because it's possible for multiple commands to come in at once.
		 *
		 * \t - The data is simple text that should be added to the receivedText 
		 *      StringArray.
		 * 
         * a - The data is a packet that can be Deserialized and added to the packets.
         * 
		 * b - The data is a byte array that should be added to the recievedBytes
		 * 
		 * c - confirmation that a remote addition has been made.  Format will be
		 *     c[oldID][newID]
		 */

		public enum SpriteVars
		{
			
			animate,
			animationSpeed,
			bAllowance,
			blend,
			bounceCoefficient,
			colorOperation,
			currentFrame,
			drag,
			emitting,
			fadeRate,
			keepTrackofReal,
			lAllowance,
			mass,
			name,
			parentRotationChangesPosition,
			parentRotationChangesRotation,
			rAllowance,
			relRotXVelocity,
			relRotYVelocity,
			relRotationZVelocity,
			RelativeX,
			relXAcceleration,
			relXVelocity,
			RelativeY,
			relYAcceleration,
			relYVelocity,
			RelativeZ,
			relZAcceleration,
			relZVelocity,
			rotXVelocity,
			rotYVelocity,
			RotationZVelocity,
			ScaleX,
			ScaleXVelocity,
			ScaleY,
			ScaleYVelocity,
			tAllowance,
			tintBlueRate,
			tintGreenRate,
			tintRedRate,
			type,
			visible,
			X,
			xAcceleration,
			xFlip,
			xVelocity,
			Y,
			yAcceleration,
			yFlip,
			yVelocity,
			Z,
			zAcceleration,
			zVelocity,

			pixelSize,
			relRotX,
			relRotY,
			relRotationZ,
            RotationX,
			RotationY,
			RotationZ,
			state,
			texture,
			tintBlue,
			tintGreen,
			tintRed,

			Add, AddRemote,
			Attach,
			Remove
		}
		#endregion

        #region Enums
        public enum NetworkManagerState
        {
            Disconnected,
            ConnectedAsHost,
            ConnectedAsClient,
            AttemptingConnection,
            ConnectAsClientFailed,
            AwaitingClient,
            ClientForciblyDisconnected,
            HostForciblyDisconnected
        }
        #endregion

        #region Fields

        #region Diagnostic Members
        List<string> mFrameSentPackets;
        #endregion

		Socket local;
		Socket remote;

        double mSecondsLastPingTook;
        double mTickLastPingSent;
        double mTickOfLastReceievedPing;
        bool mPingingEnabled;

		private byte[] data = new byte[1024 * 16];
		List<string> mReceivedText;
		List<byte[]>   mReceivedBytes;
//		private int size = 1024;

		public List<int> connectedTo;
        List<string> mNamesConnectedTo;
		List<NetSprite> localControlledSprites;
		List<NetSprite> remoteControlledSprites;
		List<NetSprite> pendingAddRemoteSprites;

        ulong mTotalPackets = 0;
        List<BasePacket> mReceivedPackets;

		public IPHostEntry localIP;

		NetworkManagerState mState;
		string mComputerName;

		ushort mComputerID;
		int nextComputerID = 1;
		//int nextSpriteID = 0;

        List<DelayedByteArray> mDelayedByteArrays;
        float mAdditionalLag;

        MemoryStream dataToSend;

//        string mIPAddress;

        
		#endregion

        #region Events

        public SpriteCustomBehavior onRemoteCreate;
        public SpriteCustomBehavior onRemoteName;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// The amount of time in seconds that the NetworkManager will hold information before sending it.
        /// This is used to help simulate lag for debugging.
        /// </summary>
        #endregion
        public float AdditionalLag
        {
            get { return mAdditionalLag; }
            set { mAdditionalLag = value; }
        }

        public bool ConnectedAsClient
        {
            get { return mState == NetworkManagerState.ConnectedAsClient; }
        }

        public bool ConnectedAsHost
        {
            get { return mState == NetworkManagerState.ConnectedAsHost; }
        }

        public ushort ComputerID
		{
			get{ return mComputerID;}
		}

        public string ComputerName
        {
            get { return mComputerName; }
            set { mComputerName = value; }
        }
               
        public bool IsConnected
        {
            get
            {
                return State == NetworkManagerState.ConnectedAsClient ||
                              State == NetworkManagerState.ConnectedAsHost;
            }
        }

        public double SecondsLastPingTook
        {
            get { return mSecondsLastPingTook; }
            set { mSecondsLastPingTook = value; }

        }

        public List<Byte[]> ReceivedBytes
        {
            get { return mReceivedBytes; }
        }

        public List<BasePacket> ReceivedPackets
        {
            get { return mReceivedPackets; }
        }

        public List<string> ReceivedText
        {
            get { return mReceivedText; }
        }

        public List<string> RemoteComputerNames
        {
            get { return mNamesConnectedTo; }
        }

        public NetworkManagerState State
        {
            get { return mState; }

        }

        public ulong TotalLifetimeReceivedPackets
        {
            get { return mTotalPackets; }
        }

        #endregion

		#region Methods


        #region Constructor
        public NetworkManager()
		{
            mFrameSentPackets = new List<string>();
			mComputerName = Dns.GetHostName();
			
#if FRB_MDX
            localIP = Dns.GetHostByName(mComputerName);
#else
            localIP = Dns.GetHostEntry(mComputerName);
#endif
			mReceivedText = new List<string>();
			mReceivedBytes = new List<Byte[]>();
            mReceivedPackets = new List<BasePacket>();

			local = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);

			
			localControlledSprites = new List<NetSprite>();
			remoteControlledSprites = new List<NetSprite>();
			pendingAddRemoteSprites = new List<NetSprite>();

			mState = NetworkManagerState.Disconnected;

			mComputerID = 0;

			connectedTo = new List<int>();
            mNamesConnectedTo = new List<string>();

            mDelayedByteArrays = new List<DelayedByteArray>();
            dataToSend = new MemoryStream();

        }
        #endregion


        #region Static Methods

        #region Encoding

        public static void Encode(string stringToEncode, MemoryStream ms)
        {
            ms.Write(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(stringToEncode)), 0, 4);

            ms.Write(Encoding.ASCII.GetBytes(stringToEncode), 0,
                     Encoding.ASCII.GetByteCount(stringToEncode));
        }

        #endregion

        #region Decoding

        public static void Decode(ref float targetFloat, byte[] data, ref int offset)
        {
            targetFloat = BitConverter.ToSingle(data, offset);
            offset += sizeof(float);
        }

        public static void Decode(ref Int32 targetInt, byte[] data, ref int offset)
        {
            targetInt = BitConverter.ToInt32(data, offset);
            offset += sizeof(Int32);
        }

        public static void Decode(ref string targetString, byte[] data, ref int offset)
        {
            int stringLength = 0;
            Decode(ref stringLength, data, ref offset);

            targetString = Encoding.ASCII.GetString(data, offset, stringLength);
            offset += stringLength;
        }

        public static void Decode(ref long targetLong, byte[] data, ref int offset)
        {
            targetLong = BitConverter.ToInt64(data, offset);
            offset += sizeof(Int64);
        }

        #endregion

        #endregion


        #region Public Methods
        

        public void Activity()
		{
			#region Awaiting client
			if(mState == NetworkManagerState.AwaitingClient)
			{
				if(local.Poll(2000, SelectMode.SelectRead))
				{
					remote = local.Accept();

					remote.SetSocketOption( System.Net.Sockets.SocketOptionLevel.Tcp,
						System.Net.Sockets.SocketOptionName.NoDelay, 1);


					mState = NetworkManagerState.ConnectedAsHost;

					MemoryStream ms = new MemoryStream();

                    // the size of the packet always goes first
					ms.Write(BitConverter.GetBytes(5), 0, 4);
                    // then the identifier
					ms.WriteByte((byte)('I'));
                    // then the contents
					ms.Write(BitConverter.GetBytes(this.nextComputerID), 0, 4);
                    // finally, send it on its way
					remote.Send(ms.ToArray(),  (int)ms.Length, 0);

                    ms = new MemoryStream();

                    ms.Write(BitConverter.GetBytes(1 + this.mComputerName.Length), 0, 4);
                    ms.WriteByte((byte)('N'));
                    ms.Write(Encoding.ASCII.GetBytes(mComputerName), 0, mComputerName.Length);
                    remote.Send(ms.ToArray(), (int)ms.Length, 0);

                    mNamesConnectedTo.Add("");
                    this.connectedTo.Add(nextComputerID);




					nextComputerID++;
				}
			}
				#endregion
			#region connected as a host
			else if(mState == NetworkManagerState.ConnectedAsHost)
			{
				if(remote.Poll(400, SelectMode.SelectRead))
				{
                    try
                    {
                        int recv = remote.Receive(data);
                        if (recv == 0)
                        {
                            mState = NetworkManagerState.Disconnected;
                            remote.Close();
                        }
                        else
                        {
                            int location = 0;
                            while (BitConverter.ToInt32(data, location) != 0)
                            {
                                this.DecodeData(data, location + 4, BitConverter.ToInt32(data, location), 1);
                                location += BitConverter.ToInt32(data, location) + 4;
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        switch (e.ErrorCode)
                        {
                            case 10054:
                                mState = NetworkManagerState.ClientForciblyDisconnected;
                                break;
                        }
                        string s = e.ToString();
                    }
				}
			}
				#endregion
			#region connected as a client
			else if(mState == NetworkManagerState.ConnectedAsClient)
			{
				if(remote.Poll(400, SelectMode.SelectRead))
				{
                    int recv = 0;

                    try
                    {
                        recv = remote.Receive(data);
                    }
                    catch
                    {
                        recv = 0;
                    }


                    if (recv == 0) // this occurs when the host disconnects or when the connection is lost
                    {
                        mState = NetworkManagerState.HostForciblyDisconnected;

                        try
                        {
                            remote.Shutdown(SocketShutdown.Both);
                            remote.Disconnect(false);
                        }
                        catch
                        {
                            // if we fail here, no big deal.  We're already disconnected
                        }

                    }
                    else
                    {
                        int location = 0;

                        List<string> actionsPerformed = new List<string>();

                        try
                        {

                            while (BitConverter.ToInt32(data, location) != 0)
                            {
                                actionsPerformed.Add(
                                    this.DecodeData(data, location + 4, BitConverter.ToInt32(data, location), 0));
                                location += BitConverter.ToInt32(data, location) + 4;

                                if (location > recv)
                                {
                                    StreamWriter sr = new StreamWriter("netManErrorDecodeData.txt");
                                    sr.WriteLine("Tried to read beyond the end of the data");

                                    sr.WriteLine(actionsPerformed[actionsPerformed.Count - 1]);

                                    sr.Close();
                                    break;
                                }
                            }
                            // at this point, the location should equal the received amount
                            if (location != recv)
                            {
                                StreamWriter sr = new StreamWriter("netManErrorDecodeData.txt");
                                sr.WriteLine("Didn't receive data to fill all of the actions.");

                                foreach (string s in actionsPerformed)
                                    sr.WriteLine(s);

                                sr.Close();
                                
                            }
                        }
                        catch (Exception e)
                        {
                            StreamWriter sr = new StreamWriter("netManErrorDecodeData.txt");
                            sr.Write("Error trying to decode data received from remote computer. \n");
                            sr.Write(e.ToString());
                            sr.WriteLine();
                            sr.WriteLine("Actions performed: ");
                            foreach (String s in actionsPerformed)
                                sr.WriteLine(" " + s);
                            sr.WriteLine();
                            sr.WriteLine("data length" + data.Length);
                            sr.Write("data: " + data + "\n\n\n");
                            sr.WriteLine("location: " + location);

                            sr.Close();
                        }
                    }
				}
			}
			#endregion

            #region General Activity when connected
            if (IsConnected)
            {
                #region Is there buffered data to send?
                if (dataToSend.Length != 0)
                {
                    if (AdditionalLag != 0)
                    {
                        mDelayedByteArrays.Add(new DelayedByteArray(
                            dataToSend.ToArray(), (TimeManager.CurrentTime + AdditionalLag)));
                    }
                    else
                    {
                        try
                        {
                            remote.Send(dataToSend.ToArray(), (int)dataToSend.Length, 0);
                        }
                        catch (SocketException )
                        {
                            Disconnect();
                        }
                    }
                    dataToSend = new MemoryStream();

                    mFrameSentPackets.Clear();
                }
                #endregion

                #region See if it's time to send off the delayed packets.
                while (mDelayedByteArrays.Count != 0 && mDelayedByteArrays[0].TimeToSend <
                    TimeManager.CurrentTime)
                {
                    remote.Send(mDelayedByteArrays[0].ByteArray,
                                mDelayedByteArrays[0].ByteArray.Length, 0);

                    mDelayedByteArrays.RemoveAt(0);
                }
                #endregion

                #region See if pinging is enabled and it's time to ping

                if (mPingingEnabled && TimeManager.CurrentTime - mTickLastPingSent > .5f)
                    SendPing();
                #endregion
            }
            #endregion
        }


		public void Connect(string ipAddress)
		{
			try
			{
				mState = NetworkManagerState.AttemptingConnection;
				IPEndPoint iep = new IPEndPoint(System.Net.IPAddress.Parse(ipAddress), 9050);

				remote = new Socket(AddressFamily.InterNetwork,
					SocketType.Stream, ProtocolType.Tcp);

				remote.SetSocketOption( System.Net.Sockets.SocketOptionLevel.Tcp,
					System.Net.Sockets.SocketOptionName.NoDelay, 1);
				
				remote.Connect(iep);

                mNamesConnectedTo.Add("");
				connectedTo.Add(0);
				mState = NetworkManagerState.ConnectedAsClient;

                MemoryStream ms = new MemoryStream();

                ms.Write(BitConverter.GetBytes(1 + this.mComputerName.Length), 0, 4);
                ms.WriteByte((byte)('N'));
                ms.Write(Encoding.ASCII.GetBytes(mComputerName), 0, mComputerName.Length);
                remote.Send(ms.ToArray(), (int)ms.Length, 0);


			}
			catch(Exception )
			{
				mState = NetworkManagerState.ConnectAsClientFailed;
			}
		}


        public void Disconnect()
        {
            if (remote != null && remote.Connected)
            {
                remote.Shutdown(SocketShutdown.Both);
                remote.Disconnect(false);
                mState = NetworkManagerState.Disconnected;

                try
                {
                    local.Shutdown(SocketShutdown.Both);
                    local.Disconnect(false);
                }
                catch
                {
                    // no big deal - already disconnected
                }
            }
            else if (State == NetworkManagerState.AwaitingClient)
                mState = NetworkManagerState.Disconnected;
        }


        public Sprite GetRemoteControlledSprite(int ID, ushort computerID)
        {
            foreach (NetSprite s in this.remoteControlledSprites)
                if (s.spriteID == ID && s.computerID == computerID)
                    return s.sprite;
            return null;

        }


        public Sprite GetRemoteControlledSprite(string name, ushort computerID)
        {
            foreach (NetSprite s in remoteControlledSprites)
                if (s.computerID == computerID && s.sprite.Name == name)
                    return s.sprite;
            return null;
        }


        public Sprite GetLastLocalSpriteAdded()
        {
            return ((NetSprite)this.localControlledSprites[localControlledSprites.Count - 1]).sprite;

        }


        public Sprite GetLocalSprite(int ID)
        {
            foreach (NetSprite s in this.localControlledSprites)
                if (s.spriteID == ID)
                    return s.sprite;

            return null;

        }


        public void Host()
        {
            // the host is always computer 0
            mComputerID = 0;
            // and whenever we host, next IDs should be reset:
            nextComputerID = 1;


            mState = NetworkManagerState.AwaitingClient;
            if (local.IsBound == false)
            {
                IPEndPoint iep = new IPEndPoint(System.Net.IPAddress.Any, 9050);
                local.Bind(iep);
            }
                
            local.Listen(10);
        }


        public void ResetDisconnectedState()
        {
            if (State == NetworkManagerState.ConnectAsClientFailed || State == NetworkManagerState.ClientForciblyDisconnected ||
                State == NetworkManagerState.HostForciblyDisconnected)
                mState = NetworkManagerState.Disconnected;
        }


        public void RemovePacket(BasePacket packet)
        {
            mReceivedPackets.Remove(packet);
        }


        public void SendBytes(byte[] bytesToSend)
        {
            dataToSend.Write(BitConverter.GetBytes(1 + bytesToSend.Length), 0, 4);
            dataToSend.Write(BitConverter.GetBytes('b'), 0, 1);
            dataToSend.Write(bytesToSend, 0, bytesToSend.Length);
        }


        public void SendPacket(BasePacket packetToSend)
        {
            dataToSend.Write(
                BitConverter.GetBytes(1 + packetToSend.SizeOfPacket),
                0, 4);
            dataToSend.Write(BitConverter.GetBytes('a'), 0, 1);
            packetToSend.Serialize(dataToSend);

            mFrameSentPackets.Add(packetToSend.ToString());

        }


        public void SendText(string textToSend)
        {
            dataToSend.Write(BitConverter.GetBytes(1 + textToSend.Length), 0, 4);
            dataToSend.Write(Encoding.ASCII.GetBytes('\t' + textToSend), 0, textToSend.Length + 1);
        }


        public void StartPinging()
        {
            if (IsConnected)
            {
                SendPing();
            }
            mPingingEnabled = true;
        }


        public override string ToString()
        {
            System.Text.StringBuilder sb = new StringBuilder();
            sb.Append("Current State:").Append(mState);
            sb.Append("\n# Byte Arrays:").Append(this.ReceivedBytes.Count);
            sb.Append("\n# Packets:").Append(this.ReceivedPackets.Count);
            sb.Append("\nSecondsLastPingTook:").Append(SecondsLastPingTook);
            sb.Append("\nTickOfLastReceivedPing:").Append(mTickOfLastReceievedPing);
            sb.Append("\nAdditionalLag:").Append(this.AdditionalLag);
            foreach (string s in mNamesConnectedTo)
                sb.Append("\nConnected to:").Append(s);

            return sb.ToString();
        }


        #endregion


        #region Private Methods


        string DecodeData(byte[] data, int start, int recv, ushort senderComputerID)
		{
			char code = Encoding.ASCII.GetString(data, start, 1)[0];
            MemoryStream ms = new MemoryStream();
           // Sprite s;

            switch (code)
            {
                    /* Remember, start marks the position where the code character is, so 
                     * always use start+1 when reading the actual data.
                     */
                #region 'P':  Received a ping sent the other guy
                case 'P': // received a ping from the other guy

                    // be sure to use start as the base number for reading from the data
                    mTickOfLastReceievedPing = BitConverter.ToInt64(data, start + 1);
                    // send the ping back as a reply ping
                    // the size of the packet always goes first
                    dataToSend.Write(BitConverter.GetBytes(9), 0, 4);
                    // then the identifier
                    dataToSend.WriteByte((byte)('R'));
                    // then the contents
                    dataToSend.Write(BitConverter.GetBytes(mTickOfLastReceievedPing), 0, 8);

                    return "Received and replied to ping.";

                    //break;
                #endregion

                #region 'R': Received response on sent ping, so mark how long it all took.
                case 'R':
                    SecondsLastPingTook = TimeManager.CurrentTime - BitConverter.ToDouble(data, start + 1);

                    
                    break;
                #endregion

                #region '\t': Regular text
                case '\t': // text is sent
                    this.mReceivedText.Add(Encoding.ASCII.GetString(data, start + 1, recv - 1));
                    return "Decoded text";
                    //break;
                #endregion

                #region 'N':  Remote computer renamed

                case 'N':
                    mNamesConnectedTo[connectedTo.IndexOf(senderComputerID)] =
                        Encoding.ASCII.GetString(data, start + 1, recv - 1);

                    return "Renamed Computer " + senderComputerID + " to " +
                        mNamesConnectedTo[connectedTo.IndexOf(senderComputerID)];

                    //break;

                #endregion

                #region 'a': Packet
                case 'a': // packet

                    MemoryStream tempStream = new MemoryStream();

                    tempStream.Write(data, start + 1, recv);
                    tempStream.Position = 0;

                    mTotalPackets++;
                    mReceivedPackets.Add(
                        BasePacket.Deserialize(tempStream));

                    return "Decoded and added packet to mReceivedPackets";

                    //break;
                #endregion

                #region 'b': Plain ol' bytes
                case 'b': // bytes
                    ms = new MemoryStream();

                    ms.Write(data, start + 1, recv - 1);
                    this.mReceivedBytes.Add(ms.ToArray());
                    return "Decoded bytes";
                    //break;
                #endregion


                case 'c':
                    int oldID = BitConverter.ToInt32(data, start + 1);
                    int newID = BitConverter.ToInt32(data, start + 5);

                    NetSprite netSprite = this.GetPendingSprite(oldID, senderComputerID);

                    this.pendingAddRemoteSprites.Remove(netSprite);

                    netSprite.spriteID = newID;
                    netSprite.computerID = senderComputerID;


                    this.remoteControlledSprites.Add(netSprite);

                    return "Decoded confirmation of Sprite ID change - moved pending to remote";

                    //break;
                case 'I':
                    mComputerID = (ushort)BitConverter.ToChar(data, start + 1);

                    return "Decoded assignment of computer ID assignment from the server";

                    //break;
                default:
                    return "Data not understood.\n" + 
                        "Prefix: " + code + "\n" +
                        "Start of data at: " + start + "\n" +
                        "Number of bytes received: " + recv + "\n";

                    //break;
            }

            return "Data not understood.\n" +
                "Prefix: " + code + "\n" +
                "Start of data at: " + start + "\n" +
                "Number of bytes received: " + recv + "\n";
        }


        public int GetPacketType(byte[] data, int offset)
        {
            int packetType = 0;
            Decode(ref packetType, data, ref offset);

            return packetType;

        }


		private NetSprite GetPendingSprite(int ID, ushort computerID)
		{
			foreach(NetSprite s in this.pendingAddRemoteSprites)
				if(s.spriteID == ID)
					return s;
			return null;

		}


        private void SendPing()
        {
            // the size of the packet always goes first
            dataToSend.Write(BitConverter.GetBytes(9), 0, 4);
            // then the identifier
            dataToSend.WriteByte((byte)('P'));
            // then the contents
            dataToSend.Write(BitConverter.GetBytes(TimeManager.CurrentTime), 0, 8);
            // mark when the ping was set
            this.mTickLastPingSent = TimeManager.CurrentTime;
        }


        #endregion

		

		public int GetID(Sprite s)
		{
			foreach(NetSprite ns in localControlledSprites)
				if(ns.sprite == s)
					return ns.spriteID;
			return -1;
		}


        public static bool IsValidIP(string s)
        {
            bool canBeDot = false;
            bool canBeNumber = true;

            int numberOfDots = 0;
            int currentNumberOfNumbers = 0;

            foreach (char c in s)
            {
                if (Char.IsNumber(c) && canBeNumber)
                {
                    currentNumberOfNumbers++;
                    canBeDot = true;
                }
                else if (c == '.' && canBeDot)
                {
                    currentNumberOfNumbers = 0;
                    numberOfDots++;
                }
                else
                    return false;

                canBeNumber = currentNumberOfNumbers < 3;
                canBeDot = currentNumberOfNumbers > 0 && numberOfDots < 3;
            }

            return numberOfDots == 3 && currentNumberOfNumbers > 0;
        }

		#endregion
	}
}
