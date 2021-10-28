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

Note that if the decompressed data size goes over 9000 bytes, the games intepret your profile as 
corrupt. Practically, this will never happen, it's only a concern when creating your own files.

## Decompressed Data
The decompressed data consits of a number of entries. The first four bytes hold the entry count.

| Size | Description |
|---:|:---|
| 4 | The amount of profile entries in the file |

Each entry is then appended one after the other. There may be padding after the stored amount of
entries, but this is ignored.

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

I do not know exactly what encoding it uses, beyond that it's ASCII-compatible - it's not UTF16 like
Unreal Engine likes to use.

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

# Intepreting Individual Settings
So if you've followed along so far, you should have a map of setting IDs (and perhaps their names)
to their associated values. Most of these values pretty intuitive, but some, including pretty much
all the interesting ones, use more puzzling formats.

## Customizations
In BL2 and TPS, `UnlockedCustomizations_MainGame` (ID 300) is a blob storing all your
customizations, in what appears to be a bitfield, of exactly 1001 bytes. A few default bits are set,
and as you unlock more customizations more bits get set. Setting all bits unlocks all
customizations. Clearing all bits will re-unlock the default bits for you, you don't need to worry
about storing defaults. I didn't investigate any further, to work out anything like what bits map to
what customization.

## Golden Keys
### BL2/TPS
You have a single integer amount of Golden Keys, so makes sense to just use an int setting right? Oh
boy are you in for some fun. In BL2 and TPS, `GoldenKeysEarned` (162) is a *blob* value, which
consists of a series of repeated 3 byte entries.

| Size | Description |
|---:|:---|
| 1 | Some sort of ID? Can be ignored, duplicate values don't matter |
| 1 | An amount of keys you've earned (unsigned) |
| 1 | An amount of keys you've spent (unsigned) |

The game looks through all of these entires, sums the amount you've earned, and subtracts the sum of
the amount you've spent. Once you go above 255 keys, you can simply add another 3 bytes on the end
and start again.

In practice, you can generally get to about 500,000 keys before you start running into the 9000 byte
decompressed data limit. The game's UI starts breaking at just 1000 however - the icon when hovering
over the chest caps keys to 999, and the BAR screen just shows the leftmost 3 digits. If you set
your spent count higher than your earned count you will end up with negative keys, though this does
nothing, you can't use the Golden Chest with them.

### BL1E
BL1E is a little more sensible, though this is still Gearbox we're talking about. It uses two int32
settings, `keyCount` (130) and `keysSpent` (131). `keyCount` holds the keys you've earned again, while
`keysSpent` stores the amount you've spent, the actual amount of keys you have access to is
`keyCount` - `keysSpent`.

Since the Golden Chest is the only way to view your key count in this game, it's impossible to tell
if your key count is allowed to go negative. Setting `keyCount` <= 0 or `keysSpent` >= `keyCount`
both cause you to be unable to use the chest though, it's not likely to be useful. `keysSpent` is
clamped to >= 0, you can't set it negative to give yourself more.

## Stash
The stash uses 4 settings, `StashSlot0` through `StashSlot3` (130-133). These are
all blob values simply holding the item serial directly. To convert to a traditional serial code,
simply base64 encode the bytes, and surround them with brackets and the relevant prefix.

## Badass Rank
Badass Rank is actually uses two settings - `BadassPoints` (136) and `BadassPointsSpent` (137).
Ignore this `Spent` name, it's misleading. These settings always have the exact same value (unless
you profile edit), but in classical Gearbox style they're used differently. `BadassPointsSpent` is
used to calculate your displayed Badass Rank. When you complete a challenge, the game adds the it's
point value to `BadassPoints` instead, and sets both settings to the resulting value.

Once you've got a point value, you still need to convert it to a rank value, which you just do by
dividing points by 5. This actually uses a value that can be modded `GD_Globals.General.Globals`
`BadassPointsPerRank`

Setting your Badass Points to be negative will cause the game to crash when it loads your profile.

## Badass Tokens
Finally an easy one. Your available Badass Tokens are stored in `BadassTokens` (138) as a regular
int value.

## Badass Rank Rewards
Here we get to Gearbox making questionable data format decisions again. Your Badass Rank Rewards
are stored as a *string* in `BadassRewardsEarned` (143).

The actual stored value we'll get to in a bit is an array of int32s, storing the amount of redeemed
rewards for each stat. This is in the same order as displayed in game, which is stored in
`GD_Globals.General.Globals` `BadassRewards` (you can probably mod this again):
 1. MaxHealth
 2. ShieldCapacity
 3. ShieldDelay
 4. ShieldRate
 5. MeleeDamage
 6. GrenadeDamage
 7. GunAccuracy
 8. GunDamage
 9. FireRate
10. RecoilReduction
11. ReloadSpeed 
12. ElementalChance 
13. ElementalDamage 
14. CritDamage 

The actual applied bonuses use a diminisher and convert from a percentage to a decimal, for each
value you do `redeemed^0.75 / 100`. The displayed bonuses on the Badass Rank screen of course still 
show the (rounded) percentage value. This is also moddable, look at
`GD_Challenges.BadassSkill.BadassSkill`.

So how do you convert the string stored in the setting to and from this array. You start with a
custom alphabet, `0123456789ABCDEFGHJKMNPQRSTVWXYZ` (note that this omits `ILOU`). This is exactly 32 characters, so each character in the string maps to a 5 bit index.

```
S     P     M     C     3     D     6
25    22    20    12    3     13    6
11001 10110 10100 01100 00011 01101 00110
```

These bits fill up the array ints starting at index 0 moving forwards, working from the LSB to MSB
of each entry. It can be convenient to think of this by working in reverse - if you reverse the
string, you can simply mash all the index bits together, and read off 32 bit chunks from the right
hand side. Any required padding bits are simply ignored.

```
Index 1 |               Index 0
  001   | 10011010 00110110 01010010 11011001
  666   | 66     3 3333      MMMMM      SSSSS
        |   DDDDD      CCCC C     PP PPP
```

So at this point you have an array of int32s, but they're nothing like the amount of redeemed
rewards. The final step is that every value in the array is XORed with `0x9A3652D9`. You'll note
that in this example, this leaves us with a final value of 0.
