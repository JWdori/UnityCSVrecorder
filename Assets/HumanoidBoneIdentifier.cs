using System;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class HumanoidBoneIdentifier : MonoBehaviour
{
    private Animator _animator;

    void Awake()
    {
        // 같은 계층의 부모에서 Animator를 찾아옵니다.
        _animator = GetComponentInParent<Animator>();
        if (_animator == null || !_animator.isHuman)
        {
            Debug.LogWarning($"[{name}] 상위에 Humanoid Animator가 없습니다.");
            return;
        }

        IdentifyBone();
    }

    private void IdentifyBone()
    {
        Transform myTransform = transform;
        bool found = false;

        // HumanBodyBones 열거형을 모두 순회하며 매핑된 본을 찾아냅니다.
        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            // None 은 건너뜁니다.
            if (bone == HumanBodyBones.LastBone || bone == HumanBodyBones.Hips && _animator.GetBoneTransform(bone) == null)
                continue;

            Transform boneTransform = _animator.GetBoneTransform(bone);
            if (boneTransform == myTransform)
            {
                Debug.Log($"[HumanoidBoneIdentifier] Component GameObject: \"{name}\", Humanoid Joint: {bone}");
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.Log($"[HumanoidBoneIdentifier] \"{name}\" 는 Humanoid 본에 매핑되어 있지 않습니다.");
        }
    }
}
