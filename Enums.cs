namespace bagpipe {
  public enum OnlineProfilePropertyOwner {
    None = 0,
    OnlineService = 1,
    Game = 2,
  }

  public enum SettingsDataType {
    Empty = 0,
    Int32 = 1,
    Int64 = 2,
    Double = 3,
    String = 4,
    Float = 5,
    Blob = 6,
    DateTime = 7,
    Byte = 8
  }

  public enum OnlineDataAdvertisementType {
    DontAdvertise = 0,
    OnlineService = 1,
    QOS = 2,
    OnlineServiceAndQOS = 3,
  }

  public enum Game {
    None,
    BL1,
    BL1E,
    BL2,
    TPS,
  }

  public enum BARRewardStat {
    MaxHealth = 0,
    ShieldCapacity = 1,
    ShieldDelay = 2,
    ShieldRate = 3,
    MeleeDamage = 4,
    GrenadeDamage = 5,
    GunAccuracy = 6,
    GunDamage = 7,
    FireRate = 8,
    RecoilReduction = 9,
    ReloadSpeed = 10,
    ElementalChance = 11,
    ElementalDamage = 12,
    CritDamage = 13,
  }
}
