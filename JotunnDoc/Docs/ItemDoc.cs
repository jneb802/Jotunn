﻿using System;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace JotunnDoc.Docs
{
    public class ItemDoc : Doc
    {
        public ItemDoc() : base("objects/item-list.md")
        {
            ItemManager.OnItemsRegistered += DocItems;
        }

        private void DocItems()
        {
            if (Generated)
            {
                return;
            }

            Jotunn.Logger.LogInfo("Documenting items");

            AddHeader(1, "Item list");
            AddText("All of the items currently in the game, with English localizations applied");
            AddText("This file is automatically generated from Valheim using the JotunnDoc mod found on our GitHub.");
            AddTableHeader("Prefab", "Token", "Name", "Type", "Description");

            foreach (GameObject obj in ObjectDB.instance.m_items.Where(x => !CustomItem.IsCustomItem(x.name)))
            {
                ItemDrop item = obj.GetComponent<ItemDrop>();
                ItemDrop.ItemData.SharedData shared = item.m_itemData.m_shared;

                AddTableRow(
                    obj.name,
                    shared.m_name,
                    JotunnDoc.Localize(shared.m_name),
                    shared.m_itemType.ToString(),
                    JotunnDoc.Localize(shared.m_description));
            }

            Save();
        }
    }
}
