using UnityEngine;

public enum ElementType
{
    None,     // 무
    Water,    // 물
    Fire,     // 불
    Nature,   // 자연
    River,    // 강
    Electric, // 전기
    Poison,   // 독
    Light,    // 빛
    Dark      // 어둠
}

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "Monster/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("몬스터 기본 정보")]
    public string monsterName = "기본 몬스터";
    public ElementType elementType = ElementType.None; // 몬스터 속성

    [Header("몬스터 스텟")]
    public float maxHp = 100f;
    public float physicalAtk = 10f;  // 물리 공격력
    public float magicAtk = 0f;      // 마법 공격력
    public float defense = 5f;       // 물리 & 마법 방어력
    public float evasion = 0f;       // 회피력 (예: 10 입력 시 10% 회피율)
    public float moveSpeed = 2.5f;   // 이동 속도
}