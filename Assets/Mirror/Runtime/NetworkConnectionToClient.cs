using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    public class NetworkConnectionToClient : NetworkConnection
    {
        public override string address =>
            Transport.activeTransport.ServerGetClientAddress(connectionId);

        /// <summary>NetworkIdentities that this connection can see</summary>
        // TODO move to server's NetworkConnectionToClient?
        public new readonly HashSet<NetworkIdentity> observing = new HashSet<NetworkIdentity>();


        public int EmptyIndex = 0;
        public NetworkIdentity[] DirtyObserving = new NetworkIdentity[10000];

        /// <summary>All NetworkIdentities owned by this connection. Can be main player, pets, etc.</summary>
        // IMPORTANT: this needs to be <NetworkIdentity>, not <uint netId>.
        //            fixes a bug where DestroyOwnedObjects wouldn't find the
        //            netId anymore: https://github.com/vis2k/Mirror/issues/1380
        //            Works fine with NetworkIdentity pointers though.
        public new readonly HashSet<NetworkIdentity> clientOwnedObjects = new HashSet<NetworkIdentity>();

        // unbatcher
        public Unbatcher unbatcher = new Unbatcher();

        public NetworkConnectionToClient(int networkConnectionId)
            : base(networkConnectionId) {}

        // Send stage three: hand off to transport
        protected override void SendToTransport(ArraySegment<byte> segment, int channelId = Channels.Reliable) =>
            Transport.activeTransport.ServerSend(connectionId, segment, channelId);

        /// <summary>Disconnects this connection.</summary>
        public override void Disconnect()
        {
            // set not ready and handle clientscene disconnect in any case
            // (might be client or host mode here)
            isReady = false;
            Transport.activeTransport.ServerDisconnect(connectionId);

            // IMPORTANT: NetworkConnection.Disconnect() is NOT called for
            // voluntary disconnects from the other end.
            // -> so all 'on disconnect' cleanup code needs to be in
            //    OnTransportDisconnect, where it's called for both voluntary
            //    and involuntary disconnects!
        }

        public void AddDirty(NetworkIdentity Dirty)
        {
            if (EmptyIndex >= DirtyObserving.Length)
            {
                Debug.LogError($" Having to expand observer array expensive!!! how many do you have!!?!? {EmptyIndex} adding 1000");
                Array.Resize(ref DirtyObserving, DirtyObserving.Length + 1000);
            }
            DirtyObserving[EmptyIndex] = Dirty;
            EmptyIndex++;
        }


        public void RemoveDirty(NetworkIdentity RemovingDirty)
        {
            var IndexAt = 0;

            for (int i = 0; i < EmptyIndex; i++)
            {
                if (DirtyObserving[i] == RemovingDirty)
                {
                    IndexAt = i;
                    break;
                }
            }

            if (DirtyObserving[IndexAt] != RemovingDirty) return;

            if (EmptyIndex - 1 == IndexAt)
            {
                DirtyObserving[IndexAt] = null;
                EmptyIndex--;
            }
            else
            {
                DirtyObserving[IndexAt] = DirtyObserving[EmptyIndex - 1];
                DirtyObserving[EmptyIndex - 1] = null;
                EmptyIndex--;
            }

        }

        internal void AddToObserving(NetworkIdentity netIdentity)
        {
            observing.Add(netIdentity);
            AddDirty(netIdentity);
            // spawn identity for this conn
            NetworkServer.ShowForConnection(netIdentity, this);
        }

        internal void RemoveFromObserving(NetworkIdentity netIdentity, bool isDestroyed)
        {
            observing.Remove(netIdentity);
            RemoveDirty(netIdentity);
            if (!isDestroyed)
            {
                // hide identity for this conn
                NetworkServer.HideForConnection(netIdentity, this);
            }
        }

        internal void RemoveFromObservingsObservers()
        {
            foreach (NetworkIdentity netIdentity in observing)
            {
                netIdentity.RemoveObserver(this);
                RemoveDirty(netIdentity);
            }
            observing.Clear();
        }

        internal void AddOwnedObject(NetworkIdentity obj)
        {
            clientOwnedObjects.Add(obj);
        }

        internal void RemoveOwnedObject(NetworkIdentity obj)
        {
            clientOwnedObjects.Remove(obj);
        }

        internal void DestroyOwnedObjects()
        {
            // create a copy because the list might be modified when destroying
            HashSet<NetworkIdentity> tmp = new HashSet<NetworkIdentity>(clientOwnedObjects);
            foreach (NetworkIdentity netIdentity in tmp)
            {
                if (netIdentity != null)
                {
                    NetworkServer.Destroy(netIdentity.gameObject);
                }
            }

            // clear the hashset because we destroyed them all
            clientOwnedObjects.Clear();
        }
    }
}
