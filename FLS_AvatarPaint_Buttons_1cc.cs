/* 
 * Avatar Painting Script - Buttons version
 * by Freelight - 12/03/2024 
 *
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

public class FLS_AvatarPaint_Buttons_1cc : SceneObjectScript
{
	[DefaultValue(false)]
	public bool DebugMode = false;

	[DisplayName("Emissive Level")]
	[Tooltip("Effect's Emissive Level when applied")]
	[DefaultValue(32f)]
	public float EffectEmissLevel = 32.0f;

	[DisplayName("Paint Buttons")]
	public List<RigidBodyComponent> PaintButtons = null;
	
	[DisplayName("Paint Button Prompts")]
	public List<string> PaintButtonsPrompts = null;

	[DisplayName("Paint Colors")]
	[Tooltip("Each Button's Color (defined in list above this one) -- each effect's color when applied")]
	[DefaultValue(1,0,0,1)]
	public List<Sansar.Color> PaintColors = null;

	[DisplayName("Random Colorize Button")]
	public RigidBodyComponent RgbButton = null;
	[DisplayName("Random Colorize Button Prompt")]
	public string RgbPrompt = "Randomize Avatar Material Colors";
	
	[DisplayName("Random Colorize Speed")]
	[DefaultValue(0.01f)]
	public float RandomizeColorSpeed = 0.01f;
	
	[DisplayName("Cleansing Button")]
	public RigidBodyComponent CleanserButton = null;	
	[DisplayName("Cleansing Button Prompt")]
	public string CleansePrompt = "Remove Avatar Paint";

	[Tooltip("The delay (in seconds), the duration of how long the 'cleansing' fades in, or rather the 'paint' fading away..")]
	[DisplayName("Cleaning Speed")]
	[DefaultValue(0.25f)]
	public float CleanSpeed = 0.25f;

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
        if (PaintButtons == null || PaintButtons.Count == 0)
        {
            Log.Write("No buttons slotted for interactions, script disabled");
            return;
        }	
		
		if ((PaintButtons.Count != PaintColors.Count) || (PaintColors.Count != PaintButtonsPrompts.Count))
		{
            Log.Write("Number of entries for painting button volumes, vs. # of entries of colors, vs. prompt strings, the lists do not match in length, script disabled.");
            return;			
		}
	
        for (int i = 0; i < PaintButtons.Count; i++)
        {
            // Capture index for use in the event handler
            int index = i;

            // Check if the volume is valid
            if (PaintButtons[index] != null)
            {
				if (PaintButtons[index] != null && PaintButtons[index].IsValid)
				{
					ObjectPrivate op = ScenePrivate.FindObject(PaintButtons[index].ComponentId.ObjectId);
					
					if (op != null)
					{
						var result = WaitFor(op.AddInteraction, PaintButtonsPrompts[index], true) as ObjectPrivate.AddInteractionData;
						if (result.Success)
						{
							result.Interaction.Subscribe((InteractionData data) =>
							{			
								SimpleData sd = new SimpleData(this);
								sd.SourceObjectId = ObjectPrivate.ObjectId;
								sd.AgentInfo = ScenePrivate.FindAgent(data.AgentId)?.AgentInfo;
								sd.ObjectId = sd.AgentInfo != null ? sd.AgentInfo.ObjectId : ObjectId.Invalid;
								OnColorClick(sd.AgentInfo.ObjectId, index);
							});
						}
					}
				}				
            }
            else
            {
                Log.Write($"Volume at index {index} is null.");
            }
        }
		
		if (CleanserButton != null && CleanserButton.IsValid)
		{
			ObjectPrivate op = ScenePrivate.FindObject(CleanserButton.ComponentId.ObjectId);
			
			if (op != null)
			{
				var result = WaitFor(op.AddInteraction, CleansePrompt , true) as ObjectPrivate.AddInteractionData;
				if (result.Success)
				{
					result.Interaction.Subscribe((InteractionData data) =>
					{			
						SimpleData sd = new SimpleData(this);
						sd.SourceObjectId = ObjectPrivate.ObjectId;
						sd.AgentInfo = ScenePrivate.FindAgent(data.AgentId)?.AgentInfo;
						sd.ObjectId = sd.AgentInfo != null ? sd.AgentInfo.ObjectId : ObjectId.Invalid;
						OnClean(sd.AgentInfo.ObjectId);
					});
				}
			}
		}
		
		if (RgbButton != null && RgbButton.IsValid)
		{
			ObjectPrivate op = ScenePrivate.FindObject(RgbButton.ComponentId.ObjectId);
			
			if (op != null)
			{
				var result = WaitFor(op.AddInteraction, RgbPrompt , true) as ObjectPrivate.AddInteractionData;
				if (result.Success)
				{
					result.Interaction.Subscribe((InteractionData data) =>
					{			
						SimpleData sd = new SimpleData(this);
						sd.SourceObjectId = ObjectPrivate.ObjectId;
						sd.AgentInfo = ScenePrivate.FindAgent(data.AgentId)?.AgentInfo;
						sd.ObjectId = sd.AgentInfo != null ? sd.AgentInfo.ObjectId : ObjectId.Invalid;
						OnRGB(sd.AgentInfo.ObjectId);
					});
				}
			}
		}
		
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
			materials3[j].SetProperties(p, RandomizeColorSpeed, InterpolationModeParse("linear"));
		}
	}

	private void DoColorize(MeshComponent mc, float r, float g, float b, float a) {
		//Set avatar's material colors to whatever the user has set in the inspector for this paint buttons volume
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

	private void OnColorClick(ObjectId agentObjId, int rbnum)
	{
		try {
			if (agentObjId != null) {
				MeshComponent mc = null;
				ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);
				bool isVisible = mc.GetIsVisible();
				if (isVisible == true) {
					DoColorize(mc, PaintColors[rbnum].R, PaintColors[rbnum].G, PaintColors[rbnum].B, PaintColors[rbnum].A);
					if (DebugMode == true) Log.Write("Success performing material alterations on player ObjectId '"+agentObjId.ToString().Trim()+"'");
				}
			}
		} catch {
			if (DebugMode == true) Log.Write("OnColorClick() - Exception");
		}
	}
	
	private void OnRGB(ObjectId agentObjId)
	{
		try {
			if (agentObjId != null) {
				MeshComponent mc = null;
				ScenePrivate.FindObject(agentObjId).TryGetComponent(0, out mc);
				bool isVisible = mc.GetIsVisible();
				if (isVisible == true) {
					DoRGB(mc);
					if (DebugMode == true) Log.Write("Success performing material alterations on player ObjectId '"+agentObjId.ToString().Trim()+"'");
				}
			}
		} catch {
			if (DebugMode == true) Log.Write("OnRGB() - Exception");
		}
	}	

	private void OnClean(ObjectId agentObjId)
	{
		try {
			AvatarMatsData avatarMatsData = GetAvatarMatsData(agentObjId);

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
							if (DebugMode == true) Log.Write("Returning material back to original states for emiss/color: #" + j.ToString().Trim() + " :: "  + materials2[j].Name);
						}
					}
				}
			} catch {
				if (DebugMode == true) Log.Write("OnClean() - Exception 1 ");
			}
		} catch {
			if (DebugMode == true) Log.Write("OnClean() - Exception 2");
		}		
	}
	
	private void OnUserJoin(UserData data)
    {
		try {
			//save avatars original materials via OnUserJoin, so they can be restored when clicking the CleanserButton
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