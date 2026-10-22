using System.Linq;
using DV;
using DV.ThingTypes;
using Mapify.Editor;
using UnityEngine;

namespace Mapify.SceneInitializers.Railway
{
    public class LocomotiveSpawnerSetup : SceneSetup
    {
        public override void Run()
        {
            foreach (LocomotiveSpawner spawner in Object.FindObjectsOfType<LocomotiveSpawner>()) SetupLocomotiveSpawner(spawner);
        }

        public static void SetupLocomotiveSpawner(LocomotiveSpawner spawner)
        {
            bool wasActive = spawner.gameObject.activeSelf;
            spawner.gameObject.SetActive(false);
            StationLocoSpawner locoSpawner = spawner.gameObject.AddComponent<StationLocoSpawner>();
            locoSpawner.spawnRotationFlipped = spawner.flipOrientation;
            locoSpawner.locoSpawnTrackName = spawner.Track.name;
            locoSpawner.locoTypeGroupsToSpawn = spawner.condensedLocomotiveTypes
                .Select(rollingStockTypes =>
                    new ListTrainCarTypeWrapper(
                        rollingStockTypes.Split(',')
                        .Select(FindLiveryForTrainType)
                        .ToList()
                    )
                ).ToList();
            spawner.gameObject.SetActive(wasActive);
        }

        private static TrainCarLivery FindLiveryForTrainType(string trainTypeID)
        {
            var livery = Globals.G.Types.Liveries.Find(livery => livery.parentType.id == trainTypeID);
            if (livery == default)
            {
                Mapify.LogError($"{nameof(LocomotiveSpawnerSetup)}.{nameof(FindLiveryForTrainType)}: could not find livery for train type ID {trainTypeID}");
            }

            return livery;
        }
    }
}
