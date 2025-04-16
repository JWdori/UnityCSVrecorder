using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class HumanoidDataPlayer : MonoBehaviour
{
    [Header("upload CSV file")]
    [SerializeField] private TextAsset csvData;
    [SerializeField] private int currentFramePlaying = 0;

    [SerializeField] private bool loopPlayback = true;

    [SerializeField] private float playbackSpeed = 1.0f;

    // same record and play
    private HumanBodyBones[] targetBones = new HumanBodyBones[]
    {
        // Upper Body
        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.LeftThumbProximal,
        HumanBodyBones.LeftThumbIntermediate,
        HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,
        HumanBodyBones.LeftIndexIntermediate,
        HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal,
        HumanBodyBones.LeftMiddleIntermediate,
        HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,
        HumanBodyBones.LeftRingIntermediate,
        HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal,
        HumanBodyBones.LeftLittleIntermediate,
        HumanBodyBones.LeftLittleDistal,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightHand,
        HumanBodyBones.RightThumbProximal,
        HumanBodyBones.RightThumbIntermediate,
        HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal,
        HumanBodyBones.RightIndexIntermediate,
        HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal,
        HumanBodyBones.RightMiddleIntermediate,
        HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal,
        HumanBodyBones.RightRingIntermediate,
        HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal,
        HumanBodyBones.RightLittleIntermediate,
        HumanBodyBones.RightLittleDistal,
        // Lower Body
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightFoot
    };

    private Dictionary<HumanBodyBones, Transform> boneDict = new Dictionary<HumanBodyBones, Transform>();

    private class FrameData
    {
        public float time;
        public Quaternion[] rotations; 
        public Vector3[] positions;  
    }

    
    private List<FrameData> frames = new List<FrameData>();


    private float playbackTimer = 0f;
    private int currentFrameIndex = 0;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if(animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("Humanoid Animator is required.");
            return;
        }

        foreach (HumanBodyBones bone in targetBones)
        {
            Transform t = animator.GetBoneTransform(bone);
            if(t != null)
                boneDict[bone] = t;
            else
                Debug.LogWarning($"Animator could not find bone: {bone}");
        }

        if(csvData == null)
        {
            Debug.LogError("CSV file is not assigned.");
            return;
        }
        ParseCSV();
    }

    /// <summary>
    /// [Frame, Time, (RotX, RotY, RotZ, RotW, PosX, PosY, PosZ) ...]
    /// </summary>
    private void ParseCSV()
    {
        string[] lines = csvData.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        if(lines.Length < 2)
        {
            Debug.LogError("Not enough lines in CSV data.");
            return;
        }

    
        for(int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] tokens = line.Split(',');
            int expectedTokenCount = 2 + targetBones.Length * 7;
            if(tokens.Length < expectedTokenCount)
            {
                Debug.LogWarning($"Line {i} has insufficient data.");
                continue;
            }

            FrameData fd = new FrameData();

            // tokens[0] x, tokens[1] o
            if (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out fd.time))
            {
                Debug.LogWarning($"Could not parse time at line {i}.");
                continue;
            }

            int boneCount = targetBones.Length;
            fd.rotations = new Quaternion[boneCount];
            fd.positions = new Vector3[boneCount];

            for (int j = 0; j < boneCount; j++)
            {
                int baseIndex = 2 + j * 7;
                float rx = float.Parse(tokens[baseIndex], CultureInfo.InvariantCulture);
                float ry = float.Parse(tokens[baseIndex + 1], CultureInfo.InvariantCulture);
                float rz = float.Parse(tokens[baseIndex + 2], CultureInfo.InvariantCulture);
                float rw = float.Parse(tokens[baseIndex + 3], CultureInfo.InvariantCulture);
                Quaternion rot = new Quaternion(rx, ry, rz, rw);

                float px = float.Parse(tokens[baseIndex + 4], CultureInfo.InvariantCulture);
                float py = float.Parse(tokens[baseIndex + 5], CultureInfo.InvariantCulture);
                float pz = float.Parse(tokens[baseIndex + 6], CultureInfo.InvariantCulture);
                Vector3 pos = new Vector3(px, py, pz);

                fd.rotations[j] = rot;
                fd.positions[j] = pos;
            }
            frames.Add(fd);
        }

        if(frames.Count > 0)
        {
            playbackTimer = frames[0].time;  
        }
        else
        {
            Debug.LogError("No frame data read from CSV.");
        }
    }

    void Update()
    {
        if (frames.Count == 0)
            return;

        playbackTimer += Time.deltaTime * playbackSpeed;

        if (playbackTimer > frames[frames.Count - 1].time)
        {
            if (loopPlayback)
            {
                playbackTimer = frames[0].time;
                currentFrameIndex = 0;
            }
            else
            {
                playbackTimer = frames[frames.Count - 1].time;
                currentFrameIndex = frames.Count - 1;
            }
        }
        while (currentFrameIndex < frames.Count - 1 && frames[currentFrameIndex + 1].time <= playbackTimer)
        {
            currentFrameIndex++;
        }
        
        currentFramePlaying = currentFrameIndex;
        ApplyFrame(frames[currentFrameIndex]);
    }


    private void ApplyFrame(FrameData fd)
    {
        for (int i = 0; i < targetBones.Length; i++)
        {
            HumanBodyBones bone = targetBones[i];
            if(boneDict.TryGetValue(bone, out Transform t))
            {
                t.localRotation = fd.rotations[i];
                t.localPosition = fd.positions[i];
            }
        }
    }
}
