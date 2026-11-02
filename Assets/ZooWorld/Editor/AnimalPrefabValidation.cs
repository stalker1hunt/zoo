using System;
using UnityEditor;
using UnityEngine;

namespace ZooWorld.Editor
{
    public static class AnimalPrefabValidation
    {
        public static void Validate(AnimalActor actor)
        {
            actor.ValidateConfiguration();
            if (!PrefabUtility.IsPartOfPrefabAsset(actor)) throw new InvalidOperationException("Animal reference must be a prefab asset.");
            if (actor.GetComponents<AnimalActor>().Length != 1 || actor.GetComponents<AnimalMovement>().Length != 1
                || actor.GetComponents<Rigidbody>().Length != 1 || actor.GetComponents<Collider>().Length != 1)
                throw new InvalidOperationException($"'{actor.name}' needs exactly one root actor, movement, Rigidbody and collider.");
            foreach (Transform child in actor.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                    throw new InvalidOperationException($"'{actor.name}' contains a Missing Script.");
                if (child != actor.transform && (child.GetComponent<AnimalActor>() != null || child.GetComponent<Rigidbody>() != null
                    || child.GetComponent<Collider>() != null || child.GetComponent<AnimalMovement>() != null))
                    throw new InvalidOperationException($"'{actor.name}' has gameplay physics/movement on a child; keep it on the assigned root.");
            }
        }
    }
}
