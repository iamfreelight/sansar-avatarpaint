/* 
 * Avatar Painting Script 
 * by Freelight - 10/29/2024 
 *
 * Makes avatar's turn the specified color when touching the TriggerVolume you have slotted into the 'Paint Avatar Trigger' aka 'rbTrigger' field;  Use FLS_PaintBucket_Cleanser_1a with another TriggerVolume to restore avatar materials to their original state
 *
 * https://github.com/iamfreelight/sansar-avatarpaint
 *
 */

using Sansar;
using Sansar.Script;
using Sansar.Simulation;
using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class FLS_PaintBucket_Paint_1a : SceneObjectScript
{
	[DefaultValue(false)]
	public bool DebugMode = false;

	[DisplayName("Randomize All Materials")]
	[Tooltip("When set to True the script will randomize the Tint and increase emissiveness on each of the avatar's materials seperately, via OnTrigger; When set to false, ColorizeColor will be used on all of the avatar's materials when activated via OnTrigger")]
	[DefaultValue(false)]
	public bool rgbMode = false;

	[DisplayName("Paint Avatar Trigger")]
	public RigidBodyComponent rbTrigger = null;

	[DisplayName("Emissive Level")]
	[Tooltip("Effect's Emissive Level when applied")]
	[DefaultValue(32f)]
	public float effectEmissLevel = 32.0f;

	[DisplayName("Colorize Color")]
	[Tooltip("Effect's Color when applied ")]
	[DefaultValue(1,0,0,1)]
	public Sansar.Color colorizeColor;

	public interface ISimpleData
	{
		AgentInfo AgentInfo { get; }
		ObjectId ObjectId { get; }
		ObjectId SourceObjectId { get; }
		Reflective ExtraData { get; }
	}

	public class SimpleData : Reflective, ISimpleData
	{
		public SimpleData(ScriptBase script) { ExtraData = script; }
		public AgentInfo AgentInfo { get; set; }
		public ObjectId ObjectId { get; set; }
		public ObjectId SourceObjectId { get; set; }
		public Reflective ExtraData { get; }
	}

	private static readonly Random rnd = new Random();
	private float GetRandomFloat()
	{
		return (float)rnd.NextDouble();
	}

	InterpolationMode InterpolationModeParse(string s)
	{
		s = s.ToLower();
		if (s == "easein") return InterpolationMode.EaseIn;
		if (s == "easeout") return InterpolationMode.EaseOut;
		if (s == "linear") return InterpolationMode.Linear;
		if (s == "smoothstep") return InterpolationMode.Smoothstep;
		if (s == "step") return InterpolationMode.Step;
		if (DebugMode == true) Log.Write(LogLevel.Warning, $"Unknown InterpolationMode '{s}'!  Using Linear...");
		return InterpolationMode.Linear;
	}

	private void OnTrigger(CollisionData data)
	{
		AgentPrivate agent = ScenePrivate.FindAgent(data.HitComponentId.ObjectId);
		ObjectId agentObjId;

		if (data.Phase == CollisionEventPhase.TriggerExit)
		{
		}
		else if (data.Phase == CollisionEventPhase.TriggerEnter)
		{
			try {
				SimpleData sd = new SimpleData(this);
				sd.ObjectId = data.HitComponentId.ObjectId;
				sd.AgentInfo = ScenePrivate.FindAgent(sd.ObjectId)?.AgentInfo;
				sd.SourceObjectId = ObjectPrivate.ObjectId;
				agentObjId = sd.AgentInfo.ObjectId;
				
				if (agentObjId != null) {
					if (rgbMode == true) {
						MeshComponent mc = null;
						ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);
						bool isVisible = mc.GetIsVisible();
						if (isVisible == true) {
							DoRGB(mc);
							if (DebugMode == true) agent.SendChat("Success performing material alterations on player '"+sd.AgentInfo.Handle.ToString().Trim()+"'");
						}
					} else {
						MeshComponent mc = null;
						ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);
						bool isVisible = mc.GetIsVisible();
						if (isVisible == true) {
							DoColorize(mc, colorizeColor.R, colorizeColor.G, colorizeColor.B, colorizeColor.A);
							if (DebugMode == true) agent.SendChat("Success performing material alterations on player '"+sd.AgentInfo.Handle.ToString().Trim()+"'");
						}
					}
				}
			} catch {
				if (DebugMode == true) Log.Write("OnTrigger() - Exception");
			}
		}
	}

	private void DoRGB(MeshComponent mc) {
		//Randomize avatar's material tint colors, each one differently
		List<RenderMaterial> materials3 = mc.GetRenderMaterials().ToList();
		for (int j = 0; j < materials3.Count; j++)
		{
			MaterialProperties p = materials3[j].GetProperties();
			float r1 = GetRandomFloat();
			float g1 = GetRandomFloat();
			float b1 = GetRandomFloat();
			Sansar.Color newcolor = new Sansar.Color(r1,g1,b1,1.0f);
			p.Tint = newcolor;
			p.EmissiveIntensity = effectEmissLevel;
			materials3[j].SetProperties(p, 3.0f, InterpolationModeParse("linear"));
		}
	}

	private void DoColorize(MeshComponent mc, float r, float g, float b, float a) {
		//Set avatar's material colors to whatever the user has set in the inspector on this paint bucket trigger's script
		List<RenderMaterial> materials = mc.GetRenderMaterials().ToList();
		for (int j = 0; j < materials.Count; j++)
		{
			MaterialProperties p = materials[j].GetProperties();
			Sansar.Color newcolor = new Sansar.Color(r,g,b,a);
			p.Tint = newcolor;
			p.EmissiveIntensity = effectEmissLevel;
			materials[j].SetProperties(p, 0.0001f, InterpolationModeParse("linear"));
		}
	}

	public override void Init() {
		rbTrigger.Subscribe(CollisionEventType.Trigger, OnTrigger);
	}
}
