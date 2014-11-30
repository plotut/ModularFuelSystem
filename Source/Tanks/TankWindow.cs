using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections.ObjectModel;
using KSPAPIExtensions;
using KSPAPIExtensions.PartMessage;

// ReSharper disable InconsistentNaming, CompareOfFloatsByEqualityOperator

namespace RealFuels.Tanks
{
	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
    public class TankWindow : MonoBehaviour
    {
        [KSPField]
        public int offsetGUIPos = -1;

        [KSPField (isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Show Tank"),
         UI_Toggle (enabledText = "Tank GUI", disabledText = "GUI")]
        [NonSerialized]
        public bool showRFGUI;

        private static GUIStyle unchanged;
        private static GUIStyle changed;
        private static GUIStyle greyed;
        private static GUIStyle overfull;
        public static string myToolTip = "";

		static TankWindow instance;

        private int counterTT;
        private Vector2 scrollPos;

		bool ActionGroupMode;
		ModuleFuelTanks tank_module;

		public static void HideGUI ()
		{
			if (instance != null) {
				instance.tank_module = null;
				instance.UpdateGUIState ();
			}
		}

		public static void ShowGUI (ModuleFuelTanks tank_module)
		{
			if (instance != null) {
				instance.tank_module = tank_module;
				instance.UpdateGUIState ();
			}
		}

        private void OnPartActionGuiCreate (Part p)
        {
            tank_module = null;
			ActionGroupMode = true;
			UpdateGUIState ();
        }

        private void OnPartActionGuiDismiss (Part p)
        {
            tank_module = null;
			ActionGroupMode = false;
			UpdateGUIState ();
        }

		void UpdateGUIState ()
		{
			enabled = tank_module != null;
		}

		void Awake ()
		{
			enabled = false;
			if (!CompatibilityChecker.IsAllCompatible ()) {
				return;
			}
			instance = this;
			GameEvents.onPartActionUICreate.Add (OnPartActionGuiCreate);
			GameEvents.onPartActionUIDismiss.Add (OnPartActionGuiDismiss);
		}

		void OnDestroy ()
		{
			instance = null;
			GameEvents.onPartActionUICreate.Remove (OnPartActionGuiCreate);
			GameEvents.onPartActionUIDismiss.Remove (OnPartActionGuiDismiss);
		}

        private Rect guiWindowRect = new Rect (0, 0, 0, 0);
        private static Vector3 mousePos = Vector3.zero;
        public void OnGUI ()
        {
            EditorLogic editor = EditorLogic.fetch;
            if (!HighLogic.LoadedSceneIsEditor || !editor) {
                return;
            }

            //UpdateMixtures ();
            bool cursorInGUI = false; // nicked the locking code from Ferram
            mousePos = Input.mousePosition; //Mouse location; based on Kerbal Engineer Redux code
            mousePos.y = Screen.height - mousePos.y;

            Rect tooltipRect;
            int posMult = 0;
            if (offsetGUIPos != -1) {
                posMult = offsetGUIPos;
			}
            if (ActionGroupMode) {
                guiWindowRect = new Rect (430 * posMult, 365, 438, (Screen.height - 365));
                tooltipRect = new Rect (430 * (posMult+1)+10, mousePos.y-5, 300, 20);
            } else {
                if (guiWindowRect.width == 0) {
                    guiWindowRect = new Rect (Screen.width - 8 - 430 * (posMult+1), 365, 438, (Screen.height - 365));
				}
                tooltipRect = new Rect (guiWindowRect.xMin - (230-8), mousePos.y - 5, 220, 20);
            }
			cursorInGUI = guiWindowRect.Contains (mousePos);
			if (cursorInGUI) {
				editor.Lock (false, false, false, "MFTGUILock");
				EditorTooltip.Instance.HideToolTip ();
			} else if (!cursorInGUI) {
				editor.Unlock ("MFTGUILock");
			}

            GUI.Label (tooltipRect, myToolTip);
            guiWindowRect = GUILayout.Window (GetInstanceID (), guiWindowRect, GUIWindow, "Fuel Tanks for " + tank_module.part.partInfo.title);
        }

		void DisplayMass ()
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Mass: " + tank_module.massDisplay + ", Cst " + tank_module.GetModuleCost ().ToString ("N1"));
			GUILayout.EndHorizontal ();
		}

		bool CheckTankList ()
		{
			if (tank_module.tankList.Count == 0) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("This fuel tank cannot hold resources.");
				GUILayout.EndHorizontal ();
				return false;
			}
			return true;
		}

        public void GUIWindow (int windowID)
        {
			InitializeStyles ();

			GUILayout.BeginVertical ();

			if (CheckTankList ()) {
				GUILayout.BeginHorizontal ();
				if (Math.Round (tank_module.AvailableVolume, 4) < 0) {
					GUILayout.Label ("Volume: " + tank_module.volumeDisplay, overfull);
				} else {
					GUILayout.Label ("Volume: " + tank_module.volumeDisplay);
				}
				GUILayout.EndHorizontal ();

				scrollPos = GUILayout.BeginScrollView (scrollPos);

				GUIEngines ();

				GUITanks ();

				GUILayout.EndScrollView ();
				GUILayout.Label (MFSSettings.GetVersion ());
			}
			GUILayout.EndVertical ();

			if (!(myToolTip.Equals ("")) && GUI.tooltip.Equals ("")) {
				if (counterTT > 4) {
					myToolTip = GUI.tooltip;
					counterTT = 0;
				} else {
					counterTT++;
				}
			} else {
				myToolTip = GUI.tooltip;
				counterTT = 0;
			}
			//print ("GT: " + GUI.tooltip);
			if (!ActionGroupMode) {
				GUI.DragWindow ();
			}
        }

