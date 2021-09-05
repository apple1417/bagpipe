using System;
using System.ComponentModel;

namespace bagpipe {
  enum OnlineProfilePropertyOwner {
    None = 0,
    OnlineService = 1,
    Game = 2,
  }

  enum SettingsDataType {
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

  enum OnlineDataAdvertisementType {
    DontAdvertise = 0,
    OnlineService = 1,
    QOS = 2,
    OnlineServiceAndQOS = 3,
  }
}
