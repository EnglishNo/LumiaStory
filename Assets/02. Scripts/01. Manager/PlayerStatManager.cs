using UnityEngine;

[System.Serializable]
public struct PlayerStats
{
    [Header("기본 스탯 (Base)")]
    public float maxHp;
    public float jumpPower;
    public float moveSpeed;

    [Header("공격 스탯 (Attack)")]
    public float physicalAtk;       // 물리 공격력
    public float magicalAtk;        // 마법 공격력
    [Range(0f, 100f)] public float criticalRate;   // 치명타 확률 (%)
    public float criticalDamage;    // 치명타 추가 데미지 비율 (%) (예: 30 입력 시 2.3배)

    [Header("방어 및 유틸 스탯 (Defense & Utility)")]
    public float physicalDef;       // 물리 방어력
    public float magicalDef;        // 마법 방어력
    [Range(0f, 100f)] public float evasionRate;    // 회피력 (%)
    [Range(0f, 100f)] public float statusResist;   // 상태이상 저항력 (%)
    public float buffDurationBonus; // 버프 추가 지속력

    [Header("속성 추가 데미지 (Elemental Bonus)")]
    public float waterBonusDmg;
    public float fireBonusDmg;
    public float natureBonusDmg;
    public float solidBonusDmg;     // Solid(강) 속성 추가 데미지
    public float electricBonusDmg;
    public float poisonBonusDmg;
    public float lightBonusDmg;
    public float darkBonusDmg;

    [Header("속성 방어력 (Elemental Defense)")]
    public float waterDef;
    public float fireDef;
    public float natureDef;
    public float solidDef;          // Solid(강) 속성 방어력
    public float electricDef;
    public float poisonDef;
    public float lightDef;
    public float darkDef;
}

public class PlayerStatManager : MonoBehaviour
{
    [SerializeField]
    private PlayerStats baseStats; // 인스펙터에서 입력할 플레이어 전체 스탯

    private Player playerScript;

    private void Awake()
    {
        playerScript = GetComponent<Player>();
    }

    private void Start()
    {
        SyncStats();
    }

    // 게임 실행 중(Play Mode) 인스펙터에서 스탯 수치를 변경하면 즉시 Player에 다시 전달
    private void OnValidate()
    {
        if (Application.isPlaying && playerScript != null)
        {
            playerScript.ReceiveStats(baseStats);
        }
    }

    public void SyncStats()
    {
        if (playerScript != null)
        {
            playerScript.ReceiveStats(baseStats);
        }
    }

    /// <summary>
    /// 피격 시 데미지 계산 로직
    /// </summary>
    public float CalculateTakenDamage(float rawDamage, DamageType dmgType, ElementType element)
    {
        float finalDamage = rawDamage;

        // 1. 물리/마법 방어력 차감
        if (dmgType == DamageType.Physical)
            finalDamage -= baseStats.physicalDef;
        else if (dmgType == DamageType.Magical)
            finalDamage -= baseStats.magicalDef;

        // 2. 속성 방어력 차감
        finalDamage -= GetElementalDefense(element);

        // 최소 데미지 1 보정
        return Mathf.Max(1f, finalDamage);
    }

    /// <summary>
    /// 공격 시 데미지 계산 로직 (방식 1: 퍼센트 합산 방식 적용)
    /// </summary>
    public float CalculateDealDamage(DamageType dmgType, ElementType element, out bool isCritical)
    {
        // 1. 기본 공격력 설정 (물리 or 마법)
        float damage = (dmgType == DamageType.Physical) ? baseStats.physicalAtk : baseStats.magicalAtk;

        // 2. 속성 추가 데미지 합산
        damage += GetElementalBonusDamage(element);

        // 3. 치명타 계산 (방식 1 적용)
        isCritical = Random.Range(0f, 100f) < baseStats.criticalRate;
        if (isCritical)
        {
            // 기본 2배(200%) + (스탯 / 100)% 추가 배율
            damage *= (2f + (baseStats.criticalDamage / 100f));
        }

        return damage;
    }

    // --- 속성별 스탯 반환 헬퍼 함수 ---
    private float GetElementalDefense(ElementType type)
    {
        switch (type)
        {
            case ElementType.Water: return baseStats.waterDef;
            case ElementType.Fire: return baseStats.fireDef;
            case ElementType.Nature: return baseStats.natureDef;
            case ElementType.Solid: return baseStats.solidDef;
            case ElementType.Electric: return baseStats.electricDef;
            case ElementType.Poison: return baseStats.poisonDef;
            case ElementType.Light: return baseStats.lightDef;
            case ElementType.Dark: return baseStats.darkDef;
            default: return 0f;
        }
    }

    private float GetElementalBonusDamage(ElementType type)
    {
        switch (type)
        {
            case ElementType.Water: return baseStats.waterBonusDmg;
            case ElementType.Fire: return baseStats.fireBonusDmg;
            case ElementType.Nature: return baseStats.natureBonusDmg;
            case ElementType.Solid: return baseStats.solidBonusDmg;
            case ElementType.Electric: return baseStats.electricBonusDmg;
            case ElementType.Poison: return baseStats.poisonBonusDmg;
            case ElementType.Light: return baseStats.lightBonusDmg;
            case ElementType.Dark: return baseStats.darkBonusDmg;
            default: return 0f;
        }
    }
}