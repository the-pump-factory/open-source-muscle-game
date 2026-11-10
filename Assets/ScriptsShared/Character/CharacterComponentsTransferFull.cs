using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity;

namespace Swole.Modding
{

    [ExecuteAlways]
    public class CharacterComponentsTransferFull : CharacterComponentsTransfer
    {

        public override void Transfer(GameObject rootToCopy, GameObject rootTarget)
        {
            if (rootTarget == null) rootTarget = gameObject;
            TransferComponentsFull(rootToCopy, rootTarget);
        }

        public static void TransferComponentsFull(GameObject rootToCopy, GameObject rootTarget)
        {
            CharacterComponentsTransfer.TransferComponents(rootToCopy, rootTarget);

            Transform rootToCopyTransform = rootToCopy.transform;
            Transform rootTransform = rootTarget.transform;

            Transform FindTransform(GameObject equivalent) => ReferenceEquals(rootToCopy, equivalent) ? rootTransform : rootTransform.FindDeepChild(equivalent.name);

            BipedalCharacterHeight[] bipedalCharacterHeights = rootToCopy.GetComponentsInChildren<BipedalCharacterHeight>(true);
            foreach (var controller in bipedalCharacterHeights)
            {
                var target = FindTransform(controller.gameObject);
                if (target == null) continue;

                var targetC = target.gameObject.GetComponent<BipedalCharacterHeight>(); 
                if (targetC != null) continue; 

                targetC = target.gameObject.AddComponent<BipedalCharacterHeight>();

                controller.CopySerializedDataTo(targetC, rootToCopyTransform, rootTransform); 
            }

            GenitalController[] genitalControllers = rootToCopy.GetComponentsInChildren<GenitalController>(true);
            foreach (var controller in genitalControllers)
            {
                var target = FindTransform(controller.gameObject);
                if (target == null) continue;

                var targetGC = target.gameObject.GetComponent<GenitalController>();
                if (targetGC != null) continue;

                targetGC = target.gameObject.AddComponent<GenitalController>();

                controller.CopySerializedDataTo(targetGC, rootToCopyTransform, rootTransform);
            }

        }

    }
}