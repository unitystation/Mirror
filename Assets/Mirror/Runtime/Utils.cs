using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;

namespace Mirror
{
    // Handles network messages on client and server
    public delegate void NetworkMessageDelegate(NetworkConnection conn, NetworkReader reader, int channelId);

    // Handles requests to spawn objects on the client
    public delegate GameObject SpawnDelegate(Vector3 position, Guid assetId);

    public delegate GameObject SpawnHandlerDelegate(SpawnMessage msg);

    // Handles requests to unspawn objects on the client
    public delegate void UnSpawnDelegate(GameObject spawned);

    // channels are const ints instead of an enum so people can add their own
    // channels (can't extend an enum otherwise).
    //
    // note that Mirror is slowly moving towards quake style networking which
    // will only require reliable for handshake, and unreliable for the rest.
    // so eventually we can change this to an Enum and transports shouldn't
    // add custom channels anymore.
    public static class Channels
    {
        public const int Reliable = 0;   // ordered
        public const int Unreliable = 1; // unordered
    }

    public static class Utils
    {
        public static uint GetTrueRandomUInt()
        {
            // use Crypto RNG to avoid having time based duplicates
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                return BitConverter.ToUInt32(bytes, 0);
            }
        }

        public static bool IsPrefab(GameObject obj)
        {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj);
#else
            return false;
#endif
        }

        public static bool IsSceneObjectWithPrefabParent(GameObject gameObject, out GameObject prefab)
        {
            prefab = null;

#if UNITY_EDITOR
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                return false;
            }
            prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
#endif

            if (prefab == null)
            {
                Debug.LogError($"Failed to find prefab parent for scene object [name:{gameObject.name}]");
                return false;
            }
            return true;
        }

        // is a 2D point in screen? (from ummorpg)
        // (if width = 1024, then indices from 0..1023 are valid (=1024 indices)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInScreen(Vector2 point) =>
            0 <= point.x && point.x < Screen.width &&
            0 <= point.y && point.y < Screen.height;

        // universal .spawned function
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkIdentity GetSpawnedInServerOrClient(uint netId)
        {
            // server / host mode: use the one from server.
            // host mode has access to all spawned.
            if (NetworkServer.active)
            {
                NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity entry);
                return entry;
            }

            // client
            if (NetworkClient.active)
            {
                NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity entry);
                return entry;
            }

            return null;
        }

        /// <summary>
        /// UNITYSTATION CODE ///
        /// Gets the location of the gameobject in the scene hierarchy
        /// by building a path from the object to the root.
        /// </summary>
        /// <param name="obj">The object to build a path for</param>
        /// <returns>e.g. Grandparent/Parent/Object</returns>
        public static string GetGameObjectPath(GameObject obj)
        {
            var sb = new System.Text.StringBuilder(obj.name);

            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                sb.Insert(0, $"{obj.name}/");
            }

            return sb.ToString();
        }
    }
}
