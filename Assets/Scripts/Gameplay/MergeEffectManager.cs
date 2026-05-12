using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 머지 발생 시 해당 위치에 점수 팝업 등의 시각 효과를 생성한다.
    /// </summary>
    public class MergeEffectManager : MonoBehaviour
    {
        [Header("팝업 설정")]
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private Color textColor = Color.yellow;

        [Header("이펙트 설정")]
        [SerializeField] private GameObject mergeParticlePrefab;

        private void OnEnable()
        {
            MergeSpawner.OnMerge += HandleMerge;
        }

        private void OnDisable()
        {
            MergeSpawner.OnMerge -= HandleMerge;
        }

        private void HandleMerge(int level, Vector2 pos, int score)
        {
            SpawnScorePopup(pos, score);
            SpawnParticle(pos);
        }

        private void SpawnScorePopup(Vector2 pos, int score)
        {
            GameObject go = new GameObject("ScorePopup");
            go.transform.position = pos;

            var tm = go.AddComponent<TextMesh>();
            tm.text = $"+{score}";
            tm.color = textColor;
            tm.fontSize = 64; 
            tm.characterSize = 0.02f; 
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            var anim = go.AddComponent<PopupAnimator>();
            anim.Setup(duration, moveSpeed);
        }

        private void SpawnParticle(Vector2 pos)
        {
            if (mergeParticlePrefab != null)
            {
                var go = Instantiate(mergeParticlePrefab, pos, Quaternion.identity);
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
            }
        }

        private class PopupAnimator : MonoBehaviour
        {
            private float elapsed = 0f;
            private float maxLife;
            private float speed;
            private TextMesh tm;
            private Color startColor;

            public void Setup(float life, float s)
            {
                maxLife = life;
                speed = s;
                tm = GetComponent<TextMesh>();
                startColor = tm.color;
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                if (elapsed >= maxLife)
                {
                    Destroy(gameObject);
                    return;
                }

                transform.Translate(Vector3.up * speed * Time.deltaTime);
                
                float alpha = 1f - (elapsed / maxLife);
                tm.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
        }
    }
}
