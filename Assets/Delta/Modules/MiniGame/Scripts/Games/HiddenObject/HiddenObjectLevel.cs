using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Delta.Modules.MiniGame
{
    public class HiddenObjectLevel : MonoBehaviour
    {
        public Action<int> OnObjectCompleted;
        public List<DragObject> DragObjects;
        public List<DragObject> KeyObjects;
        public List<DragObject> Keys;

        public void SetUp(int num,float canvasScale)
        {
            foreach (var obj in DragObjects)
            {
                obj.SetUp(false,canvasScale);
            }
            if (KeyObjects.Count > num)
            {
                Keys = KeyObjects.OrderBy(x => UnityEngine.Random.value).Take(num).ToList();
            }
            foreach (var obj in Keys)
            {
                obj.SetUp(true,canvasScale);
                obj.OnKeyObjectComplete = ObjectComplete;
            }
            Keys.First().FindThisObject();
        }

        private void ObjectComplete(int index)
        {
            OnObjectCompleted?.Invoke(index);
        }
    }
}