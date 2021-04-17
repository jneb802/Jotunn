﻿using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Utils;
using JotunnLib.Entities;
using JotunnLib.Configs;
using System.Linq;
using UnityEngine.UI;

namespace JotunnLib.Managers
{
    /// <summary>
    ///    Manager for handling items, recipes, and status effects added to the game.
    /// </summary>
    public class ItemManager : IManager
    {

        private static ItemManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static ItemManager Instance
        {
            get
            {
                if (_instance == null) _instance = new ItemManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Event that get fired after the ObjectDB get init and before its filled with custom items.
        ///     Your code will execute once unless you resub, the event get cleared after each fire.
        /// </summary>
        public static Action OnBeforeCustomItemsAdded;

        /// <summary>
        ///     Event that get fired after the ObjectDB get init and filled with custom items.
        ///     Your code will execute once unless you resub, the event get cleared after each fire.
        /// </summary>
        public static Action OnAfterInit;

        public event EventHandler OnItemsRegistered;

        internal readonly List<CustomItem> Items = new List<CustomItem>();
        internal readonly List<CustomRecipe> Recipes = new List<CustomRecipe>();
        internal readonly List<CustomStatusEffect> StatusEffects = new List<CustomStatusEffect>();


        public void Init()
        {
            On.ObjectDB.CopyOtherDB += RegisterCustomDataFejd;
            On.ObjectDB.Awake += RegisterCustomData;
            On.Player.Load += ReloadKnownRecipes;
        }

        /// <summary>
        ///     Add a <see cref="CustomItem"/> to the game.<br />
        ///     Checks if the custom item is valid and unique and adds it to the list of custom items.<br />
        ///     Also adds the prefab of the custom item to the <see cref="PrefabManager"/>.<br />
        ///     Custom items are added to the current <see cref="ObjectDB"/> on every <see cref="ObjectDB.Awake"/>.
        /// </summary>
        /// <param name="customItem">The custom item to add.</param>
        /// <returns>true if the custom item was added to the manager.</returns>
        public bool AddItem(CustomItem customItem)
        {
            if (!customItem.IsValid())
            {
                Logger.LogWarning($"Custom item {customItem} is not valid");
                return false;
            }
            if (Items.Contains(customItem))
            {
                Logger.LogWarning($"Custom item {customItem} already added");
                return false;
            }

            // Add prefab to the right layer
            if (customItem.ItemPrefab.layer == 0)
            {
                customItem.ItemPrefab.layer = LayerMask.NameToLayer("item");
            }

            // Add prefab to PrefabManager
            PrefabManager.Instance.AddPrefab(customItem.ItemPrefab);

            // Add custom item to ItemManager
            Items.Add(customItem);

            // Add custom recipe if provided
            if (customItem.Recipe != null)
            {
                AddRecipe(customItem.Recipe);
            }

            return true;
        }

        private void RegisterCustomItems(ObjectDB objectDB)
        {
            Logger.LogInfo($"---- Adding custom items to {objectDB} ----");

            foreach (var customItem in Items)
            {
                try
                {
                    var itemDrop = customItem.ItemDrop;

                    if (!itemDrop.m_itemData.m_dropPrefab)
                    {
                        itemDrop.m_itemData.m_dropPrefab = customItem.ItemPrefab;
                    }
                    if (customItem.FixReference)
                    {
                        customItem.ItemPrefab.FixReferences();
                        itemDrop.m_itemData.m_shared.FixReferences();
                        customItem.FixReference = false;
                    }
                    objectDB.m_items.Add(customItem.ItemPrefab);

                    Logger.LogInfo($"Added custom item : {customItem.ItemPrefab.name} | Token : {customItem.ItemDrop.TokenName()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding custom item {customItem.ItemPrefab.name}: {ex.Message}");

                    // Remove prefab, item and recipe from the managers again
                    PrefabManager.Instance.RemovePrefab(customItem.ItemPrefab.name);
                    Items.Remove(customItem);
                    if (customItem.Recipe != null)
                    {
                        Recipes.Remove(customItem.Recipe);
                    }
                }
            }

            Logger.LogInfo("Updating item hashes");

            objectDB.UpdateItemHashes();
        }

        /// <summary>
        ///     Add a <see cref="CustomRecipe"/> to the game.<br />
        ///     Checks if the custom recipe is unique and adds it to the list of custom recipes.<br />
        ///     Custom recipes are added to the current <see cref="ObjectDB"/> on every <see cref="ObjectDB.Awake"/>.
        /// </summary>
        /// <param name="customRecipe">The custom recipe to add.</param>
        /// <returns>true if the custom recipe was added to the manager.</returns>
        public bool AddRecipe(CustomRecipe customRecipe)
        {
            if (!customRecipe.IsValid())
            {
                Logger.LogWarning($"Custom recipe {customRecipe} is not valid");
                return false;
            }
            if (Recipes.Contains(customRecipe))
            {
                Logger.LogWarning($"Custom recipe {customRecipe} already added");
                return false;
            }

            Recipes.Add(customRecipe);
            return true;
        }

        /// <summary>
        ///     Adds recipes defined in a JSON file at given path, relative to BepInEx/plugins
        /// </summary>
        /// <param name="path">JSON file path, relative to BepInEx/plugins folder</param>
        public void AddRecipesFromJson(string path)
        {
            string json = AssetUtils.LoadText(path);

            if (string.IsNullOrEmpty(json))
            {
                Logger.LogError($"Failed to load recipes from json: {path}");
                return;
            }

            List<RecipeConfig> recipes = RecipeConfig.ListFromJson(json);

            foreach (RecipeConfig recipe in recipes)
            {
                AddRecipe(new CustomRecipe(recipe));
            }
        }

        private void RegisterCustomRecipes(ObjectDB objectDB)
        {
            Logger.LogInfo($"---- Adding custom recipes to {objectDB} ----");

            foreach (var customRecipe in Recipes)
            {
                try
                {
                    var recipe = customRecipe.Recipe;

                    if (customRecipe.FixReference)
                    {
                        recipe.FixReferences();
                        customRecipe.FixReference = false;
                    }

                    if (customRecipe.FixRequirementReferences)
                    {
                        foreach (var requirement in recipe.m_resources)
                        {
                            requirement.FixReferences();
                        }
                        customRecipe.FixRequirementReferences = false;
                    }
                    objectDB.m_recipes.Add(recipe);

                    Logger.LogInfo($"Added recipe for : {recipe.m_item.TokenName()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding custom recipe {customRecipe.Recipe.name}: {ex.Message}");

                    // Remove recipe from the manager again
                    Recipes.Remove(customRecipe);
                }

            }
        }

        /// <summary>
        ///     Add a <see cref="CustomStatusEffect"/> to the game.<br />
        ///     Checks if the custom status effect is unique and adds it to the list of custom status effects.<br />
        ///     Custom status effects are added to the current <see cref="ObjectDB"/> on every <see cref="ObjectDB.Awake"/>.
        /// </summary>
        /// <param name="customStatusEffect">The custom status effect to add.</param>
        /// <returns>true if the custom status effect was added to the manager.</returns>
        public bool AddStatusEffect(CustomStatusEffect customStatusEffect)
        {
            if (!customStatusEffect.IsValid())
            {
                Logger.LogWarning($"Custom status effect {customStatusEffect} is not valid");
                return false;
            }
            if (StatusEffects.Contains(customStatusEffect))
            {
                Logger.LogWarning($"Custom status effect {customStatusEffect} already added");
                return false;
            }

            StatusEffects.Add(customStatusEffect);
            return true;
        }

        private void RegisterCustomStatusEffects(ObjectDB objectDB)
        {
            Logger.LogInfo($"---- Adding custom status effects to {objectDB} ----");

            foreach (var customStatusEffect in StatusEffects)
            {
                try
                {
                    var statusEffect = customStatusEffect.StatusEffect;
                    if (customStatusEffect.FixReference)
                    {
                        statusEffect.FixReferences();
                        customStatusEffect.FixReference = false;
                    }

                    objectDB.m_StatusEffects.Add(statusEffect);

                    Logger.LogInfo($"Added status effect : {statusEffect.m_name}");

                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding custom status effect {customStatusEffect.StatusEffect.name}: {ex.Message}");

                    // Remove status effect
                    StatusEffects.Remove(customStatusEffect);
                }
            }
        }

        /// <summary>
        ///     Hook on <see cref="ObjectDB.CopyOtherDB"/> to add custom items to FejdStartup screen (aka main menu)
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="other"></param>
        private void RegisterCustomDataFejd(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            orig(self, other);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                OnBeforeCustomItemsAdded.SafeInvoke();
                OnBeforeCustomItemsAdded = null;

                RegisterCustomItems(self);

                self.UpdateItemHashes();

                OnAfterInit.SafeInvoke();
                OnAfterInit = null;
            }
        }

        /// <summary>
        ///     Hook on <see cref="ObjectDB.Awake"/> to register all custom entities from this manager to the <see cref="ObjectDB"/>.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void RegisterCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                OnBeforeCustomItemsAdded.SafeInvoke();
                OnBeforeCustomItemsAdded = null;

                RegisterCustomItems(self);
                RegisterCustomRecipes(self);
                RegisterCustomStatusEffects(self);

                self.UpdateItemHashes();

                OnAfterInit.SafeInvoke();
                OnAfterInit = null;
            }

            // Fire event that everything is added and registered
            OnItemsRegistered?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        ///     Hook on <see cref="Player.Load"/> to refresh recipes for the custom items.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="pkg"></param>
        private void ReloadKnownRecipes(On.Player.orig_Load orig, Player self, ZPackage pkg)
        {
            orig(self, pkg);

            if (Game.instance == null)
            {
                return;
            }

            self.UpdateKnownRecipesList();
        }
    }
}