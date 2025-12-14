using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AtlyssItemStackMod {

	[BepInPlugin("io.github.severnarch.atlyssitemstackmod",
				 "AtlyssItemStackMod",
				 "1.0.1"                                  )]
	public class AtlyssItemStackMod : BaseUnityPlugin {

		public static AtlyssItemStackMod Instance { get; private set; }
		private Harmony _harmony;

		private static Dictionary<string, int> _originalMaxStack;

		private void Awake() {
			Instance = this;
			_harmony = new Harmony("io.github.severnarch.atlyssitemstackmod");
			_originalMaxStack = new Dictionary<string, int>();

			var ScriptableItemCtor = AccessTools.Constructor(typeof(ScriptableItem));
			_harmony.Patch(ScriptableItemCtor,
				postfix: new HarmonyMethod(
					typeof(AtlyssItemStackMod).GetMethod(
						nameof(ScriptableItemCtorPostfix), 
						BindingFlags.NonPublic | BindingFlags.Static
					)
				)
			);
			var DropItemMthd = AccessTools.Method(typeof(ItemMenuCell), "PromptCmd_DropItem");
			_harmony.Patch(DropItemMthd,
				prefix: new HarmonyMethod(
					typeof(AtlyssItemStackMod).GetMethod(
						nameof(DropItemPrefix),
						BindingFlags.NonPublic | BindingFlags.Static
					)
				)
			);

			_harmony.PatchAll(); 
			StartCoroutine(GameplayLoop());
		}

		private static void ScriptableItemCtorPostfix(ScriptableItem __instance) {
			if (__instance == null) return;

			if (__instance._maxStackAmount != 999) {
				if (!_originalMaxStack.ContainsKey(__instance._itemName)) {
					_originalMaxStack.Add(__instance._itemName, __instance._maxStackAmount);
					AtlyssItemStackMod.Instance.Logger.LogDebug($"Added {__instance._itemName} to stack cache with value of {__instance._maxStackAmount}.");
				}
				__instance._maxStackAmount = 999;
			}
		}

		private static bool DropItemPrefix(ref int _quantity, ItemData _itemData) {
			var _itemName = _itemData._itemName;

			if (_quantity > _originalMaxStack[_itemName]) {
				_quantity = _originalMaxStack[_itemName];
			}

			return true;
		}

		private void SetAllStackSizes() {
			try {
				var items = Resources.FindObjectsOfTypeAll<ScriptableItem>();
				int changed = 0;

				for (int i = 0; i < items.Length; i++) {
					ScriptableItem item = items[i];
					if (item == null) continue;
					if (item._maxStackAmount != 999) {
						if (!_originalMaxStack.ContainsKey(item._itemName)) {
							_originalMaxStack.Add(item._itemName, item._maxStackAmount);
							Logger.LogDebug($"Added {item._itemName} to stack cache with value of {item._maxStackAmount}.");
						}
						item._maxStackAmount = 999;
						changed++;
					}
				}

				Logger.LogDebug($"Updated {changed} ScriptableItems to stack at 999.");
			} catch (System.Exception exc) {
				Logger.LogError($"Error updating stack sizes: {exc}");
			}
		}

		private IEnumerator GameplayLoop() {
			while (true) {
				yield return new WaitForSeconds(10f);
				SetAllStackSizes();
			}
		}

	}
}