using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Unity.Jobs;

namespace nugen.rigging
{

    [ExecuteInEditMode]
    public class DriverController : MonoBehaviour
    {

        public bool clearDrivers = false;

        public List<DriverLogic.IDriver> drivers;

        private List<DriverLogic.IDriver> updatedDrivers;

        private DriverJob.Handler jobHandler;

        public bool serialize = false;

        public bool deserialize = false;

        [HideInInspector]
        public byte[] serializedDrivers;

        public void SerializeDrivers()
        {

            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, drivers);
                serializedDrivers = stream.ToArray();
            }

        }

        public void DeserializeDrivers()
        {

            if (serializedDrivers == null)
            {

                Debug.LogWarning("No Drivers serialized for controller " + gameObject.name);

                return;

            }

            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(serializedDrivers))
            { 
                drivers = (List<DriverLogic.IDriver>)formatter.Deserialize(stream);
            }

#if !UNITY_EDITOR

            serializedDrivers = null;

#endif

        }

        public virtual void start()
        {

            if (!Application.isPlaying) return;

            DeserializeDrivers();

            if (drivers != null) foreach (DriverLogic.IDriver driver in drivers) driver?.Initialize(gameObject);

        }

        public virtual void update()
        {

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            if (updatedDrivers == null) updatedDrivers = new List<DriverLogic.IDriver>();

            updatedDrivers.Clear();

            foreach (DriverLogic.IDriver driver in drivers) if (driver != null && driver.HasUpdated()) updatedDrivers.Add(driver);

            jobHandler = DriverJob.Create(updatedDrivers);

            jobHandler.ScheduleLocal(new JobHandle());

        }

        public virtual void lateUpdate()
        {

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            jobHandler.AwaitCallback();

            jobHandler = null;

            foreach (DriverLogic.IDriver driver in updatedDrivers) driver?.Apply();

        }

        public virtual void onDestroy()
        {

            if (drivers != null) foreach (DriverLogic.IDriver driver in drivers) driver?.Dispose();

        }

        void Start()
        {

            start();

        }

        private void OnDestroy()
        {

            onDestroy();

        }

        void Update()
        {

            update();

        }

        private void LateUpdate()
        {

            lateUpdate();

        }

        private void OnValidate()
        {

            if (clearDrivers && drivers != null) 
            {

                clearDrivers = false;

                Debug.Log("Clearing " + drivers.Count + " drivers");

                drivers.Clear();
            
            }

            if (serialize && drivers != null)
            {

                serialize = false;

                SerializeDrivers();

                Debug.Log("Serialized " + drivers.Count + " drivers");

            }

            if (deserialize)
            {

                deserialize = false;

                DeserializeDrivers();

                Debug.Log("Deserialized " + drivers.Count + " drivers");

            }

        }

    }

}
