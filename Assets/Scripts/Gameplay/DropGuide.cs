using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 스포너의 위치에서 아래로 낙하 가이드 점선을 그린다.
    /// DropSpawner 의 자식으로 붙어 Preview 오브젝트와 함께 움직인다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class DropGuide : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private DropSpawner spawner;
        [SerializeField] private LineRenderer line;

        [Header("설정")]
        [SerializeField] private float maxY = 5f;
        [SerializeField] private float minY = -4.5f;

        private void Reset()
        {
            line = GetComponent<LineRenderer>();
            spawner = GetComponentInParent<DropSpawner>();
            
            // 기본 점선 설정
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(1f, 1f, 1f, 0.3f);
            line.endColor = new Color(1f, 1f, 1f, 0.1f);
        }

        private void Update()
        {
            if (spawner == null || line == null) return;

            // 스포너가 쿨다운 중(IsDropping)이면 가이드를 숨긴다.
            if (spawner.IsDropping)
            {
                line.enabled = false;
                return;
            }

            line.enabled = true;
            Vector3 startPos = transform.position;
            line.SetPosition(0, startPos);
            line.SetPosition(1, new Vector3(startPos.x, minY, startPos.z));
        }
    }
}
