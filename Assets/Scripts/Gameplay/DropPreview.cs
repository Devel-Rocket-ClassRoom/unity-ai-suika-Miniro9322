using SuikaGame.Data;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 드롭되기 전 대기 중인 과일을 시각적으로 보여주는 컴포넌트.
    /// DropSpawner 가 현재 과일 데이터를 전달하면 SpriteRenderer 와 스케일을 갱신한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DropPreview : MonoBehaviour
    {
        [Header("컴포넌트 참조")]
        [SerializeField] private SpriteRenderer sr;

        private void Reset()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 프리뷰 이미지와 크기를 갱신.
        /// </summary>
        public void SetFruit(FruitData data, float baseRadius)
        {
            if (data == null || sr == null)
            {
                if (sr != null) sr.sprite = null;
                return;
            }

            sr.sprite = data.icon;

            if (sr.sprite != null)
            {
                float worldRadius = Mathf.Max(0.001f, baseRadius * data.relativeRadius);
                
                // data.tightRadius 가 0이면 (분석 안됨) bounds extents 사용
                float localTightRadius = (data.tightRadius > 0) 
                    ? (data.tightRadius / sr.sprite.pixelsPerUnit) 
                    : sr.sprite.bounds.extents.x;

                if (localTightRadius > 0.0001f)
                {
                    float scale = worldRadius / localTightRadius;
                    transform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }
            }
            
            // 프리뷰는 약간 투명하게 처리할 수도 있음 (선택)
            Color c = sr.color;
            c.a = 0.8f; 
            sr.color = c;
        }

        public void SetVisible(bool visible)
        {
            if (sr != null) sr.enabled = visible;
        }
    }
}
