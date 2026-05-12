namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 게임 전역 상수. 추후 ScriptableObject 로 옮길 가능성 있음.
    /// 참고: GDD §3 (체리=1.0× 기준 상대 반지름).
    /// </summary>
    public static class SuikaConstants
    {
        /// <summary>
        /// 체리(상대 반지름 1.0×) 의 실제 월드 반지름.
        /// 모든 과일의 CircleCollider2D.radius = BaseFruitRadius * FruitData.relativeRadius.
        /// 기본 박스 너비 6 기준으로 시각적으로 적당하도록 0.25 로 시작.
        /// </summary>
        public const float BaseFruitRadius = 0.25f;
    }
}
