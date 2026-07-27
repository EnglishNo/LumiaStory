public static class ElementCalculator
{
    /// <summary>
    /// 공격자의 속성과 방어자의 속성을 비교하여 데미지 배율을 반환합니다.
    /// </summary>
    public static float GetMultiplier(ElementType attacker, ElementType defender)
    {
        // 1. 방어자가 빛 속성일 때 특수 규칙: 어둠 공격만 1.5배, 그 외 모든 속성은 0.5배 반감
        if (defender == ElementType.Light)
        {
            return (attacker == ElementType.Dark) ? 1.5f : 0.5f;
        }

        // 2. 공격자가 어둠 속성일 때 특수 규칙: 모든 속성에 1.5배 (빛 속성 포함)
        if (attacker == ElementType.Dark)
        {
            return 1.5f;
        }

        // 3. 일반 속성 상성 관계 처리
        switch (attacker)
        {
            case ElementType.Water: // 물: 불/독에 1.5배, 전기에 0.5배
                if (defender == ElementType.Fire || defender == ElementType.Poison) return 1.5f;
                if (defender == ElementType.Electric) return 0.5f;
                break;

            case ElementType.Fire: // 불: 자연/독에 1.5배, 물/강에 0.5배
                if (defender == ElementType.Nature || defender == ElementType.Poison) return 1.5f;
                if (defender == ElementType.Water || defender == ElementType.Solid) return 0.5f;
                break;

            case ElementType.Nature: // 자연: 강/전기에 1.5배, 독에 0.5배
                if (defender == ElementType.Solid || defender == ElementType.Electric) return 1.5f;
                if (defender == ElementType.Poison) return 0.5f;
                break;

            case ElementType.Solid: // 강: 불/독에 1.5배, 자연에 0.5배
                if (defender == ElementType.Fire || defender == ElementType.Poison) return 1.5f;
                if (defender == ElementType.Nature) return 0.5f;
                break;

            case ElementType.Electric: // 전기: 물에 1.5배, 자연에 0.5배
                if (defender == ElementType.Water) return 1.5f;
                if (defender == ElementType.Nature) return 0.5f;
                break;

            case ElementType.Poison: // 독: 자연/물에 1.5배, 불/강에 0.5배
                if (defender == ElementType.Nature || defender == ElementType.Water) return 1.5f;
                if (defender == ElementType.Fire || defender == ElementType.Solid) return 0.5f;
                break;

            case ElementType.Light: // 빛: 어둠에 1.5배 (상극)
                if (defender == ElementType.Dark) return 1.5f;
                break;

            case ElementType.None: // 무 속성
            default:
                return 1.0f;
        }

        return 1.0f; // 상성에 해당하지 않는 경우 기본 1.0배
    }
}