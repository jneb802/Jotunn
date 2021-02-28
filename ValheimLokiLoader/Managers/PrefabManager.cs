﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public static class PrefabManager
    {
        public static event EventHandler PrefabLoad;
        public static GameObject prefabContainer;
        internal static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        private static bool loaded = false;

        internal static void Init()
        {
            prefabContainer = new GameObject("_LokiPrefabs");
            prefabContainer.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(prefabContainer);

            Debug.Log("Initialized PrefabManager");
        }

        internal static void LoadPrefabs()
        {
            Debug.Log("---- Registering custom prefabs ----");

            // Call event handlers to load prefabs
            PrefabLoad?.Invoke(null, EventArgs.Empty);

            var namedPrefabs = Util.GetPrivateField<Dictionary<int, GameObject>>(ZNetScene.instance, "m_namedPrefabs");

            // Load prefabs into game
            foreach (var pair in Prefabs)
            {
                GameObject prefab = pair.Value;

                ZNetScene.instance.m_prefabs.Add(prefab);
                namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);

                Debug.Log("Added prefab: " + pair.Key);
            }

            loaded = true;
        }

        public static void AddPrefab(GameObject prefab, string name)
        {
            if (GetPrefab(name))
            {
                Debug.LogError("Prefab already exists: " + name);
                return;
            }

            prefab.name = name;
            prefab.transform.parent = prefabContainer.transform;
            prefab.SetActive(true);
            Prefabs.Add(name, prefab);
        }

        public static GameObject CreatePrefab(string name)
        {
            GameObject prefab = new GameObject(name);
            prefab.transform.parent = prefabContainer.transform;

            // TODO: Add any components required for it to work as a prefab (ZNetView, etc?)
            Prefabs.Add(name, prefab);

            return prefab;
        }

        public static GameObject CreatePrefab(string name, string baseName)
        {
            if (GetPrefab(name))
            {
                Debug.LogError("Prefab already exists: " + name);
                return null;
            }

            GameObject prefabBase = GetPrefab(baseName);

            if (!prefabBase)
            {
                Debug.LogError("Prefab base does not exist: " + baseName);
                return null;
            }

            GameObject prefab = UnityEngine.Object.Instantiate(prefabBase, prefabContainer.transform);
            prefab.name = name;
            prefab.SetActive(true);
            Prefabs.Add(name, prefab);

            return prefab;
        }

        
        public static GameObject GetPrefab(string name)
        {
            if (Prefabs.ContainsKey(name))
            {
                return Prefabs[name];
            }

            if (!ZNetScene.instance)
            {
                Debug.LogError("\t-> ZNetScene instance null for some reason");
                return null;
            }

            foreach (GameObject obj in ZNetScene.instance.m_prefabs)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}
