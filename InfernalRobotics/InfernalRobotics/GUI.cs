﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MuMech;

namespace MuMech
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
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

			public Group()
			{
				this.name = "";
				ForwardKey = "";
				ReverseKey = "";
				servos = new List<MuMechToggle>();
			}
		}

		protected static Rect controlWinPos;
		protected static Rect editorWinPos;
		protected static bool resetWin;
		protected static Vector2 editorScroll;
		List<Group> servo_groups;
		protected static MuMechGUI gui_controller;

		public static MuMechGUI gui
		{
			get { return gui_controller; }
		}

		static void move_servo(Group from, Group to, MuMechToggle servo)
		{
			to.servos.Add(servo);
			from.servos.Remove(servo);
			servo.GroupName = to.name;
			servo.ForwardKey = to.ForwardKey;
			servo.ReverseKey = to.ReverseKey;
		}

		public static void add_servo(MuMechToggle servo)
		{
			if (!gui)
				return;
			gui.enabled = true;
			if (servo.part.customPartData != null
				&& servo.part.customPartData != "") {
				servo.ParseCData();
			}
			if (gui.servo_groups == null)
				gui.servo_groups = new List<Group>();
			Group group = null;
			if (servo.GroupName != null && servo.GroupName != "") {
				for (int i = 0; i < gui.servo_groups.Count; i++) {
					if (servo.GroupName == gui.servo_groups[i].name) {
						group = gui.servo_groups[i];
						break;
					}
				}
				if (group == null) {
					gui.servo_groups.Add(new Group(servo));
					return;
				}
			}
			if (group == null) {
				if (gui.servo_groups.Count < 1) {
					gui.servo_groups.Add(new Group());
				}
				group = gui.servo_groups[gui.servo_groups.Count - 1];
			}

			group.servos.Add(servo);
			servo.GroupName = group.name;
			servo.ForwardKey = group.ForwardKey;
			servo.ReverseKey = group.ReverseKey;
		}

		public static void remove_servo(MuMechToggle servo)
		{
			if (!gui)
				return;
			if (gui.servo_groups == null)
				return;
			int num = 0;
			foreach (var group in gui.servo_groups) {
				if (group.name == servo.GroupName) {
					group.servos.Remove(servo);
				}
				num += group.servos.Count;
			}
			gui.enabled = num > 0;
		}


		void onVesselChange(Vessel v)
		{
			Debug.Log(String.Format("[IR GUI] vessel {0}", v.name));

			servo_groups = null;
			enabled = false;

			var groups = new List<Group>();
			var group_map = new Dictionary<string, int>();

			foreach (Part p in v.Parts) {
				foreach (var servo in p.Modules.OfType<MuMechToggle>()) {
					if (servo.part.customPartData != null
						&& servo.part.customPartData != "") {
						servo.ParseCData();
					}
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

		void onPartAttach(GameEvents.HostTargetAction<Part,Part> host_target)
		{
			Part p = host_target.host;
			foreach (var servo in p.Modules.OfType<MuMechToggle>()) {
				add_servo(servo);
			}
		}

		void onPartRemove(GameEvents.HostTargetAction<Part,Part> host_target)
		{
			Part p = host_target.target;
			foreach (var servo in p.Modules.OfType<MuMechToggle>()) {
				remove_servo(servo);
			}
		}

		void Awake()
		{
			Debug.Log("[IR GUI] awake");
			enabled = false;
			var scene = HighLogic.LoadedScene;
			if (scene == GameScenes.FLIGHT) {
				GameEvents.onVesselChange.Add(onVesselChange);
				gui_controller = this;
			} else if (scene == GameScenes.EDITOR) {
				GameEvents.onPartAttach.Add(onPartAttach);
				GameEvents.onPartRemove.Add(onPartRemove);
				gui_controller = this;
			} else {
				gui_controller = null;
			}
		}
		void OnDestroy()
		{
			Debug.Log("[IR GUI] destroy");
			GameEvents.onVesselChange.Remove(onVesselChange);
			GameEvents.onPartAttach.Remove(onPartAttach);
			GameEvents.onPartRemove.Remove(onPartRemove);
		}

		private void ControlWindow(int windowID)
		{
			GUILayout.BeginVertical();

			foreach (Group g in servo_groups) {
				GUILayout.BeginHorizontal();

				GUILayout.Label(g.name, GUILayout.ExpandWidth(true));
				int forceFlags = 0;
				var width = GUILayout.Width(20);
				forceFlags |= GUILayout.RepeatButton("<", width) ? 1 : 0;
				forceFlags |= GUILayout.RepeatButton("O", width) ? 4 : 0;
				forceFlags |= GUILayout.RepeatButton(">", width) ? 2 : 0;
				foreach (MuMechToggle servo in g.servos) {
					servo.moveFlags &= ~7;
					servo.moveFlags |= forceFlags;
				}

				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();

			GUI.DragWindow();
		}

		void EditorWindow(int windowID)
		{
			var expand = GUILayout.ExpandWidth(true);
			var width20 = GUILayout.Width(20);
			var width40 = GUILayout.Width(40);
			var width60 = GUILayout.Width(60);
			var maxHeight = GUILayout.MaxHeight(Screen.height / 2);

			Vector2 mousePos = Input.mousePosition;
			mousePos.y = Screen.height - mousePos.y;

			editorScroll = GUILayout.BeginScrollView(editorScroll, false,
													 false, maxHeight);

			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Group Name", expand);
			GUILayout.Label("Keys", width40);
			if (servo_groups.Count > 1) {
				GUILayout.Space(60);
			}
			GUILayout.EndHorizontal();

			for (int i = 0; i < servo_groups.Count; i++) {
				Group grp = servo_groups[i];

				GUILayout.BeginHorizontal();
				string tmp = GUILayout.TextField(grp.name, expand);
				if (grp.name != tmp) {
					grp.name = tmp;
				}
				tmp = GUILayout.TextField(grp.ForwardKey, width20);
				if (grp.ForwardKey != tmp) {
					grp.ForwardKey = tmp;
				}
				tmp = GUILayout.TextField(grp.ReverseKey, width20);
				if (grp.ReverseKey != tmp) {
					grp.ReverseKey = tmp;
				}
				if (i > 0) {
					if (GUILayout.Button("Remove", width60)) {
						foreach (var servo in grp.servos) {
							move_servo(grp, servo_groups[i - 1], servo);
						}
						servo_groups.RemoveAt(i);
						resetWin = true;
						return;
					}
				} else {
					if (servo_groups.Count > 1) {
						GUILayout.Space(60);
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();

				GUILayout.Space(20);

				GUILayout.BeginVertical();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Servo Name", expand);
				GUILayout.Label("Rotate", width40);
				if (servo_groups.Count > 1) {
					GUILayout.Label("Group", width40);
				}
				GUILayout.EndHorizontal();

				foreach (var servo in grp.servos) {
					GUILayout.BeginHorizontal();
					servo.ServoName = GUILayout.TextField(servo.ServoName,
														  expand);
					if (editorWinPos.Contains(mousePos)) {
						var last = GUILayoutUtility.GetLastRect();
						var pos = Event.current.mousePosition;
						bool highlight = last.Contains(pos);
						servo.part.SetHighlight(highlight);
					}
					if (GUILayout.Button("<", width20)) {
						servo.transform.RotateAround(servo.transform.up,
													 Mathf.PI / 4);
					}
					if (GUILayout.Button(">", width20)) {
						servo.transform.RotateAround(servo.transform.up,
													 -Mathf.PI / 4);
					}
					if (servo_groups.Count > 1) {
						if (i > 0) {
							if (GUILayout.Button("/\\", width20)) {
								move_servo(grp, servo_groups[i - 1], servo);
							}
						} else {
							GUILayout.Space(20);
						}
						if (i < (servo_groups.Count - 1)) {
							if (GUILayout.Button("\\/", width20)) {
								move_servo(grp, servo_groups[i + 1], servo);
							}
						} else {
							GUILayout.Space(20);
						}
					}
					GUILayout.EndHorizontal();
				}

				GUILayout.EndVertical();

				GUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add new Group")) {
				servo_groups.Add(new Group());
			}

			GUILayout.EndVertical();

			GUILayout.EndScrollView();

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
            if (controlWinPos.x == 0 && controlWinPos.y == 0) {
                controlWinPos = new Rect(Screen.width / 2, Screen.height / 2,
										 10, 10);
            }
			if (editorWinPos.x == 0 && editorWinPos.y == 0) {
				editorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
			}
			if (resetWin) {
				editorWinPos = new Rect(editorWinPos.x, editorWinPos.y,
										10, 10);
				resetWin = false;
			}
            GUI.skin = MuUtils.DefaultSkin;
			var scene = HighLogic.LoadedScene;
			if (scene == GameScenes.FLIGHT) {
				controlWinPos = GUILayout.Window(956, controlWinPos,
												 ControlWindow,
												 "Servo Control",
												 GUILayout.MinWidth(150));
			} else if (scene == GameScenes.EDITOR) {
				var height = GUILayout.Height(Screen.height / 2);
				editorWinPos = GUILayout.Window(957, editorWinPos,
												EditorWindow,
												"Servo Configuration",
												GUILayout.Width(250),
												height);
			}
		}
	}
}
