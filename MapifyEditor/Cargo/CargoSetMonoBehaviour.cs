using System.Collections.Generic;
using UnityEngine;

namespace Mapify.Editor
{
    // I can't with Unity man...
    public class CargoSetMonoBehaviour : MonoBehaviour
    {
        [HideInInspector]
        public List<Cargo> cargoTypes;
        [HideInInspector]
        public List<string> customCargoTypes;
        [HideInInspector]
        public List<Station> stations;

        public CargoSet ToOriginal()
        {
            return new CargoSet {
                cargoTypes = this.cargoTypes,
                customCargoTypes = this.customCargoTypes,
                stations = this.stations
            };
        }
    }
}
