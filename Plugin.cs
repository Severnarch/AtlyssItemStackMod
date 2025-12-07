using System.Collections;
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

		private void Awake() {

			Instance = this;
			_harmony = new Harmony("io.github.severnarch.atlyssitemstackmod");

			var ScriptableItemCtor = AccessTools.Constructor(typeof(ScriptableItem));
			_harmony.Patch(ScriptableItemCtor,
				postfix: new HarmonyMethod(
					typeof(AtlyssItemStackMod).GetMethod(
						nameof(ScriptableItemCtorPostfix), 
						BindingFlags.NonPublic | BindingFlags.Static
					)
				)
			);

			_harmony.PatchAll(); 
			StartCoroutine(GameplayLoop());

		}

		private static void ScriptableItemCtorPostfix(ScriptableItem __instance) {

			if (__instance == null) return;

			__instance._maxStackAmount = 999;

		}

		private void SetAllStackSizes() {

			try {

				var items = Resources.FindObjectsOfTypeAll<ScriptableItem>();
				int changed = 0;

				for (int i = 0; i < items.Length; i++) {

					ScriptableItem item = items[i];
					if (item == null) continue;

					if (item._maxStackAmount != 999) {

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