using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MuMech;

namespace MuMech
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class MuMechGUI : MonoBehaviour
	{
		public class Group
		{
			public string name;
			public List<MuMechToggle> servos;
			public string ForwardKey;
			public string ReverseKey;

			public Group(MuMechToggle servo)
			{
				this.name = servo.GroupName;
				ForwardKey = servo.ForwardKey;
				ReverseKey = servo.ReverseKey;
				servos = new List<MuMechToggle>();
				servos.Add(servo);
			}
		}

		protected static Rect winPos;
		List<Group> servo_groups;

		void onVesselChange(Vessel v)
		{
			Debug.Log(String.Format("[IR GUI] vessel {0}", v.name));

			servo_groups = null;
			enabled = false;

			var groups = new List<Group>();
			var group_map = new Dictionary<string, int>();

			foreach (Part p in v.Parts) {
				foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>()) {
					if (!group_map.ContainsKey(servo.GroupName)) {
						groups.Add(new Group(servo));
						group_map[servo.GroupName] = groups.Count - 1;
					} else {
						Group g = groups[group_map[servo.GroupName]];
						g.servos.Add(servo);
					}
				}
			}
			Debug.Log(String.Format("[IR GUI] {0} groups", groups.Count));
			if (groups.Count > 0) {
				servo_groups = groups;
				enabled = true;
			}
		}
		void Awake()
		{
			Debug.Log("[IR GUI] awake");
			enabled = false;
			GameEvents.onVesselChange.Add(onVesselChange);
		}
		void OnDestroy()
		{
			Debug.Log("[IR GUI] destroy");
			GameEvents.onVesselChange.Remove(onVesselChange);
		}

		private void ControlWindow(int windowID)
		{
			GUILayout.BeginVertical();

			foreach (Group g in servo_groups) {
				GUILayout.BeginHorizontal();

				GUILayout.Label(g.name, GUILayout.ExpandWidth(true));
				int forceFlags = 0;
				forceFlags |= (GUILayout.RepeatButton("<", GUILayout.Width(20))?1:0);
				forceFlags |= (GUILayout.RepeatButton("O", GUILayout.Width(20))?4:0);
				forceFlags |= (GUILayout.RepeatButton(">", GUILayout.Width(20))?2:0);
				foreach (MuMechToggle servo in g.servos) {
					servo.moveFlags &= ~7;
					servo.moveFlags |= forceFlags;
				}

				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();

			GUI.DragWindow();
		}

		void OnGUI()
		{
			// This particular test isn't needed due to the GUI being enabled
			// and disabled as appropriate, but it saves potential NREs.
			if (servo_groups == null)
				return;
			if (InputLockManager.IsLocked(ControlTypes.LINEAR))
				return;
            if (winPos.x == 0 && winPos.y == 0) {
                winPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
            }
            GUI.skin = MuUtils.DefaultSkin;
            winPos = GUILayout.Window(956, winPos, ControlWindow, "Servo Control",
									  GUILayout.MinWidth(150));
		}
	}
}
