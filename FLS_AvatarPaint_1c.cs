/* 
 * Avatar Painting Script 
 * by Freelight - 11/30/2024 
 *
 * Makes avatar's materials tint to the specified colors when touching the TriggerVolume associated with each from the two lists;  The Cleansing Area Trigger will restore avatar materials to their original state from when they joined the scene/instance;
 * When entering the RGBTrigger -- the avatar's materials will each be tinted random colors :)
 *
 * Ver 1c - 'All-in-one' version of the scripts for AvatarPaint
 * https://github.com/iamfreelight/sansar-avatarpaint
 *
 */

using Sansar;
using Sansar.Script;
using Sansar.Simulation;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class FLS_AvatarPaint_1c : SceneObjectScript
{
	[DefaultValue(false)]
	public bool DebugMode = false;

	[DisplayName("Emissive Level")]
	[Tooltip("Effect's Emissive Level when applied")]
	[DefaultValue(32f)]
	public float EffectEmissLevel = 32.0f;

	[DisplayName("Paint Avatar Triggers")]
	public List<RigidBodyComponent> PaintTriggers = null;

	[DisplayName("Paint Avatar Colors")]
	[Tooltip("Each Trigger (defined in list above this one) -- each effect's color when applied")]
	[DefaultValue(1,0,0,1)]
	public List<Sansar.Color> PaintColors = null;

	[DisplayName("Random/Rainbow Colorize Trigger")]
	public RigidBodyComponent RgbTrigger = null;
	
	[DisplayName("Cleansing Area Trigger")]
	public RigidBodyComponent CleanserAreaTrigger = null;

	[Tooltip("The delay (in seconds), the duration of how long the 'cleansing' fades in, or rather the 'paint' fading away.. when triggering cleansing for themselves")]
	[DisplayName("Cleaning Speed")]
	[DefaultValue(5.0f)]
	public float CleanSpeed = 0.0001f;	

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
	
	public class AvatarMatsData {
		public Sansar.Color[] origMatColors = new Sansar.Color[2048];
		public float[] origMatEmiss = new float[2048];
	}
	private List<Tuple<ObjectId, AvatarMatsData>> AMD = new List<Tuple<ObjectId, AvatarMatsData>>();
	

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
	
	public override void Init() {
	        if (PaintTriggers == null || PaintTriggers.Count == 0)
	        {
	            Log.Write("No triggers defined, script disabled");
	            return;
	        }	
		
		if (PaintTriggers == null || PaintTriggers.Count == 0)
		{
	            Log.Write("Number of entries for painting triggers vs # of entries of colors do not match in length, script disabled.");
	            return;			
		}
	
	        for (int i = 0; i < PaintTriggers.Count; i++)
	        {
	            // Capture index for use in the event handler
	            int index = i;
	
	            // Check if the trigger is valid
	            if (PaintTriggers[index] != null)
	            {
	                PaintTriggers[index].Subscribe(CollisionEventType.Trigger, (CollisionData data) => OnTrigger(data, index));
	            }
	            else
	            {
	                Log.Write($"Trigger at index {index} is null.");
	            }
	        }
		
		if (CleanserAreaTrigger != null) CleanserAreaTrigger.Subscribe(CollisionEventType.Trigger, OnTriggerClean);
		if (RgbTrigger != null) RgbTrigger.Subscribe(CollisionEventType.Trigger, OnTriggerRGB);
		ScenePrivate.User.Subscribe(User.AddUser, OnUserJoin);		
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
			p.EmissiveIntensity = EffectEmissLevel;
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
			p.EmissiveIntensity = EffectEmissLevel;
			materials[j].SetProperties(p, 0.0001f, InterpolationModeParse("linear"));
		}
	}

	private void OnTrigger(CollisionData data, int rbnum)
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
				
				if (agentObjId != null)
    				{
					MeshComponent mc = null;
					ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);
					bool isVisible = mc.GetIsVisible();
					if (isVisible == true) {
						DoColorize(mc, PaintColors[rbnum].R, PaintColors[rbnum].G, PaintColors[rbnum].B, PaintColors[rbnum].A);
						if (DebugMode == true) agent.SendChat("Success performing material alterations on player '"+sd.AgentInfo.Handle.ToString().Trim()+"'");
					}
				}
			} catch {
				if (DebugMode == true) Log.Write("OnTrigger() - Exception");
			}
		}	
	}
	
	private void OnTriggerRGB(CollisionData data)
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
					MeshComponent mc = null;
					ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);
					bool isVisible = mc.GetIsVisible();
					if (isVisible == true) {
						DoRGB(mc);
						if (DebugMode == true) agent.SendChat("Success performing material alterations on player '"+sd.AgentInfo.Handle.ToString().Trim()+"'");
					}
				}
			} catch {
				if (DebugMode == true) Log.Write("OnTrigger() - Exception");
			}
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
						
						if (mc != null) {
							mc.SetIsVisible(true);						
							List<RenderMaterial> materials2 = mc.GetRenderMaterials().ToList();
							for (int j = 0; j < materials2.Count; j++)
							{
								MaterialProperties p = materials2[j].GetProperties();
								p.Tint = avatarMatsData.origMatColors[j];
								p.EmissiveIntensity = avatarMatsData.origMatEmiss[j];
								materials2[j].SetProperties(p, CleanSpeed, InterpolationModeParse("linear"));
								if (DebugMode == true) agent.SendChat("Returning material back to original states for emiss/color: #" + j.ToString().Trim() + " :: "  + materials2[j].Name);
							}
						}
					}
				} catch {
					if (DebugMode == true) Log.Write("OnTriggerClean() - Exception 1 ");
				}
			} else {
			}
		} catch {
			if (DebugMode == true) Log.Write("OnTriggerClean() L1 Exception 2");
		}		
	}
	
	private void OnUserJoin(UserData data)
	{
		try {
			//save avatars original materials via OnUserJoin, so they can be restored when going inside the CleansingTrigger
			AgentPrivate agent = ScenePrivate.FindAgent(data.User);
			ObjectId agentObjId = agent.AgentInfo.ObjectId;
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

				AddEntry(agent.AgentInfo.ObjectId, tmpDataToAdd);
			}
		} catch (Exception ex) {
			if (DebugMode == true) Log.Write("OnUserJoin() - Exception: " + ex.Message.ToString());
		}
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
}
