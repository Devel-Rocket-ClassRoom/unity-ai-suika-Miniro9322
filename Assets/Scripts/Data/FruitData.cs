using UnityEngine;

namespace SuikaGame.Data
{
    /// <summary>
    /// 단일 과일의 메타데이터. 단계(level)별로 1개씩 11개 인스턴스 생성.
    /// 머지 시 nextStage 가 다음 단계 데이터를 가리킨다. 11번(수박)은 null.
    /// 참고: GDD §3 과일 단계 표.
    /// </summary>
    [CreateAssetMenu(fileName = "FruitData", menuName = "Suika/Fruit Data", order = 10)]
    public class FruitData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("1~11 단계. 1=체리, 11=수박")]
        [Range(1, 11)] public int level = 1;

        [Tooltip("한국어 이름 (예: 체리)")]
        public string nameKo;

        [Tooltip("영문 이름 (예: Cherry)")]
        public string nameEn;

        [Header("물리 / 스코어")]
        [Tooltip("체리(1단계)를 1.0 으로 한 상대 반지름. 실제 월드 반지름은 FruitPool/스폰 측에서 기준값과 곱해 사용.")]
        [Min(0.01f)] public float relativeRadius = 1.0f;

        [Tooltip("이 과일을 머지로 만들 때 얻는 점수.")]
        [Min(0)] public int mergeScore = 1;

        [Header("표시")]
        [Tooltip("UI/스폰 시 사용할 스프라이트 아이콘.")]
        public Sprite icon;

        [Header("머지 연결")]
        [Tooltip("머지 결과 다음 단계 과일 데이터. 최종 단계(11번 수박)는 null.")]
        public FruitData nextStage;

        [Header("스폰 프리팹")]
        [Tooltip("이 과일의 인스턴스를 생성할 때 사용할 프리팹. FruitPrefabGenerator 실행 시 자동 할당된다.")]
        public GameObject prefab;

        /// <summary>최종 단계(수박) 여부.</summary>
        public bool IsFinalStage => nextStage == null;
    }
}