        private static void InitializeStyles ()
        {
            if (unchanged == null) {
                if (GUI.skin == null) {
                    unchanged = new GUIStyle ();
                    changed = new GUIStyle ();
                    greyed = new GUIStyle ();
                    overfull = new GUIStyle ();
                } else {
                    unchanged = new GUIStyle (GUI.skin.textField);
                    changed = new GUIStyle (GUI.skin.textField);
                    greyed = new GUIStyle (GUI.skin.textField);
                    overfull = new GUIStyle (GUI.skin.label);
                }

                unchanged.normal.textColor = Color.white;
                unchanged.active.textColor = Color.white;
                unchanged.focused.textColor = Color.white;
                unchanged.hover.textColor = Color.white;

                changed.normal.textColor = Color.yellow;
                changed.active.textColor = Color.yellow;
                changed.focused.textColor = Color.yellow;
                changed.hover.textColor = Color.yellow;

                greyed.normal.textColor = Color.gray;

                overfull.normal.textColor = Color.red;
            }
        }

		void UpdateTank (FuelTank tank)
		{
			if (GUILayout.Button ("Update", GUILayout.Width (53))) {
				string trimmed = tank.maxAmountExpression.Trim ();

				if (trimmed == "") {
					tank.maxAmount = 0;
					//Debug.LogWarning ("[MFT] Removing tank as empty input " + tank.name + " amount: " + tank.maxAmountExpression ?? "null");
				} else {
					double tmp;
					if (MathUtils.TryParseExt (trimmed, out tmp)) {
						tank.maxAmount = tmp;

						if (tmp != 0) {
							tank.amount = tank.fillable ? tank.maxAmount : 0;

							// Need to round-trip the value
							tank.maxAmountExpression = tank.maxAmount.ToString ();
							//Debug.LogWarning ("[MFT] Updating maxAmount " + tank.name + " amount: " + tank.maxAmountExpression ?? "null");
						}
					}
				}
			}
		}

		void RemoveTank (FuelTank tank)
		{
			if (GUILayout.Button ("Remove", GUILayout.Width (58))) {
				tank.maxAmount = 0;
				GameEvents.onEditorShipModified.Fire (EditorLogic.fetch.ship);
				//Debug.LogWarning ("[MFT] Removing tank from button " + tank.name + " amount: " + tank.maxAmountExpression ?? "null");
			}
		}

		void EditTank (FuelTank tank)
		{
			GUILayout.Label (" ", GUILayout.Width (5));

			GUIStyle style = unchanged;
			if (tank.maxAmountExpression == null) {
				tank.maxAmountExpression = tank.maxAmount.ToString ();
				//Debug.LogWarning ("[MFT] Adding tank from API " + tank.name + " amount: " + tank.maxAmountExpression ?? "null");
			} else if (tank.maxAmountExpression.Length > 0 && tank.maxAmountExpression != tank.maxAmount.ToString ()) {
				style = changed;
			}

			tank.maxAmountExpression = GUILayout.TextField (tank.maxAmountExpression, style, GUILayout.Width (127));

			UpdateTank (tank);
			RemoveTank (tank);
		}

		void AddTank (FuelTank tank)
		{
			string extraData = "Max: " + (tank_module.AvailableVolume * tank.utilization).ToStringExt ("S3") + "L (+" + ModuleFuelTanks.FormatMass ((float) (tank_module.AvailableVolume * tank.mass)) + " )";

			GUILayout.Label (extraData, GUILayout.Width (150));

			if (GUILayout.Button ("Add", GUILayout.Width (120))) {
				tank.maxAmount = tank_module.AvailableVolume * tank.utilization;
				tank.amount = tank.fillable ? tank.maxAmount : 0;

				tank.maxAmountExpression = tank.maxAmount.ToString ();
				//Debug.LogWarning ("[MFT] Adding tank " + tank.name + " maxAmount: " + tank.maxAmountExpression ?? "null");
			}
		}

		void NoRoom ()
		{
			GUILayout.Label ("  No room for tank.", GUILayout.Width (150));
		}

		void TankLine (FuelTank tank)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (" " + tank, GUILayout.Width (115));

			// So our states here are:
			//   Not being edited currently (empty):   maxAmountExpression = null, maxAmount = 0
			//   Have updated the field, no user edit: maxAmountExpression == maxAmount.ToStringExt
			//   Other non UI updated maxAmount:       maxAmountExpression = null (set), maxAmount = non-zero
			//   User has updated the field:           maxAmountExpression != null, maxAmountExpression != maxAmount.ToStringExt

			if (tank_module.part.Resources.Contains (tank.name) && tank_module.part.Resources[tank.name].maxAmount > 0) {
				EditTank (tank);
			} else if (tank_module.AvailableVolume >= 0.001) {
				AddTank (tank);
			} else {
				NoRoom ();
			}
			GUILayout.EndHorizontal ();
		}

		void RemoveAllTanks ()
		{
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Remove All Tanks")) {
				tank_module.Empty ();
			}
			GUILayout.EndHorizontal ();
		}

        private void GUITanks ()
        {
			foreach (FuelTank tank in tank_module.tankList) {
				TankLine (tank);
			}

			RemoveAllTanks ();
        }

        private void GUIEngines ()
        {
/*			if (usedBy.Count > 0 && tank_module.AvailableVolume >= 0.001) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Configure remaining volume for " + engineCount + " engines:");
				GUILayout.EndHorizontal ();

				foreach (FuelInfo info in usedBy.Values)
				{
					GUILayout.BeginHorizontal ();
					if (GUILayout.Button (new GUIContent (info.Label, info.names))) {
						ConfigureFor (info);
					}
					GUILayout.EndHorizontal ();
				}
			}*/
        }
    }
}
