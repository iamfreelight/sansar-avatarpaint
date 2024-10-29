/* 
 * AvatarPaint Cleansing Script 
 * by Freelight - 10/29/2024 
 *
 * Keeps track of each avatar's original materials properties and cleanses them when hitting the cleansing TriggerVolume
 *
 * Keeps track of everyone in a List when they hit whatever TriggerVolume you have slotted into rbTriggerSpawnpoint - the 
 * SpawnPoint trigger -- please put the over the scenes spawnpoint -- but personally I prefer to make this trigger the size
 * of the entire area the avatar can get dirty in or the entire scene
 *
 * Slot in another Trigger Volume somewhere in the scene that avatars can go to restore their original color tint/emissive
 * values, a way to 'rinse' them
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

public class FLS_PaintBucket_Cleanser_1a : SceneObjectScript
{
	[DefaultValue(false)]
	public bool DebugMode = false;

	[DisplayName("Spawnpoint Trigger")]
	public RigidBodyComponent rbTriggerSpawnpoint = null;

	[DisplayName("Cleansing Area Trigger")]
	public RigidBodyComponent rbTriggerCleanserArea = null;

	[Tooltip("The delay (in seconds), the duration of how long the 'cleansing' fades in, or rather the 'paint' fading away.. when triggering cleansing for themselves")]
	[DisplayName("Cleaning Speed")]
	[DefaultValue(5.0f)]
	public float cleanSpeed = 5.0f;	

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

	public class AvatarMatsData {
		public Sansar.Color[] origMatColors = new Sansar.Color[256];
		public float[] origMatEmiss = new float[256];
	}

	private List<Tuple<ObjectId, AvatarMatsData>> AMD = new List<Tuple<ObjectId, AvatarMatsData>>();

	public override void Init() {
		rbTriggerSpawnpoint.Subscribe(CollisionEventType.Trigger, OnTriggerSpawnpoint);
		rbTriggerCleanserArea.Subscribe(CollisionEventType.Trigger, OnTriggerClean);		
	}

	public void AddEntry(ObjectId objectId, AvatarMatsData avatarMatsData)
	{
		// Check if the ObjectId already exists in the list
		bool exists = AMD.Any(tuple => tuple.Item1.Equals(objectId));

		if (!exists)
		{
			// Only add if the ObjectId doesn't already exist
			AMD.Add(Tuple.Create(objectId, avatarMatsData));
			if (DebugMode == true) Log.Write("Entry added: " + objectId.ToString());
		}
		else
		{
			if (DebugMode == true) Log.Write("Entry with this ObjectId already exists.");
		}
	}

	public AvatarMatsData GetAvatarMatsData(ObjectId objectId)
	{
		// Search for the first tuple with a matching ObjectId
		var foundTuple = AMD.Find(tuple => tuple.Item1.Equals(objectId));

		if (foundTuple != null)
		{
			// Return the AvatarMatsData if a match is found
			return foundTuple.Item2;
		}

		// Return null or handle the case if no match is found
		if (DebugMode == true) Log.Write("No AvatarMatsData found for the specified ObjectId.");
		return null;
	}

	private void OnTriggerSpawnpoint(CollisionData data)
	{
		ObjectId agentObjId;
		try {
			AgentPrivate agent = ScenePrivate.FindAgent(data.HitComponentId.ObjectId);
			if (data.Phase == CollisionEventPhase.TriggerEnter)
			{
				SimpleData sd = new SimpleData(this);
				sd.ObjectId = data.HitComponentId.ObjectId;
				sd.AgentInfo = ScenePrivate.FindAgent(sd.ObjectId)?.AgentInfo;
				sd.SourceObjectId = ObjectPrivate.ObjectId;
				agentObjId = sd.AgentInfo.ObjectId;

				try {
					MeshComponent mc = null;
					ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);

					if (mc != null) {
						AvatarMatsData tmpDataToAdd = new AvatarMatsData();
						List<RenderMaterial> materials = mc.GetRenderMaterials().ToList();
						for (int j = 0; j < materials.Count; j++)
						{
							MaterialProperties p = materials[j].GetProperties();
							tmpDataToAdd.origMatColors[j] = p.Tint;
							tmpDataToAdd.origMatEmiss[j] = p.EmissiveIntensity;
						}

						AddEntry(sd.AgentInfo.ObjectId, tmpDataToAdd);
					}
				} catch {
					if (DebugMode == true) Log.Write("Unable to add user to registry... ");
				}
			}
		} catch {
			if (DebugMode == true) Log.Write("OnTriggerSpawnpoint() - Exception");
		}
	}

	private void OnTriggerClean(CollisionData data)
	{
		ObjectId agentObjId;
		try {
			AgentPrivate agent = ScenePrivate.FindAgent(data.HitComponentId.ObjectId);
			if (data.Phase == CollisionEventPhase.TriggerEnter)
			{
				SimpleData sd = new SimpleData(this);
				sd.ObjectId = data.HitComponentId.ObjectId;
				sd.AgentInfo = ScenePrivate.FindAgent(sd.ObjectId)?.AgentInfo;
				sd.SourceObjectId = ObjectPrivate.ObjectId;
				agentObjId = sd.AgentInfo.ObjectId;

				AvatarMatsData avatarMatsData = GetAvatarMatsData(sd.AgentInfo.ObjectId);

				try {
					if (avatarMatsData != null) {
						MeshComponent mc = null;
						ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);					
						mc.SetIsVisible(true);
      
						if (mc != null) {
							List<RenderMaterial> materials2 = mc.GetRenderMaterials().ToList();
							for (int j = 0; j < materials2.Count; j++)
							{
								MaterialProperties p = materials2[j].GetProperties();
								p.Tint = avatarMatsData.origMatColors[j];
								p.EmissiveIntensity = avatarMatsData.origMatEmiss[j];
								materials2[j].SetProperties(p, cleanSpeed, InterpolationModeParse("linear"));
								if (DebugMode == true) agent.SendChat("Returning material back to original states for emiss/color: #" + j.ToString().Trim() + " :: "  + materials2[j].Name);
							}
						}
					}
				} catch {
					if (DebugMode == true) Log.Write("OnTriggerClean() - Exception 2 ");
				}
			} else {
			}
		} catch {
			if (DebugMode == true) Log.Write("OnTriggerClean() - Exception 1");
		}		
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
}
