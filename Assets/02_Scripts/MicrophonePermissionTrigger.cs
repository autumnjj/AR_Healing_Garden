using UnityEngine;

public class MicrophonePermissionTrigger : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log($"Mic Number:{Microphone.devices.Length}");

        for(int i = 0; i < Microphone.devices.Length; i++)
        {
            Debug.Log($"Mic tool {i}:{Microphone.devices[i]}");
        }

        Debug.Log("MicrophonePermissionTrigger : ����ũ ���� �ڵ� �߰� �Ϸ�");
    }
}
