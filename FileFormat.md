# Borderlands Profile File Format
In this document I'll go over what's known about the profile file format.

Also see [this gist](https://gist.github.com/apple1417/1c589dc46e330300918ed04a1b8d83fa), containing
kaitai structs describing the file format. Kaitai can't serialize files, but it provides a
[handy ide](https://ide.kaitai.io/) to look through these files with.

Firstly, note that all fields are big endian.

## File Header
The majority of the profile file is compressed, but there's a small header first.

| Size | Description |
|---:|:---|
| 20 | A SHA1 hash of the remainder of the file. |
| 4 | The size of the decompressed profile data.
| Until EOF | LZO compressed data

![File Header](https://i.imgur.com/7g9Us0i.png)

Decompress the remaining data before continuing looking through the file.

## Decompressed Data
The decompressed data consits of a number of entries. The first four bytes hold the entry count.

| Size | Description |
|---:|:---|
| 4 | The amount of profile entries in the file |

Each entry is then appended one after the other. There may be padding after the stored amount of entries, but this is ignored.

Each entry has the following format.

| Size | Description |
|---:|:---|
| 1 | `EOnlineProfilePropertyOwner` - unknown use |
| 4 | An int32 ID uniquely identifying each setting |
| 1 | `ESettingsDataType` - What type of data the setting stores |
| Variable | Value |
| 1 | `EOnlineDataAdvertisementType` - unknown use |

![Decompressed Data Format](https://i.imgur.com/JeuJTZI.png)

## Unrealscript Decompilation
The enums previously described can be found when decompiling the game's UnrealScript.
```cs
enum EOnlineProfilePropertyOwner {
    OPPO_None,
    OPPO_OnlineService,
    OPPO_Game,
    OPPO_MAX
};
enum ESettingsDataType {
    SDT_Empty,
    SDT_Int32,
    SDT_Int64,
    SDT_Double,
    SDT_String,
    SDT_Float,
    SDT_Blob,
    SDT_DateTime,
    SDT_Byte,
    SDT_MAX
};
enum EOnlineDataAdvertisementType {
    ODAT_DontAdvertise,
    ODAT_OnlineService,
    ODAT_QoS,
    ODAT_OnlineServiceAndQoS,
    ODAT_MAX
};
```
In practice, I've never seen the following values:
```cs
EOnlineProfilePropertyOwner.OPPO_None
ESettingsDataType.SDT_Empty
ESettingsDataType.SDT_Int64
ESettingsDataType.SDT_Double
ESettingsDataType.SDT_DateTime
EOnlineDataAdvertisementType.ODAT_OnlineService
EOnlineDataAdvertisementType.ODAT_QoS
EOnlineDataAdvertisementType.ODAT_OnlineServiceAndQoS
```

Additionally, you can find the `WillowProfileSettings` class, which has two important fields when
dumping in game: `ProfileSettings` and `ProfileMappings`.

`ProfileMappings` contains a bit of info on what values are stored in each id. Most notably, this is
where I extracted all the names from.

`ProfileSettings` contains an array of `OnlineProfileSetting` structs. These hold values in the
exact same order as profile entries, they're how I could confirm what enums each field maps to.

```cs
struct native OnlineProfileSetting {
    var OnlinePlayerStorage.EOnlineProfilePropertyOwner Owner;
    var SettingsProperty ProfileSetting;

    structdefaultproperties
    {
        Owner=EOnlineProfilePropertyOwner.OPPO_None
        ProfileSetting=(PropertyId=0,Data=(Type=ESettingsDataType.SDT_Empty,Value1=0),AdvertisementType=EOnlineDataAdvertisementType.ODAT_DontAdvertise)
    }
};
```

## Value Format
The value field of each entry uses different formats based on the setting data type.

### Observed Formats
#### `SDT_Int32` (1)
| Size | Description |
|---:|:---|
| 4 | The int32 value |

#### `SDT_String` (4)
| Size | Description |
|---:|:---|
| 4 | The size of the string |
| Variable | The string contents |

I do not know exactly what encoding it uses, beyond that it's ASCII-compatible.

#### `SDT_Float` (5)
| Size | Description |
|---:|:---|
| 4 | The float value |

#### `SDT_Blob` (6)
| Size | Description |
|---:|:---|
| 4 | The size of the binary blob |
| Variable | The binary blob contents |

#### `SDT_Byte` (8)
| Size | Description |
|---:|:---|
| 1 | The byte value |

### Unknown Formats
I have not seen these data types in practice, but you can make some reasonable guesses.

#### `SDT_Empty` (0)
| Size | Description |
|---:|:---|
| 0 | Nothing |

#### `SDT_Int64` (2)
| Size | Description |
|---:|:---|
| 8 | The int64 value |

#### `SDT_Double` (3)
| Size | Description |
|---:|:---|
| 8 | The double value |

#### `SDT_DateTime` (7)
This one is more difficult to guess. The only definite hint we have is some getters/setters which
take two seperate int values. As UnrealScript ints are 32-bit, we probably have an 8 byte value.
UE4 defines a
[`FDateTime` struct](https://docs.unrealengine.com/4.26/en-US/API/Runtime/Core/Misc/FDateTime/),
which stores ticks in an int64 ticks - maybe it just splits this value in two out of neccesity?

| Size | Description |
|---:|:---|
| 8 | An int64 value of 100 nanosecond ticks since January 1, 0001 |

This could be completely wrong, but since nothing uses this data type I can't exactly confirm it.
