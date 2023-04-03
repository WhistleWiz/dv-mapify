using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapify.Editor
{
    public class Station : MonoBehaviour
    {
        [Header("Station Info")]
        public string stationName;
        [FormerlySerializedAs("yardID")]
        [Tooltip("The 2-character ID of the station")]
        public string stationID;
        public Color color;
        [Tooltip("The location where the player will be teleport to when fast travelling")]
        public Transform teleportLocation;
        [Tooltip("The rough center of the yard. Used at the reference point for generating jobs")]
        public Transform yardCenter;

        [Header("Station Tracks")]
        public List<string> storageTrackNames;
        public List<string> transferInTrackNames;
        public List<string> transferOutTrackNames;

        [Header("Jobs")]
        [Tooltip("The area where job booklets should spawn")]
        public BoxCollider bookletSpawnArea;
        public int jobsCapacity = 30;
        public int maxShuntingStorageTracks = 3;
        public int minCarsPerJob = 3;
        public int maxCarsPerJob = 20;
        [Header("Cargo groups")]
        // public List<CargoGroup> inputCargoGroups;
        // public List<CargoGroup> outputCargoGroups;
        [Header("Starting chain job priorities")]
        public bool loadStartingJobSupported = true;
        public bool haulStartingJobSupported = true;
        public bool unloadStartingJobSupported = true;
        public bool emptyHaulStartingJobSupported = true;
    }
}
